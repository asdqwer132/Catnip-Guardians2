using System.Collections.Generic;
using UnityEngine;

public class BagCooldownUI : MonoBehaviour
{
    [Header("Select UI")]
    public GameObject selectedFrame;

    [Header("Slot Create UI")]
    public Transform slotUIParent;
    public BagSlotCooldownUI slotUIPrefab;

    private List<BagSlotCooldownUI> slotUIs = new List<BagSlotCooldownUI>();

    public void BuildSlotUIs(BagItemUseManager manager)
    {
        ClearSlotUIs();

        if (manager == null || manager.bag == null || manager.bag.equippedItems == null)
            return;

        if (slotUIParent == null || slotUIPrefab == null)
            return;

        int slotCount = manager.bag.GetEquippedCount();

        for (int i = 0; i < slotCount; i++)
        {
            BagSlotCooldownUI slotUI = Instantiate(slotUIPrefab, slotUIParent);
            slotUI.gameObject.SetActive(true);
            slotUIs.Add(slotUI);
        }

        RefreshSlotItemIcons(manager);
    }

    public void RefreshUI(BagItemUseManager manager, bool isSelected)
    {
        UpdateSelectedUI(isSelected);
        ClearNextUseSlotImages();

        if (manager == null)
        {
            ClearSlotUIsVisualOnly();
            return;
        }

        RefreshSlotItemIcons(manager);

        if (!isSelected)
            return;

        UpdateNextUseSlotImage(manager);
    }

    private void UpdateSelectedUI(bool isSelected)
    {
        if (selectedFrame != null)
            selectedFrame.SetActive(isSelected);
    }

    private void UpdateNextUseSlotImage(BagItemUseManager manager)
    {
        if (manager == null)
            return;

        if (manager.IsBagCoolingDown())
            return;

        int nextSlotIndex = GetNextSlotIndex(manager);

        if (nextSlotIndex < 0 || nextSlotIndex >= slotUIs.Count)
            return;

        if (slotUIs[nextSlotIndex] == null)
            return;

        slotUIs[nextSlotIndex].SetNextUse(true);
    }

    private int GetNextSlotIndex(BagItemUseManager manager)
    {
        int readyIndex = manager.GetNextReadyUsableSlotIndexForUI();

        if (readyIndex >= 0)
            return readyIndex;

        return FindNextItemSlotIndex(manager);
    }

    private int FindNextItemSlotIndex(BagItemUseManager manager)
    {
        if (manager == null || manager.bag == null || manager.bag.equippedItems == null)
            return -1;

        InventoryItem nextItem = manager.GetNextUsableInventoryItemForUI();

        if (nextItem == null)
            return -1;

        for (int i = 0; i < manager.bag.equippedItems.Count; i++)
        {
            if (manager.bag.equippedItems[i] == nextItem)
                return i;
        }

        return -1;
    }

    private void RefreshSlotItemIcons(BagItemUseManager manager)
    {
        if (manager == null || manager.bag == null || manager.bag.equippedItems == null)
            return;

        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (slotUIs[i] == null)
                continue;

            InventoryItem item = null;

            if (i < manager.bag.equippedItems.Count)
                item = manager.bag.equippedItems[i];

            slotUIs[i].SetItem(item);
        }
    }

    private void ClearNextUseSlotImages()
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (slotUIs[i] != null)
                slotUIs[i].SetNextUse(false);
        }
    }

    private void ClearSlotUIsVisualOnly()
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (slotUIs[i] != null)
                slotUIs[i].Clear();
        }
    }

    private void ClearSlotUIs()
    {
        for (int i = slotUIs.Count - 1; i >= 0; i--)
        {
            if (slotUIs[i] != null)
                Destroy(slotUIs[i].gameObject);
        }

        slotUIs.Clear();
    }
}