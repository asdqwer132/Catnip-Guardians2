using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : ItemSearchFilterTargetUI
{
    [Header("Quick Inventory")]
    public Transform slotParent;

    [Header("Quick Inventory Slot")]
    public GameObject slotPrefab;
    [Tooltip("Show owned quantities on inventory item slots.")]
    public bool showItemCount = true;

    [Header("Tooltip")]
    public ItemTooltipUI tooltipUI;
    [Tooltip("Pin item information when an inventory slot Toggle is selected. Disable to keep hover tooltips.")]
    public bool showTooltipOnSelection;
    public ToggleGroup selectionGroup;

    [Header("Material Returns")]
    [Tooltip("Optional material manager. Empty uses this inventory's parent combination or sub-plant panel.")]
    public ItemRecipeManager materialSource;

    private bool isInitialized;
    private InventoryManager boundInventory;
    private ItemRecipeManager boundMaterialSource;

    private readonly List<InventoryItem> validItemsCache = new List<InventoryItem>();

    private readonly List<GameObject> quickSlotObjects = new List<GameObject>();
    private readonly List<BaseItemSlotUI> quickSlotUIs = new List<BaseItemSlotUI>();

    protected virtual bool ListenForMaterialReturns => true;
    protected virtual bool CanDisplayItem(InventoryItem item) => IsInventoryItemVisible(item);
    protected virtual void ConfigureSlot(BaseItemSlotUI slot) { }

    public virtual void Init()
    {
        BindMaterialReturns();
        BindInventoryEvent();
        if (isInitialized)
        {
            RefreshUI();
            return;
        }

        isInitialized = true;
        RefreshUI();
    }

    protected virtual void OnDestroy()
    {
        UnbindMaterialReturns();
        UnbindInventoryEvent();
    }

    protected virtual void OnEnable()
    {
        BindMaterialReturns();
        InventoryManager.InstanceChanged += OnInventorySourceChanged;
        if (isInitialized) { BindInventoryEvent(); RefreshUI(); }
    }

    protected virtual void OnDisable()
    {
        InventoryManager.InstanceChanged -= OnInventorySourceChanged;
        UnbindInventoryEvent();
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
        if (!ListenForMaterialReturns) return;
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

    private void OnInventorySourceChanged()
    {
        BindInventoryEvent();
        if (isInitialized) RefreshUI();
    }
    private void BindInventoryEvent()
    {
        if (boundInventory == InventoryManager.instance) return;
        UnbindInventoryEvent();
        boundInventory = InventoryManager.instance;
        if (boundInventory != null) boundInventory.onInventoryChanged += RefreshUI;
    }
    private void UnbindInventoryEvent()
    {
        if (boundInventory != null) boundInventory.onInventoryChanged -= RefreshUI;
        boundInventory = null;
    }

    protected override void OnSearchFilterChanged()
    {
        RefreshUI();
    }

    protected IReadOnlyList<BaseItemSlotUI> Slots => quickSlotUIs;
    protected virtual int DisplayItemCount => validItemsCache.Count;
    protected virtual void CollectDisplayItems() => GetValidItems(validItemsCache);
    protected virtual void BindSlot(BaseItemSlotUI slot, int index) => slot.SetSlot(validItemsCache[index]);
    protected virtual void OnSlotsRefreshed() { }

    public virtual void RefreshUI()
    {
        CollectDisplayItems();
        RefreshQuickInventory(DisplayItemCount);
        OnSlotsRefreshed();
    }

    private void GetValidItems(List<InventoryItem> result)
    {
        result.Clear();

        if (InventoryManager.instance == null)
            return;

        foreach (InventoryItem item in InventoryManager.instance.items)
        {
            if (!CanDisplayItem(item))
                continue;

            result.Add(item);
        }
    }

    private void RefreshQuickInventory(int count)
    {
        if (slotParent == null || slotPrefab == null)
            return;

        EnsureQuickSlots(count);

        for (int i = 0; i < quickSlotObjects.Count; i++)
        {
            bool active = i < count;

            quickSlotObjects[i].SetActive(active);

            if (quickSlotUIs[i] == null)
                continue;

            if (active)
                BindSlot(quickSlotUIs[i], i);
            else
                quickSlotUIs[i].SetSlot(null);
            quickSlotUIs[i].SetAmountVisible(showItemCount);
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

            ConfigureSlot(slotUI);
            quickSlotObjects.Add(slotObj);
            quickSlotUIs.Add(slotUI);
        }
    }
}
