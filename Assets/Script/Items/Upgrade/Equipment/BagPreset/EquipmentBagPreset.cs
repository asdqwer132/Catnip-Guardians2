using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EquipmentBagPreset
{
    public string presetName;
    public BagData sourceBagData;
    public List<ItemData> slotItems = new List<ItemData>();

    public EquipmentBagPreset()
    {
    }

    public EquipmentBagPreset(string presetName, EquipmentBag sourceBag)
    {
        this.presetName = presetName;
        Capture(sourceBag);
    }

    public void Capture(EquipmentBag sourceBag)
    {
        sourceBagData = sourceBag != null ? sourceBag.bagData : null;

        if (slotItems == null)
            slotItems = new List<ItemData>();

        slotItems.Clear();

        if (sourceBag == null || sourceBag.equippedItems == null)
            return;

        int max = Mathf.Min(sourceBag.currentSlotCount, sourceBag.equippedItems.Count);

        for (int i = 0; i < max; i++)
        {
            InventoryItem item = sourceBag.equippedItems[i];
            slotItems.Add(item != null && item.itemData != null && item.amount > 0 ? item.itemData : null);
        }

        TrimTrailingEmptySlots();
    }

    public bool HasAnyItem()
    {
        if (slotItems == null)
            return false;

        for (int i = 0; i < slotItems.Count; i++)
        {
            if (slotItems[i] != null)
                return true;
        }

        return false;
    }

    public int GetRequiredSlotCount()
    {
        if (slotItems == null)
            return 0;

        for (int i = slotItems.Count - 1; i >= 0; i--)
        {
            if (slotItems[i] != null)
                return i + 1;
        }

        return 0;
    }

    public float GetTotalWeight()
    {
        if (slotItems == null)
            return 0f;

        float totalWeight = 0f;

        for (int i = 0; i < slotItems.Count; i++)
        {
            ItemData itemData = slotItems[i];

            if (itemData == null)
                continue;

            totalWeight += itemData.weight;
        }

        return totalWeight;
    }

    public Dictionary<ItemData, int> GetItemCountMap()
    {
        Dictionary<ItemData, int> result = new Dictionary<ItemData, int>();

        if (slotItems == null)
            return result;

        for (int i = 0; i < slotItems.Count; i++)
        {
            ItemData itemData = slotItems[i];

            if (itemData == null)
                continue;

            if (!result.ContainsKey(itemData))
                result.Add(itemData, 0);

            result[itemData]++;
        }

        return result;
    }

    private void TrimTrailingEmptySlots()
    {
        if (slotItems == null)
            return;

        for (int i = slotItems.Count - 1; i >= 0; i--)
        {
            if (slotItems[i] != null)
                break;

            slotItems.RemoveAt(i);
        }
    }
}
