using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
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

    [Header("Option")]
    public bool refreshOnEnable = true;

    private void Start()
    {
        if (InventoryManager.instance != null)
            InventoryManager.instance.onInventoryChanged += RefreshUI;

        RefreshUI();
    }

    private void OnEnable()
    {
        if (refreshOnEnable)
            RefreshUI();
    }

    private void OnDestroy()
    {
        if (InventoryManager.instance != null)
            InventoryManager.instance.onInventoryChanged -= RefreshUI;
    }

    public void RefreshUI()
    {
        if (InventoryManager.instance == null)
            return;

        List<InventoryItem> validItems = GetValidItems();

        RefreshQuickInventory(validItems);
        RefreshDetailInventory(validItems);
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

        for (int i = 0; i < validItems.Count; i++)
        {
            int pageIndex = i / quickSlotsPerPage;

            GameObject slotObj = Instantiate(
                quickSlotPrefab,
                pages[pageIndex]
            );

            BaseItemSlotUI slotUI = slotObj.GetComponent<BaseItemSlotUI>();

            if (slotUI != null)
                slotUI.SetSlot(validItems[i]);
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

    private List<InventoryItem> GetValidItems()
    {
        List<InventoryItem> result = new List<InventoryItem>();

        foreach (InventoryItem item in InventoryManager.instance.items)
        {
            if (item == null || item.itemData == null)
                continue;

            result.Add(item);
        }

        return result;
    }

    private void ClearChildren(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}