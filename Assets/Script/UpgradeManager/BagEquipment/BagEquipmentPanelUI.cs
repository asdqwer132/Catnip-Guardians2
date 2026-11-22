using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class BagEquipmentPanelUI : MonoBehaviour
{
    public BagEquipmentInventoryUI inventory;
    public BagEquipmentSlotUI[] itemSlots = Array.Empty<BagEquipmentSlotUI>();
    public BasicTooltipUI equippedDetails;
    public BasicTooltipUI candidateDetails;
    public GameObject comparison;
    public GameObject guide;
    public GameObject confirmArea;
    public GameObject clearArea;
    public GameObject comparisonArrow;
    public Button equipButton, unequipButton, clearButton;

    private int selectedSlot = -1;
    private ItemData candidate;
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
                itemSlots[i]?.Bind(() => PickSlot(index), index + 1);
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
        ResetSelection();
    }

    private void RefreshFromBroadcast(RefreshType type)
    {
        if ((type & (RefreshType.Unlock | RefreshType.Equipment | RefreshType.Inventory)) != 0) Refresh();
    }

    public void SelectMaterial(ItemData item)
    {
        if (!OwnsMaterial(item)) return;
        var data = EquipmentSave.Current;
        if (data.itemIds.Contains(item.dataId))
        {
            int equippedIndex = data.itemIds.IndexOf(item.dataId);
            if (equippedIndex < itemSlots.Length) PickSlot(equippedIndex);
            return;
        }

        int slot = selectedSlot >= 0 ? selectedSlot : FirstEmptyMaterial(data);
        if (slot < 0) { Refresh(); return; }
        selectedSlot = slot;
        candidate = item;
        if (FindItem(ItemId(data, slot)) == null) Confirm();
        else Refresh();
    }

    private void PickSlot(int index)
    {
        selectedSlot = index;
        candidate = null;
        Refresh();
    }

    private bool CanEquip(EquipmentData data)
        => selectedSlot >= 0 && selectedSlot < itemSlots.Length && OwnsMaterial(candidate) &&
            !data.itemIds.Contains(candidate.dataId);

    public void Confirm()
    {
        var data = EquipmentSave.Current.Copy();
        if (!CanEquip(data)) return;

        while (data.itemIds.Count <= selectedSlot) data.itemIds.Add("");
        data.itemIds[selectedSlot] = candidate.dataId;
        Apply(data);
    }

    public void Unequip()
    {
        if (selectedSlot < 0) return;
        var data = EquipmentSave.Current.Copy();
        if (selectedSlot >= data.itemIds.Count) return;
        data.itemIds[selectedSlot] = "";
        Apply(data);
    }

    public void ClearLoadout()
    {
        var data = EquipmentSave.Current.Copy();
        data.itemIds.Clear();
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
        candidate = null;
        inventory?.ClearSelection();
    }

    private void Refresh()
    {
        var data = EquipmentSave.Current;
        for (int i = 0; i < itemSlots.Length; i++)
            itemSlots[i]?.Show(FindItem(ItemId(data, i)), selectedSlot == i);

        var equipped = FindItem(ItemId(data, selectedSlot));
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

    private static ItemData FindItem(string id)
    {
        if (string.IsNullOrEmpty(id) || InventoryManager.instance == null) return null;
        foreach (var item in InventoryManager.instance.items)
            if (item?.itemData != null && item.itemData.dataId == id && item.amount > 0) return item.itemData;
        return null;
    }

    private int FirstEmptyMaterial(EquipmentData data)
    {
        for (int i = 0; i < itemSlots.Length; i++)
            if (string.IsNullOrEmpty(ItemId(data, i)) || FindItem(ItemId(data, i)) == null) return i;
        return -1;
    }

}
