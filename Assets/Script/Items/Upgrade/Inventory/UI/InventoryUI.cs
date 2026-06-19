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

    public ItemTooltipUI tooltipUI; 

    public void Init()
    {
        if (InventoryManager.instance != null)
            InventoryManager.instance.onInventoryChanged += RefreshUI;

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (InventoryManager.instance != null)
            InventoryManager.instance.onInventoryChanged -= RefreshUI;
    }

    protected override void OnSearchFilterChanged()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (InventoryManager.instance == null)
            return;

        List<InventoryItem> validItems = GetValidItems();

        RefreshQuickInventory(validItems);
        RefreshDetailInventory(validItems);
    }

    private List<InventoryItem> GetValidItems()
    {
        List<InventoryItem> result = new List<InventoryItem>();

        if (InventoryManager.instance == null)
            return result;

        foreach (InventoryItem item in InventoryManager.instance.items)
        {
            if (!IsInventoryItemVisible(item))
                continue;

            result.Add(item);
        }

        return result;
    }

    private void RefreshQuickInventory(List<InventoryItem> validItems)
    {
        if (quickPageParent == null || quickPagePrefab == null || quickSlotPrefab == null)
            return;

        ClearChildren(quickPageParent);

        int pageCount = Mathf.Max(
            1,
            Mathf.CeilToInt((float)validItems.Count / quickSlotsPerPage)
        );

        List<Transform> pages = new List<Transform>();

        for (int i = 0; i < pageCount; i++)
        {
            GameObject pageObj = Instantiate(quickPagePrefab, quickPageParent);
            pages.Add(pageObj.transform);
        }

        int totalSlotCount = pageCount * quickSlotsPerPage;

        for (int i = 0; i < totalSlotCount; i++)
        {
            int pageIndex = i / quickSlotsPerPage;

            GameObject slotObj = Instantiate(
                quickSlotPrefab,
                pages[pageIndex]
            );

            BaseItemSlotUI slotUI = slotObj.GetComponent<BaseItemSlotUI>();
            ItemTooltipTrigger tooltipTrigger = slotObj.GetComponent<ItemTooltipTrigger>();

            if (slotUI == null)
                continue;
            if (tooltipTrigger != null)
                tooltipTrigger.Init(tooltipUI);

            if (i < validItems.Count)
                slotUI.SetSlot(validItems[i]);
            else
                slotUI.SetSlot(null);

        }

        if (snapScroll != null)
        {
            snapScroll.SetPageCount(pageCount);
            snapScroll.MoveToPageInstant(0);
        }
    }

    private void RefreshDetailInventory(List<InventoryItem> validItems)
    {
        if (detailSlotParent == null || detailSlotPrefab == null)
            return;

        ClearChildren(detailSlotParent);

        for (int i = 0; i < validItems.Count; i++)
        {
            GameObject slotObj = Instantiate(
                detailSlotPrefab,
                detailSlotParent
            );

            BaseItemSlotUI slotUI = slotObj.GetComponent<BaseItemSlotUI>();

            if (slotUI != null)
                slotUI.SetSlot(validItems[i]);
        }
    }

    private void ClearChildren(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}