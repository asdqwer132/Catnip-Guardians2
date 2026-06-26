using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : ItemSearchFilterTargetUI
{
    [Header("Quick Inventory Page")]
    public Transform quickPageParent;
    public GameObject quickPagePrefab;
    public int quickSlotsPerPage = 12;

    [Header("Quick Inventory Slot")]
    public GameObject quickSlotPrefab;

    [Header("Quick Scroll")]
    public SnapScroll snapScroll;

    [Header("Detail Inventory")]
    public Transform detailSlotParent;
    public GameObject detailSlotPrefab;

    [Header("Tooltip")]
    public ItemTooltipUI tooltipUI;

    [Header("Option")]
    public bool resetQuickPageOnRefresh = true;

    private bool isInitialized;
    private bool isEventBound;

    private readonly List<InventoryItem> validItemsCache = new List<InventoryItem>();

    private readonly List<GameObject> quickPageObjects = new List<GameObject>();
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
        if (quickPageParent == null || quickPagePrefab == null || quickSlotPrefab == null)
            return;

        int safeSlotPerPage = Mathf.Max(1, quickSlotsPerPage);

        int pageCount = Mathf.Max(
            1,
            Mathf.CeilToInt((float)validItems.Count / safeSlotPerPage)
        );

        int totalSlotCount = pageCount * safeSlotPerPage;

        EnsureQuickPages(pageCount);
        EnsureQuickSlots(totalSlotCount, safeSlotPerPage);

        for (int i = 0; i < quickSlotObjects.Count; i++)
        {
            bool active = i < totalSlotCount;

            quickSlotObjects[i].SetActive(active);

            if (!active)
            {
                if (quickSlotUIs[i] != null)
                    quickSlotUIs[i].SetSlot(null);

                continue;
            }

            int pageIndex = i / safeSlotPerPage;
            Transform targetParent = quickPageObjects[pageIndex].transform;

            if (quickSlotObjects[i].transform.parent != targetParent)
                quickSlotObjects[i].transform.SetParent(targetParent, false);

            if (quickSlotUIs[i] == null)
                continue;

            if (i < validItems.Count)
                quickSlotUIs[i].SetSlot(validItems[i]);
            else
                quickSlotUIs[i].SetSlot(null);
        }

        if (snapScroll != null)
        {
            snapScroll.SetPageCount(pageCount);

            if (resetQuickPageOnRefresh)
                snapScroll.MoveToPageInstant(0);
        }
    }

    private void EnsureQuickPages(int pageCount)
    {
        while (quickPageObjects.Count < pageCount)
        {
            GameObject pageObj = Instantiate(quickPagePrefab, quickPageParent);
            quickPageObjects.Add(pageObj);
        }

        for (int i = 0; i < quickPageObjects.Count; i++)
            quickPageObjects[i].SetActive(i < pageCount);
    }

    private void EnsureQuickSlots(int totalSlotCount, int slotPerPage)
    {
        while (quickSlotObjects.Count < totalSlotCount)
        {
            int index = quickSlotObjects.Count;
            int pageIndex = index / slotPerPage;

            Transform parent = quickPageObjects[pageIndex].transform;

            GameObject slotObj = Instantiate(quickSlotPrefab, parent);

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