using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class BoardEquipmentPanelUI : MonoBehaviour
{
    [Serializable]
    public sealed class SkillOption
    {
        public MergeBoxEquipmentData data;
        public Toggle toggle;
        public GameObject selected;
        public Image[] skillFrames;
        [NonSerialized] public Color[] authoredColors;
    }

    [SerializeField] private BagEquipmentInventoryUI inventory;
    [SerializeField] private InventorySearchUI search;
    [SerializeField] private BagEquipmentSlotUI[] materialSlots;
    [SerializeField] private BagEquipmentSlotUI[] pinSlots;
    [SerializeField] private SkillOption[] boardSkills;
    [SerializeField] private BagEquipmentSlotUI eggSlot;
    [SerializeField] private Button previousEgg;
    [SerializeField] private Button nextEgg;
    [SerializeField] private TooltipUI itemTooltip;
    [SerializeField] private InventoryManager materialInventory;

    private GameSession session;
    private InventoryManager stock;
    private UnityAction<bool>[] skillHandlers;
    private bool editingPin;
    private int selectedSlot = -1;

    public void InitializeLoadout(IReadOnlyList<int> unlocked)
    {
        var current = EquipmentSave.Current;
        if (current.board?.IsConfigured == true && current.itemIds.Count == materialSlots.Length &&
            current.board.pinIds.Count == pinSlots.Length) return;

        var equipment = current.Copy();
        var board = GetOrCreateBoard(equipment, unlocked);
        FitSlots(equipment.itemIds, materialSlots.Length);
        FitSlots(board.pinIds, pinSlots.Length);
        EquipmentSave.Write(equipment);
    }

    private static void FitSlots(List<string> ids, int count)
    {
        if (ids.Count > count) ids.RemoveRange(count, ids.Count - count);
        while (ids.Count < count) ids.Add(string.Empty);
    }

    private void OnEnable()
    {
        BindSlots(materialSlots, false);
        BindSlots(pinSlots, true);
        eggSlot?.Bind(null, tooltip: itemTooltip);
        previousEgg.onClick.AddListener(SelectPreviousEgg);
        nextEgg.onClick.AddListener(SelectNextEgg);

        skillHandlers = new UnityAction<bool>[boardSkills.Length];
        for (int i = 0; i < boardSkills.Length; i++)
        {
            var option = boardSkills[i];
            if (option.authoredColors == null && option.skillFrames != null)
                option.authoredColors = Array.ConvertAll(option.skillFrames, frame => frame != null ? frame.color : Color.white);
            skillHandlers[i] = selected => SetSkill(option, selected);
            option.toggle.onValueChanged.AddListener(skillHandlers[i]);
        }

        session = GameSession.GetOrCreate();
        session.GameDataChanged += Refresh;
        InventoryManager.InstanceChanged += BindStock;
        BindStock();
        search.SetSeriesControlAvailable(true);
        inventory.Init();
    }

    private void OnDisable()
    {
        previousEgg.onClick.RemoveListener(SelectPreviousEgg);
        nextEgg.onClick.RemoveListener(SelectNextEgg);
        for (int i = 0; i < boardSkills.Length; i++)
            boardSkills[i].toggle.onValueChanged.RemoveListener(skillHandlers[i]);
        if (session != null) session.GameDataChanged -= Refresh;
        InventoryManager.InstanceChanged -= BindStock;
        if (stock != null) stock.onInventoryChanged -= Refresh;
        stock = null;
        selectedSlot = -1;
        editingPin = false;
    }

    private void BindSlots(BagEquipmentSlotUI[] slots, bool pin)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            int index = i;
            slots[i].Bind(() => SelectSlot(pin, index), tooltip: itemTooltip, onDeselected: () =>
            {
                if (selectedSlot != index || editingPin != pin) return;
                selectedSlot = -1;
                Refresh();
            });
        }
    }

    private void BindStock()
    {
        if (stock != null) stock.onInventoryChanged -= Refresh;
        stock = materialInventory != null ? materialInventory : InventoryManager.instance;
        inventory.SetInventorySource(stock);
        if (stock != null) stock.onInventoryChanged += Refresh;
        Refresh();
    }

    private void SelectSlot(bool pin, int index)
    {
        bool categoryChanged = editingPin != pin;
        editingPin = pin;
        selectedSlot = index;
        inventory.ClearSelection();
        if (categoryChanged)
        {
            // Pins have no merge-item series.
            search.SetSeriesControlAvailable(!pin);
            if (!pin) inventory.RefreshUI();
        }
    }

    public bool CanDisplay(ItemData item)
        => editingPin ? item is BoardPinData : BoardLoadout.AcceptsMaterial(item);

    public void SelectItem(ItemData item)
    {
        if (!CanDisplay(item) || stock == null || stock.GetItemAmount(item) <= 0 || string.IsNullOrWhiteSpace(item.dataId))
            return;

        var equipment = EquipmentSave.Current.Copy();
        var board = GetOrCreateBoard(equipment, GetUnlocked());
        var ids = editingPin ? board.pinIds : equipment.itemIds;
        int capacity = editingPin ? pinSlots.Length : materialSlots.Length;
        int index = selectedSlot;
        if (index < 0)
        {
            for (int i = 0; i < capacity; i++)
                if (FindOwned(ReadId(ids, i), editingPin) == null) { index = i; break; }
        }
        if (index < 0 || index >= capacity || ReadId(ids, index) == item.dataId) return;

        int equipped = 0;
        for (int i = 0; i < capacity; i++)
            if (i != index && ReadId(ids, i) == item.dataId) equipped++;
        // A material drop lane uses that item's full round allocation. A pin slot uses one copy.
        if (editingPin && equipped >= stock.GetItemAmount(item)) return;
        if (!editingPin && equipped > 0)
        {
            int previous = ids.IndexOf(item.dataId);
            ids[previous] = ReadId(ids, index) ?? string.Empty;
        }

        while (ids.Count <= index) ids.Add(string.Empty);
        ids[index] = item.dataId;
        if (Write(equipment))
        {
            selectedSlot = index;
            Refresh();
        }
    }

    private void SetSkill(SkillOption option, bool selected)
    {
        var equipment = EquipmentSave.Current.Copy();
        var board = GetOrCreateBoard(equipment, GetUnlocked());
        var unlocked = GetUnlocked();
        int number = option.data.boxIndex;
        if (!EquipmentSave.IsChestUnlocked(number, unlocked)) { Refresh(); return; }

        board.skillIndices.RemoveAll(index => !EquipmentSave.IsChestUnlocked(index, unlocked));
        if (selected && !board.skillIndices.Contains(number))
        {
            if (board.skillIndices.Count >= MergeGimicManager.MaxActiveModules) { Refresh(); return; }
            board.skillIndices.Add(number);
        }
        if (!selected) board.skillIndices.Remove(number);
        if (!Write(equipment)) Refresh();
    }

    private void SelectPreviousEgg() => CycleEgg(-1);
    private void SelectNextEgg() => CycleEgg(1);

    private void CycleEgg(int direction)
    {
        var equipment = EquipmentSave.Current.Copy();
        var board = GetOrCreateBoard(equipment, GetUnlocked());
        var unlocked = GetUnlocked();
        int current = EquipmentSave.ResolveEgg(equipment, unlocked)?.boxIndex ?? 1;
        for (int step = 1; step < boardSkills.Length; step++)
        {
            int index = (current - 1 + direction * step + boardSkills.Length) % boardSkills.Length;
            var candidate = boardSkills[index].data;
            if (!EquipmentSave.IsChestUnlocked(candidate.boxIndex, unlocked) || candidate.plant == null) continue;
            board.eggBoxIndex = candidate.boxIndex;
            if (Write(equipment))
            {
                itemTooltip?.Hide();
                Refresh();
            }
            return;
        }
    }

    private static BoardLoadout GetOrCreateBoard(EquipmentData equipment, IReadOnlyList<int> unlocked)
    {
        if (equipment.board?.IsConfigured == true) return equipment.board;
        var chest = EquipmentSave.ResolveChest(equipment, unlocked);
        int current = chest != null ? chest.boxIndex : 1;
        return equipment.board = new BoardLoadout { eggBoxIndex = current, skillIndices = new List<int> { current } };
    }

    private IReadOnlyList<int> GetUnlocked() => session != null ? session.UnlockedMergeBoxes : null;

    private bool Write(EquipmentData equipment)
    {
        if (!EquipmentSave.Write(equipment)) return false;
        session.UpdateEquipment(equipment);
        return true;
    }

    private void Refresh()
    {
        var equipment = EquipmentSave.Current;
        var board = GetOrCreateBoard(equipment.Copy(), GetUnlocked());
        var unlocked = GetUnlocked();
        RefreshSlots(materialSlots, equipment.itemIds, false);
        RefreshSlots(pinSlots, board.pinIds, true);
        var egg = EquipmentSave.ResolveEgg(equipment, unlocked);
        var plant = egg != null ? egg.plant : null;
        eggSlot?.Show(plant, false, displayIcon: plant != null ? plant.GetGrowthSprite(0) ?? egg.icon : null);

        bool canChangeEgg = false;
        int equippedSkillCount = board.skillIndices.FindAll(index => EquipmentSave.IsChestUnlocked(index, unlocked)).Count;
        foreach (var option in boardSkills)
        {
            var data = option.data;
            bool available = EquipmentSave.IsChestUnlocked(data.boxIndex, unlocked);
            bool selected = available && board.skillIndices.Contains(data.boxIndex);
            option.toggle.interactable = available && (selected || equippedSkillCount < MergeGimicManager.MaxActiveModules);
            option.toggle.SetIsOnWithoutNotify(selected);
            if (option.selected != null) option.selected.SetActive(selected);
            if (option.authoredColors != null)
                for (int i = 0; i < option.skillFrames.Length; i++)
                {
                    var frame = option.skillFrames[i];
                    if (frame == null) continue;
                    bool skillUnlocked = available && MergeBoxUpgradeManager.IsBoardSkillUnlocked(
                        data.boxIndex, i + 1, session?.UnlockedSkillNodes);
                    var color = option.authoredColors[i];
                    if (!skillUnlocked) color.a *= 0.32f;
                    frame.color = color;
                }
            canChangeEgg |= available && data.plant != null && data != egg;
        }
        previousEgg.interactable = canChangeEgg;
        nextEgg.interactable = canChangeEgg;
    }

    private void RefreshSlots(BagEquipmentSlotUI[] slots, List<string> ids, bool pin)
    {
        var used = pin ? new Dictionary<ItemData, int>() : null;
        for (int i = 0; i < slots.Length; i++)
        {
            var item = FindOwned(ReadId(ids, i), pin);
            if (used != null && item != null)
            {
                used.TryGetValue(item, out int count);
                if (count >= stock.GetItemAmount(item)) item = null;
                else used[item] = count + 1;
            }
            var sprite = item is BoardPinData definition ? definition.boardIcon : null;
            slots[i].Show(item, editingPin == pin && selectedSlot == i, displayIcon: sprite);
        }
    }

    private ItemData FindOwned(string id, bool pin)
    {
        if (string.IsNullOrEmpty(id) || stock == null) return null;
        foreach (var entry in stock.items)
            if (entry?.itemData is { } item && entry.amount > 0 && item.dataId == id &&
                (pin ? item is BoardPinData : BoardLoadout.AcceptsMaterial(item))) return item;
        return null;
    }

    private static string ReadId(List<string> ids, int index) => index < ids.Count ? ids[index] : null;
}
