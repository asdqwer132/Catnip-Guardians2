using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class InventoryUI : ItemSearchFilterTargetUI
{
    [Header("Quick Inventory")]
    public Transform slotParent;

    [Header("Quick Inventory Slot")]
    public GameObject slotPrefab;

    [Header("Tooltip")]
    public ItemTooltipUI tooltipUI;
    [Tooltip("Pin item information when an inventory slot Toggle is selected. Disable to keep hover tooltips.")]
    public bool showTooltipOnSelection;
    public ToggleGroup selectionGroup;

    [Header("Material Returns")]
    [Tooltip("Optional material manager. Empty uses this inventory's parent combination or sub-plant panel.")]
    public ItemRecipeManager materialSource;

    private bool isInitialized;
    private bool isEventBound;
    private ItemRecipeManager boundMaterialSource;

    private readonly List<InventoryItem> validItemsCache = new List<InventoryItem>();

    private readonly List<GameObject> quickSlotObjects = new List<GameObject>();
    private readonly List<BaseItemSlotUI> quickSlotUIs = new List<BaseItemSlotUI>();

    public void Init()
    {
        BindMaterialReturns();
        if (isInitialized)
        {
            RefreshUI();
            return;
        }

        BindInventoryEvent();

        isInitialized = true;
        RefreshUI();
    }

    private void OnDestroy()
    {
        UnbindMaterialReturns();
        UnbindInventoryEvent();
    }

    private void OnEnable() => BindMaterialReturns();

    private void OnDisable()
    {
        UnbindMaterialReturns();
        foreach (var row in quickSlotObjects)
            if (row != null && row.TryGetComponent<ItemTooltipTrigger>(out var trigger))
                trigger.ReleaseSelection(true);
    }

    public void ClearSelection()
    {
        foreach (var row in quickSlotUIs)
            if (row is ClickableItemSlotUI slot) slot.ResetSelection();
    }

    private void BindMaterialReturns()
    {
        ItemRecipeManager source = materialSource;
        if (source == null)
        {
            var panel = GetComponentInParent<ItemCombinationPanelUI>(true);
            if (panel != null) source = panel.Manager;
            else
            {
                var legacy = GetComponentInParent<SubPlantUIManager>(true);
                if (legacy != null) source = legacy.subplantmanager;
            }
        }
        if (boundMaterialSource == source) return;
        UnbindMaterialReturns();
        if (source == null || !isActiveAndEnabled) return;
        boundMaterialSource = source;
        boundMaterialSource.MaterialsReturned += ClearSelection;
    }

    private void UnbindMaterialReturns()
    {
        if (boundMaterialSource != null) boundMaterialSource.MaterialsReturned -= ClearSelection;
        boundMaterialSource = null;
    }

    private void BindInventoryEvent()
    {
        if (isEventBound)
            return;

        if (InventoryManager.instance == null)
            return;

        InventoryManager.instance.onInventoryChanged += RefreshUI;
        isEventBound = true;
    }

    private void UnbindInventoryEvent()
    {
        if (!isEventBound)
            return;

        if (InventoryManager.instance != null)
            InventoryManager.instance.onInventoryChanged -= RefreshUI;

        isEventBound = false;
    }

    protected override void OnSearchFilterChanged()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (InventoryManager.instance == null)
            return;

        GetValidItems(validItemsCache);

        RefreshQuickInventory(validItemsCache);
    }

    private void GetValidItems(List<InventoryItem> result)
    {
        result.Clear();

        if (InventoryManager.instance == null)
            return;

        foreach (InventoryItem item in InventoryManager.instance.items)
        {
            if (!IsInventoryItemVisible(item))
                continue;

            result.Add(item);
        }
    }

    private void RefreshQuickInventory(List<InventoryItem> validItems)
    {
        if (slotParent == null || slotPrefab == null)
            return;

        EnsureQuickSlots(validItems.Count);

        for (int i = 0; i < quickSlotObjects.Count; i++)
        {
            bool active = i < validItems.Count;

            quickSlotObjects[i].SetActive(active);

            if (quickSlotUIs[i] == null)
                continue;

            if (active)
                quickSlotUIs[i].SetSlot(validItems[i]);
            else
                quickSlotUIs[i].SetSlot(null);
            var trigger = quickSlotObjects[i].GetComponent<ItemTooltipTrigger>();
            if (trigger != null)
            {
                if (trigger.showOnSelection && !showTooltipOnSelection) trigger.ReleaseSelection();
                trigger.showOnSelection = showTooltipOnSelection;
            }
        }
    }

    private void EnsureQuickSlots(int count)
    {
        while (quickSlotObjects.Count < count)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotParent);

            BaseItemSlotUI slotUI = slotObj.GetComponent<BaseItemSlotUI>();
            ItemTooltipTrigger tooltipTrigger = slotObj.GetComponent<ItemTooltipTrigger>();

            if (tooltipTrigger != null)
            {
                tooltipTrigger.Init(tooltipUI);
                tooltipTrigger.showOnSelection = showTooltipOnSelection;
            }
            if (slotObj.TryGetComponent<Toggle>(out var toggle))
            {
                toggle.group = selectionGroup != null ? selectionGroup : GetComponent<ToggleGroup>();
                toggle.onValueChanged.AddListener(selected =>
                {
                    if (!selected) return;
                    foreach (var row in quickSlotUIs)
                        if (row is ClickableItemSlotUI other && other.slotToggle != toggle) other.ResetSelection();
                });
            }

            quickSlotObjects.Add(slotObj);
            quickSlotUIs.Add(slotUI);
        }
    }
}
