using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class InventoryUI : ItemSearchFilterTargetUI
{
    [Header("Quick Inventory")]
    [FormerlySerializedAs("quickPageParent")]
    public Transform quickSlotParent;

    [Header("Quick Inventory Slot")]
    public GameObject quickSlotPrefab;

    [Header("Detail Inventory")]
    public Transform detailSlotParent;
    public GameObject detailSlotPrefab;

    [Header("Tooltip")]
    public ItemTooltipUI tooltipUI;

    private bool isInitialized;
    private bool isEventBound;

    private readonly List<InventoryItem> validItemsCache = new List<InventoryItem>();

    private readonly List<GameObject> quickSlotObjects = new List<GameObject>();
    private readonly List<BaseItemSlotUI> quickSlotUIs = new List<BaseItemSlotUI>();

    private readonly List<GameObject> detailSlotObjects = new List<GameObject>();
    private readonly List<BaseItemSlotUI> detailSlotUIs = new List<BaseItemSlotUI>();

    public void Init()
    {
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
        UnbindInventoryEvent();
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
        RefreshDetailInventory(validItemsCache);
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
        if (quickSlotParent == null || quickSlotPrefab == null)
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
        }
    }

    private void EnsureQuickSlots(int count)
    {
        while (quickSlotObjects.Count < count)
        {
            GameObject slotObj = Instantiate(quickSlotPrefab, quickSlotParent);

            BaseItemSlotUI slotUI = slotObj.GetComponent<BaseItemSlotUI>();
            ItemTooltipTrigger tooltipTrigger = slotObj.GetComponent<ItemTooltipTrigger>();

            if (tooltipTrigger != null)
                tooltipTrigger.Init(tooltipUI);

            quickSlotObjects.Add(slotObj);
            quickSlotUIs.Add(slotUI);
        }
    }

    private void RefreshDetailInventory(List<InventoryItem> validItems)
    {
        if (detailSlotParent == null || detailSlotPrefab == null)
            return;

        EnsureDetailSlots(validItems.Count);

        for (int i = 0; i < detailSlotObjects.Count; i++)
        {
            bool active = i < validItems.Count;

            detailSlotObjects[i].SetActive(active);

            if (detailSlotUIs[i] == null)
                continue;

            if (active)
                detailSlotUIs[i].SetSlot(validItems[i]);
            else
                detailSlotUIs[i].SetSlot(null);
        }
    }

    private void EnsureDetailSlots(int count)
    {
        while (detailSlotObjects.Count < count)
        {
            GameObject slotObj = Instantiate(detailSlotPrefab, detailSlotParent);

            BaseItemSlotUI slotUI = slotObj.GetComponent<BaseItemSlotUI>();
            ItemTooltipTrigger tooltipTrigger = slotObj.GetComponent<ItemTooltipTrigger>();

            if (tooltipTrigger != null)
                tooltipTrigger.Init(tooltipUI);

            detailSlotObjects.Add(slotObj);
            detailSlotUIs.Add(slotUI);
        }
    }
}