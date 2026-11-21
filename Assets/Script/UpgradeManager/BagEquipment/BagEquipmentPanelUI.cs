using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class BagEquipmentPanelUI : MonoBehaviour
{
    public BagEquipmentInventoryUI inventory;
    public BagEquipmentSlotUI[] itemSlots = Array.Empty<BagEquipmentSlotUI>();
    public BagEquipmentSlotUI[] chestSlots = Array.Empty<BagEquipmentSlotUI>();
    public BagEquipmentSlotUI[] chestChoices = Array.Empty<BagEquipmentSlotUI>();
    public BagEquipmentSlotUI playerSlot;
    public PlayerChoice[] playerChoices = Array.Empty<PlayerChoice>();
    public TooltipUI chestTooltip, playerTooltip;
    public MergeBoxEquipmentData[] chestData = Array.Empty<MergeBoxEquipmentData>();
    public BasicTooltipUI equippedDetails;
    public BasicTooltipUI candidateDetails;
    public GameObject comparison;
    public GameObject guide;
    public GameObject confirmArea;
    public GameObject clearArea;
    public GameObject comparisonArrow;
    public Button equipButton, unequipButton, clearButton;

    [Serializable]
    public sealed class PlayerChoice
    {
        public PlayerEquipmentSlotUI slot;
        public PlayerData player;
    }

    private enum SlotType { None, Material, Chest, Player }
    private SlotType selectedType;
    private int selectedSlot = -1;
    private DefaultData candidate;
    private bool inputsBound;
    private RefreshBroadcaster broadcaster;
    private InventoryManager observedInventory;

    // Bind inputs once even if the equipment tab is hidden at scene startup.
    private void OnEnable()
    {
        if (!inputsBound)
        {
            inputsBound = true;
            for (int i = 0; i < itemSlots.Length; i++)
            {
                int index = i;
                itemSlots[i]?.Bind(() => PickSlot(SlotType.Material, index), index + 1);
            }
            for (int i = 0; i < chestSlots.Length; i++)
            {
                int index = i;
                chestSlots[i]?.Bind(() => PickSlot(SlotType.Chest, index), index + 1);
            }
            for (int i = 0; i < chestChoices.Length; i++)
            {
                int number = chestData.Length > i && chestData[i] != null ? chestData[i].boxIndex : 0;
                chestChoices[i]?.Bind(() => SelectChest(number), tooltip: chestTooltip);
            }
            playerSlot?.Bind(() => PickSlot(SlotType.Player, 0));
            foreach (var choice in playerChoices)
                choice?.slot?.Bind(() => SelectPlayer(choice.player), tooltip: playerTooltip);
        }
        if (equipButton != null) equipButton.onClick.AddListener(Confirm);
        if (unequipButton != null) unequipButton.onClick.AddListener(Unequip);
        if (clearButton != null) clearButton.onClick.AddListener(ClearLoadout);
        BindBroadcaster();
        InventoryManager.InstanceChanged += BindInventory;
        BindInventory();
        inventory?.Init();
        Refresh();
    }

    private void Start() => BindBroadcaster();

    private void BindBroadcaster()
    {
        if (broadcaster == RefreshBroadcaster.Instance) return;
        if (broadcaster != null) broadcaster.OnRefreshRequested -= RefreshFromBroadcast;
        broadcaster = RefreshBroadcaster.Instance;
        if (broadcaster != null) broadcaster.OnRefreshRequested += RefreshFromBroadcast;
    }

    private void BindInventory()
    {
        if (observedInventory != null) observedInventory.onInventoryChanged -= Refresh;
        observedInventory = InventoryManager.instance;
        if (observedInventory != null) observedInventory.onInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (equipButton != null) equipButton.onClick.RemoveListener(Confirm);
        if (unequipButton != null) unequipButton.onClick.RemoveListener(Unequip);
        if (clearButton != null) clearButton.onClick.RemoveListener(ClearLoadout);
        if (broadcaster != null) broadcaster.OnRefreshRequested -= RefreshFromBroadcast;
        broadcaster = null;
        InventoryManager.InstanceChanged -= BindInventory;
        if (observedInventory != null) observedInventory.onInventoryChanged -= Refresh;
        observedInventory = null;
        ResetSelection();
    }

    private void RefreshFromBroadcast(RefreshType type)
    {
        if ((type & (RefreshType.Unlock | RefreshType.Equipment | RefreshType.Inventory)) != 0) Refresh();
    }

    private static bool Unlocked(int number) => UnlockManager.Instance != null
        ? UnlockManager.Instance.IsUnlocked(DataType.MergeBox, number.ToString()) : number == 1;

    public void SelectMaterial(ItemData item)
    {
        if (!OwnsMaterial(item)) return;
        var data = EquipmentSave.Current;
        if (data.itemIds.Contains(item.dataId))
        {
            int equippedIndex = data.itemIds.IndexOf(item.dataId);
            if (equippedIndex < itemSlots.Length) PickSlot(SlotType.Material, equippedIndex);
            return;
        }

        int slot = selectedType == SlotType.Material ? selectedSlot : FirstEmptyMaterial(data);
        if (slot < 0) { Refresh(); return; }
        SelectCandidate(SlotType.Material, slot, item, FindItem(ItemId(data, slot)));
    }

    public void SelectChest(int number)
    {
        var chest = FindChest(number);
        if (chest == null || !Unlocked(number)) return;
        chestTooltip?.Show(chest);

        var data = EquipmentSave.Current;
        int existing = data.equippedChests.IndexOf(number);
        if (existing >= 0 && existing < chestSlots.Length) { PickSlot(SlotType.Chest, existing); return; }

        int slot = selectedType == SlotType.Chest ? selectedSlot : FirstEmptyChest(data);
        if (slot < 0) { Refresh(); return; }
        SelectCandidate(SlotType.Chest, slot, chest, FindChest(ChestNumber(data, slot)));
    }

    public void SelectPlayer(PlayerData player)
    {
        if (playerSlot == null || player == null || string.IsNullOrWhiteSpace(player.dataId)) return;
        playerTooltip?.Show(player);
        var equipped = FindPlayer(EquipmentSave.Current.battlePlayerId);
        if (player == equipped) { PickSlot(SlotType.Player, 0); return; }
        SelectCandidate(SlotType.Player, 0, player, equipped);
    }

    private void SelectCandidate(SlotType type, int index, DefaultData next, DefaultData equipped)
    {
        selectedType = type;
        selectedSlot = index;
        candidate = next;
        if (equipped == null) Confirm();
        else Refresh();
    }

    private void PickSlot(SlotType type, int index)
    {
        selectedType = type;
        selectedSlot = index;
        candidate = null;
        Refresh();
    }

    private bool CanEquip(EquipmentData data)
    {
        if (selectedSlot < 0) return false;
        return selectedType switch
        {
            SlotType.Material => selectedSlot < itemSlots.Length && candidate is ItemData item &&
                OwnsMaterial(item) && !data.itemIds.Contains(item.dataId),
            SlotType.Chest => selectedSlot < Mathf.Min(2, chestSlots.Length) && candidate is MergeBoxEquipmentData box &&
                Unlocked(box.boxIndex) && !data.equippedChests.Contains(box.boxIndex),
            SlotType.Player => playerSlot != null && candidate is PlayerData player &&
                !string.IsNullOrWhiteSpace(player.dataId) && data.battlePlayerId != player.dataId,
            _ => false
        };
    }

    public void Confirm()
    {
        var data = EquipmentSave.Current.Copy();
        if (!CanEquip(data)) return;

        switch (candidate)
        {
            case ItemData item:
                while (data.itemIds.Count <= selectedSlot) data.itemIds.Add("");
                data.itemIds[selectedSlot] = item.dataId;
                break;
            case MergeBoxEquipmentData box:
                while (data.equippedChests.Count <= selectedSlot) data.equippedChests.Add(0);
                data.equippedChests[selectedSlot] = box.boxIndex;
                break;
            case PlayerData player:
                data.battlePlayerId = player.dataId;
                break;
        }
        Apply(data);
    }

    public void Unequip()
    {
        if (selectedSlot < 0) return;
        var data = EquipmentSave.Current.Copy();
        if (selectedType == SlotType.Material && selectedSlot < data.itemIds.Count) data.itemIds[selectedSlot] = "";
        else if (selectedType == SlotType.Chest && selectedSlot < data.equippedChests.Count) data.equippedChests[selectedSlot] = 0;
        else if (selectedType == SlotType.Player) data.battlePlayerId = null;
        else return;
        Apply(data);
    }

    public void ClearLoadout()
    {
        var data = EquipmentSave.Current.Copy();
        data.itemIds.Clear();
        data.equippedChests.Clear();
        data.battlePlayerId = null;
        Apply(data);
    }

    private void Apply(EquipmentData data)
    {
        if (!EquipmentSave.Write(data)) return;
        ResetSelection();
        GameSession.GetOrCreate().UpdateEquipment(data);
        Refresh();
    }

    private void ResetSelection()
    {
        selectedSlot = -1;
        selectedType = SlotType.None;
        candidate = null;
        inventory?.ClearSelection();
    }

    private void Refresh()
    {
        var data = EquipmentSave.Current;
        for (int i = 0; i < itemSlots.Length; i++)
            itemSlots[i]?.Show(FindItem(ItemId(data, i)), selectedType == SlotType.Material && selectedSlot == i);
        for (int i = 0; i < chestSlots.Length; i++)
            chestSlots[i]?.Show(FindChest(ChestNumber(data, i)), selectedType == SlotType.Chest && selectedSlot == i);
        for (int i = 0; i < chestChoices.Length; i++)
        {
            var choice = i < chestData.Length ? chestData[i] : null;
            chestChoices[i]?.Show(choice, choice != null && candidate == choice, choice == null || !Unlocked(choice.boxIndex));
        }

        var equippedPlayer = FindPlayer(data.battlePlayerId);
        playerSlot?.Show(equippedPlayer, selectedType == SlotType.Player && selectedSlot == 0);
        foreach (var choice in playerChoices)
            choice?.slot?.Show(choice.player, choice.player != null && candidate == choice.player, choice.player == null);

        DefaultData equipped = selectedType switch
        {
            SlotType.Material => FindItem(ItemId(data, selectedSlot)),
            SlotType.Chest => FindChest(ChestNumber(data, selectedSlot)),
            SlotType.Player => equippedPlayer,
            _ => null
        };
        bool hasSelection = selectedSlot >= 0;
        if (comparison != null) comparison.SetActive(hasSelection);
        if (guide != null) guide.SetActive(!hasSelection);
        if (confirmArea != null) confirmArea.SetActive(hasSelection);
        if (clearArea != null) clearArea.SetActive(!hasSelection);
        equippedDetails?.Show(equipped);
        candidateDetails?.Show(candidate);
        if (comparisonArrow != null) comparisonArrow.SetActive(equipped != null && candidate != null);
        if (equipButton != null) equipButton.interactable = CanEquip(data);
        if (unequipButton != null) unequipButton.interactable = equipped != null;
    }

    private PlayerData FindPlayer(string id)
        => string.IsNullOrEmpty(id) ? null : Array.Find(playerChoices, choice => choice?.player != null && choice.player.dataId == id)?.player
            ?? EquipmentSave.ResolveBattlePlayer(id);

    public bool IsMaterial(ItemData item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.dataId)) return false;
        if (item.dataType == DataType.ItemMaterial) return true;

        // Existing material assets can still be tagged Item in the data workbook.
        string prefix = item.series + "Material";
        return item.dataType == DataType.Item && MergeBoxLevelSnapshot.IsBoxSeries(item.series) &&
            item.dataId.StartsWith(prefix, StringComparison.Ordinal) &&
            int.TryParse(item.dataId.Substring(prefix.Length), out int number) && number > 0;
    }

    private bool OwnsMaterial(ItemData item)
        => IsMaterial(item)
            && InventoryManager.instance != null && InventoryManager.instance.GetItemAmount(item) > 0;

    private static string ItemId(EquipmentData data, int slot)
        => slot >= 0 && slot < data.itemIds.Count ? data.itemIds[slot] : null;

    private static int ChestNumber(EquipmentData data, int slot)
    {
        int number = slot >= 0 && slot < data.equippedChests.Count ? data.equippedChests[slot] : 0;
        return Unlocked(number) ? number : 0;
    }

    private static ItemData FindItem(string id)
    {
        if (string.IsNullOrEmpty(id) || InventoryManager.instance == null) return null;
        foreach (var item in InventoryManager.instance.items)
            if (item?.itemData != null && item.itemData.dataId == id && item.amount > 0) return item.itemData;
        return null;
    }

    private MergeBoxEquipmentData FindChest(int number)
        => Array.Find(chestData, chest => chest != null && chest.boxIndex == number);
    private int FirstEmptyMaterial(EquipmentData data)
    {
        for (int i = 0; i < itemSlots.Length; i++)
            if (string.IsNullOrEmpty(ItemId(data, i)) || FindItem(ItemId(data, i)) == null) return i;
        return -1;
    }

    private int FirstEmptyChest(EquipmentData data)
    {
        for (int i = 0; i < Mathf.Min(2, chestSlots.Length); i++)
            if (ChestNumber(data, i) == 0) return i;
        return -1;
    }
}
