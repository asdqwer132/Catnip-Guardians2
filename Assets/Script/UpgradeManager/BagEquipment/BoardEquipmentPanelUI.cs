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
        public Image[] skillFrames = Array.Empty<Image>();
        public Image[] skillIcons = Array.Empty<Image>();
        public BoardSkillTooltipProvider[] skillTooltips = Array.Empty<BoardSkillTooltipProvider>();
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

    [Header("Pinball Layout")]
    [SerializeField] private RectTransform pinballLayoutArea;
    [SerializeField] private PinballBoardLayout pinballLayout;

#if UNITY_EDITOR
    [ContextMenu("Capture Pinball Layout")]
    private void CapturePinballLayout()
    {
        if (Application.isPlaying || pinballLayout == null || pinballLayoutArea == null) return;
        if (!pinballLayoutArea.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("Show the equipment board in the scene before capturing its pin layout.", this);
            return;
        }
        Canvas.ForceUpdateCanvases();
        pinballLayout.Capture(pinballLayoutArea, pinSlots);
    }
#endif

    private GameSession session;
    private InventoryManager stock;
    private UnityAction<bool>[] skillHandlers;
    private bool editingPin;
    private int selectedSlot = -1;

    public void InitializeLoadout(IReadOnlyList<string> unlocked)
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
            skillHandlers[i] = selected => SetSkill(option, selected);
            option.toggle.onValueChanged.AddListener(skillHandlers[i]);
        }

        session = GameSession.GetOrCreate();
        session.GameDataChanged += Refresh;
        InventoryManager.InstanceChanged += BindStock;
        BindStock();
        search.SetTargetUI(inventory);
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
            }, onUnequip: () => UnequipSlot(pin, index));
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
        editingPin = pin;
        selectedSlot = index;
        inventory.ClearSelection();
        search.OnCategoryChanged((int)(pin ? DataType.Pin : DataType.ItemMaterial));
    }

    private void UnequipSlot(bool pin, int index)
    {
        var equipment = EquipmentSave.Current.Copy();
        var ids = pin ? equipment.board?.pinIds : equipment.itemIds;
        if (ids != null && index < ids.Count && !string.IsNullOrEmpty(ids[index]))
        {
            ids[index] = string.Empty;
            if (!Write(equipment)) return;
        }

        selectedSlot = -1;
        inventory.ClearSelection();
        itemTooltip?.Hide();
        Refresh();
    }

    public int GetAvailableAmount(InventoryItem item)
    {
        var equipment = EquipmentSave.Current;
        string id = item.itemData.dataId;
        var ids = item.itemData is BoardPinData ? equipment.board?.pinIds : equipment.itemIds;
        int available = item.amount;
        if (ids != null)
            foreach (string equippedId in ids)
                if (equippedId == id) available--;
        return Mathf.Max(0, available);
    }

    public bool CanDisplay(ItemData item) => item is BoardPinData || BoardLoadout.AcceptsMaterial(item);

    public void SelectItem(ItemData item)
    {
        if (!CanDisplay(item) || stock == null || stock.GetItemAmount(item) <= 0 || string.IsNullOrWhiteSpace(item.dataId))
            return;
        bool pin = item is BoardPinData;
        if (selectedSlot >= 0 && editingPin != pin) return;
        editingPin = pin;

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

        if (GetAvailableAmount(new InventoryItem(item, stock.GetItemAmount(item))) <= 0) return;

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
        var unlocked = GetUnlocked();
        var board = GetOrCreateBoard(equipment, unlocked);
        int number = option.data.boxIndex;
        if (!EquipmentUnlocks.HasBox(unlocked, number)) { Refresh(); return; }

        board.skillIndices.RemoveAll(index => !EquipmentUnlocks.HasBox(unlocked, index));
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
        var unlocked = GetUnlocked();
        var board = GetOrCreateBoard(equipment, unlocked);
        int current = EquipmentSave.ResolveEgg(equipment, unlocked)?.boxIndex ?? 1;
        int count = EquipmentUnlocks.BoxCount;
        for (int step = 1; step < count; step++)
        {
            int index = (current - 1 + direction * step + count) % count + 1;
            if (!EquipmentUnlocks.HasEgg(unlocked, index) || EquipmentSave.FindChest(index)?.plant == null) continue;
            board.eggBoxIndex = index;
            if (Write(equipment))
            {
                itemTooltip?.Hide();
                Refresh();
            }
            return;
        }
    }

    private static BoardLoadout GetOrCreateBoard(EquipmentData equipment, IReadOnlyList<string> unlocked)
    {
        if (equipment.board?.IsConfigured == true) return equipment.board;
        var board = new BoardLoadout { eggBoxIndex = EquipmentSave.ResolveEgg(equipment, unlocked)?.boxIndex ?? BoardLoadout.NoEgg };
        foreach (int index in equipment.equippedChests)
        {
            if (!EquipmentUnlocks.HasBox(unlocked, index) || board.skillIndices.Contains(index)) continue;
            board.skillIndices.Add(index);
            if (board.skillIndices.Count == MergeGimicManager.MaxActiveModules) break;
        }
        return equipment.board = board;
    }

    private IReadOnlyList<string> GetUnlocked() => session?.EquipmentUnlockKeys;

    private bool Write(EquipmentData equipment)
    {
        if (!EquipmentSave.Write(equipment)) return false;
        session.UpdateEquipment(equipment);
        return true;
    }

    private void Refresh()
    {
        var equipment = EquipmentSave.Current;
        inventory.RefreshUI();
        var board = GetOrCreateBoard(equipment.Copy(), GetUnlocked());
        var unlocked = GetUnlocked();
        RefreshSlots(materialSlots, equipment.itemIds, false);
        RefreshSlots(pinSlots, board.pinIds, true);
        var egg = EquipmentSave.ResolveEgg(equipment, unlocked);
        var plant = egg != null ? egg.plant : null;
        eggSlot?.Show(plant, false, displayIcon: plant != null ? plant.GetGrowthSprite(0) ?? egg.icon : null);

        bool canChangeEgg = false;
        for (int index = 1; index <= EquipmentUnlocks.BoxCount; index++)
        {
            if (index == egg?.boxIndex || !EquipmentUnlocks.HasEgg(unlocked, index)) continue;
            if (EquipmentSave.FindChest(index)?.plant == null) continue;
            canChangeEgg = true;
            break;
        }

        int equippedSkillCount = board.skillIndices.FindAll(index => EquipmentUnlocks.HasBox(unlocked, index)).Count;
        foreach (var option in boardSkills)
        {
            var data = option.data;
            bool available = EquipmentUnlocks.HasBox(unlocked, data.boxIndex);
            bool selected = available && board.skillIndices.Contains(data.boxIndex);
            option.toggle.interactable = available && (selected || equippedSkillCount < MergeGimicManager.MaxActiveModules);
            option.toggle.SetIsOnWithoutNotify(selected);
            if (option.selected != null) option.selected.SetActive(selected);
            option.toggle.gameObject.SetActive(available);
            for (int i = 0; i < option.skillFrames.Length; i++)
            {
                var frame = option.skillFrames[i];
                if (frame == null) continue;
                var skill = i < data.skills.Length ? data.skills[i] : null;
                bool visible = available && skill != null && EquipmentUnlocks.HasSkill(unlocked, data.boxIndex, i + 1);
                if (i < option.skillIcons.Length && option.skillIcons[i] != null)
                {
                    var icon = option.skillIcons[i];
                    icon.sprite = skill != null ? skill.icon : null;
                    icon.enabled = icon.sprite != null;
                }
                if (i < option.skillTooltips.Length) option.skillTooltips[i]?.SetSource(skill);
                frame.gameObject.SetActive(visible);
            }
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
