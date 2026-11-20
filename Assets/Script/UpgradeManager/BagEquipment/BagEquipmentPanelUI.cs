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
    public ItemBoxData[] chestData = Array.Empty<ItemBoxData>();
    public MergeRecipeBook[] materialRecipeBooks = Array.Empty<MergeRecipeBook>();
    public BasicTooltipUI equippedDetails;
    public BasicTooltipUI candidateDetails;
    public GameObject comparison;
    public GameObject guide;
    public GameObject confirmArea;
    public GameObject clearArea;
    public GameObject comparisonArrow;
    public Button equipButton, unequipButton, clearButton;

    private enum SlotType { None, Material, Chest }
    private SlotType selectedType;
    private int selectedSlot = -1;
    private ItemData materialCandidate;
    private int chestCandidate;
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
                int number = i + 1;
                chestChoices[i]?.Bind(() => SelectChest(number));
            }
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
        selectedType = SlotType.Material;
        selectedSlot = slot;
        materialCandidate = item;
        chestCandidate = 0;
        if (FindItem(ItemId(data, slot)) == null) Confirm();
        else Refresh();
    }

    public void SelectChest(int number)
    {
        if (!Unlocked(number)) return;
        var data = EquipmentSave.Current;
        int existing = data.equippedChests.IndexOf(number);
        if (existing >= 0 && existing < chestSlots.Length) { PickSlot(SlotType.Chest, existing); return; }

        int slot = selectedType == SlotType.Chest ? selectedSlot : FirstEmptyChest(data);
        if (slot < 0) { Refresh(); return; }
        selectedType = SlotType.Chest;
        selectedSlot = slot;
        chestCandidate = number;
        materialCandidate = null;
        if (ChestNumber(data, slot) == 0) Confirm();
        else Refresh();
    }

    private void PickSlot(SlotType type, int index)
    {
        selectedType = type;
        selectedSlot = index;
        materialCandidate = null;
        chestCandidate = 0;
        Refresh();
    }

    public void Confirm()
    {
        if (selectedSlot < 0) return;
        var data = EquipmentSave.Current.Copy();
        if (selectedType == SlotType.Material && selectedSlot < itemSlots.Length && OwnsMaterial(materialCandidate) &&
            !data.itemIds.Contains(materialCandidate.dataId))
        {
            while (data.itemIds.Count <= selectedSlot) data.itemIds.Add("");
            data.itemIds[selectedSlot] = materialCandidate.dataId;
        }
        else if (selectedType == SlotType.Chest && selectedSlot < Mathf.Min(2, chestSlots.Length) && Unlocked(chestCandidate) &&
            !data.equippedChests.Contains(chestCandidate))
        {
            while (data.equippedChests.Count <= selectedSlot) data.equippedChests.Add(0);
            data.equippedChests[selectedSlot] = chestCandidate;
        }
        else return;
        Apply(data);
    }

    public void Unequip()
    {
        if (selectedSlot < 0) return;
        var data = EquipmentSave.Current.Copy();
        if (selectedType == SlotType.Material && selectedSlot < data.itemIds.Count) data.itemIds[selectedSlot] = "";
        else if (selectedType == SlotType.Chest && selectedSlot < data.equippedChests.Count) data.equippedChests[selectedSlot] = 0;
        else return;
        Apply(data);
    }

    public void ClearLoadout()
    {
        var data = EquipmentSave.Current.Copy();
        data.itemIds.Clear();
        data.equippedChests = new List<int> { 1, 0 };
        Apply(data);
    }

    private void Apply(EquipmentData data)
    {
        if (!EquipmentSave.Write(data)) return;
        selectedSlot = -1;
        selectedType = SlotType.None;
        chestCandidate = 0;
        materialCandidate = null;
        inventory?.ClearSelection();
        GameSession.Instance?.UpdateEquipment(data);
        Refresh();
    }

    private void Refresh()
    {
        var data = EquipmentSave.Current;
        for (int i = 0; i < itemSlots.Length; i++)
            itemSlots[i]?.Show(FindItem(ItemId(data, i)), selectedType == SlotType.Material && selectedSlot == i);
        for (int i = 0; i < chestSlots.Length; i++)
            chestSlots[i]?.Show(FindChest(ChestNumber(data, i)), selectedType == SlotType.Chest && selectedSlot == i);
        for (int i = 0; i < chestChoices.Length; i++)
            chestChoices[i]?.Show(FindChest(i + 1), chestCandidate == i + 1, !Unlocked(i + 1));

        DefaultData equipped = selectedType == SlotType.Material ? FindItem(ItemId(data, selectedSlot)) :
            selectedType == SlotType.Chest ? FindChest(ChestNumber(data, selectedSlot)) : null;
        DefaultData candidate = selectedType == SlotType.Chest ? FindChest(chestCandidate) : materialCandidate;
        bool hasSelection = selectedSlot >= 0;
        if (comparison != null) comparison.SetActive(hasSelection);
        if (guide != null) guide.SetActive(!hasSelection);
        if (confirmArea != null) confirmArea.SetActive(hasSelection);
        if (clearArea != null) clearArea.SetActive(!hasSelection);
        equippedDetails?.Show(equipped);
        candidateDetails?.Show(candidate);
        if (comparisonArrow != null) comparisonArrow.SetActive(equipped != null && candidate != null);
        if (equipButton != null) equipButton.interactable = selectedType == SlotType.Chest
            ? candidate != null && Unlocked(chestCandidate)
            : OwnsMaterial(materialCandidate);
        if (unequipButton != null) unequipButton.interactable = equipped != null;
    }

    public bool IsMaterial(ItemData item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.dataId)) return false;
        foreach (var book in materialRecipeBooks)
            if (book != null && MergegameManager.TryGetPrefab(book.GetDefinition(item), out _)) return true;
        return false;
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

    private ItemBoxData FindChest(int number) => number > 0 && number <= chestData.Length ? chestData[number - 1] : null;
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
