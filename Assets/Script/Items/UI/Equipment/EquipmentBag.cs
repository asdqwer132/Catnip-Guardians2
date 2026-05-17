using System.Collections.Generic;
using UnityEngine;

public class EquipmentBag : MonoBehaviour
{
    [Header("Bag Info")]
    public BagData bagData;

    [Header("Slot Settings")]
    public int slotCount = 5;

    [Header("UI")]
    public EquipmentBagUI bagUI;

    public List<InventoryItem> equippedItems = new List<InventoryItem>();

    public void Init()
    {
        equippedItems.Clear();

        for (int i = 0; i < slotCount; i++)
        {
            equippedItems.Add(new InventoryItem(null, 0));
        }

        RefreshUI();
    }
    public void SwapItems(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= equippedItems.Count)
            return;

        if (toIndex < 0 || toIndex >= equippedItems.Count)
            return;

        if (fromIndex == toIndex)
            return;

        InventoryItem temp = equippedItems[fromIndex];
        equippedItems[fromIndex] = equippedItems[toIndex];
        equippedItems[toIndex] = temp;

        RefreshUI();
    }
    public bool EquipItem(InventoryItem item)
    {
        if (item == null || item.itemData == null)
        {
            Debug.LogWarning("장착할 아이템이 없습니다.");
            return false;
        }

        if (item.amount <= 0)
        {
            Debug.LogWarning("아이템 수량이 없습니다.");
            return false;
        }

        if (bagData == null)
        {
            Debug.LogWarning("가방 데이터가 없습니다.");
            return false;
        }

        int emptyIndex = GetEmptySlotIndex();

        if (emptyIndex == -1)
        {
            //Debug.LogWarning(bagData.bagName + "에 빈 슬롯이 없습니다.");
            return false;
        }

        float currentWeight = GetCurrentWeight();
        float itemWeight = item.itemData.weight;
        float maxWeight = bagData.maxWeight;

        if (currentWeight + itemWeight > maxWeight)
        {
            //Debug.LogWarning(
            //    bagData.bagName + "의 최대 무게를 초과합니다. " +
            //    "현재 무게: " + currentWeight +
            //    " / 추가 무게: " + itemWeight +
            //    " / 최대 무게: " + maxWeight
            //);
            return false;
        }

        InventoryItem equipItem = new InventoryItem(item.itemData, 1);

        equippedItems[emptyIndex] = equipItem;

        RefreshUI();

        //Debug.Log(
        //    bagData.bagName + "에 장착됨: " + item.itemData.itemName +
        //    " / 현재 무게: " + GetCurrentWeight() +
        //    " / 최대 무게: " + maxWeight
        //);

        return true;
    }

    public void ClearAllSlots()
    {
        for (int i = 0; i < equippedItems.Count; i++)
        {
            equippedItems[i] = new InventoryItem(null, 0);
        }

        RefreshUI();
    }

    public void UnequipItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= equippedItems.Count)
            return;

        InventoryItem item = equippedItems[slotIndex];

        if (item == null || item.itemData == null)
            return;

        equippedItems[slotIndex] = new InventoryItem(null, 0);

        RefreshUI();
    }

    public InventoryItem GetItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= equippedItems.Count)
            return null;

        return equippedItems[slotIndex];
    }

    public float GetCurrentWeight()
    {
        float totalWeight = 0f;

        for (int i = 0; i < equippedItems.Count; i++)
        {
            InventoryItem item = equippedItems[i];

            if (item == null || item.itemData == null || item.amount <= 0)
                continue;

            totalWeight += item.itemData.weight * item.amount;
        }

        return totalWeight;
    }

    public float GetMaxWeight()
    {
        if (bagData == null)
            return 0f;

        return bagData.maxWeight;
    }

    //public bool CanAddItem(InventoryItem item)
    //{
    //    if (item == null || item.itemData == null)
    //        return false;

    //    if (bagData == null)
    //        return false;

    //    if (GetEmptySlotIndex() == -1)
    //        return false;

    //    float nextWeight = GetCurrentWeight() + item.itemData.weight;

    //    return nextWeight <= bagData.maxWeight;
    //}

    public void RefreshUI()
    {
        if (bagUI != null)
            bagUI.Refresh(this);
    }

    private int GetEmptySlotIndex()
    {
        for (int i = 0; i < equippedItems.Count; i++)
        {
            if (equippedItems[i] == null || equippedItems[i].amount <= 0 || equippedItems[i].itemData == null)
                return i;
        }

        return -1;
    }
}