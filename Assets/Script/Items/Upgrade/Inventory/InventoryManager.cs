using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance;

    [Header("Referrences")]
    public List<InventoryItem> items = new List<InventoryItem>();

    public Action onInventoryChanged;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    public void AddItem(InventoryItem inventoryItem)
    {
        if (inventoryItem == null)
            return;

        AddItem(inventoryItem.itemData, inventoryItem.amount);
    }

    public void AddItem(ItemData itemData, int amount = 1)
    {
        if (itemData == null)
        {
            Debug.LogWarning("추가하려는 아이템이 null입니다.");
            return;
        }

        InventoryItem existingItem = items.Find(x => x.itemData == itemData);

        if (existingItem != null)
        {
            existingItem.amount += amount;
        }
        else
        {
            InventoryItem newItem = new InventoryItem(itemData, amount);
            items.Add(newItem);
        }

        SortItemsByDataId();

        onInventoryChanged?.Invoke();
    }

    public bool RemoveItem(ItemData itemData, int amount = 1)
    {
        if (itemData == null)
        {
            Debug.LogWarning("삭제하려는 아이템이 null입니다.");
            return false;
        }

        InventoryItem existingItem = items.Find(x => x.itemData == itemData);

        if (existingItem == null)
            return false;

        if (existingItem.amount < amount)
            return false;

        existingItem.amount -= amount;

        if (existingItem.amount <= 0)
        {
            items.Remove(existingItem);
        }

        SortItemsByDataId();

        onInventoryChanged?.Invoke();

        return true;
    }

    public int GetItemAmount(ItemData itemData)
    {
        if (itemData == null)
            return 0;

        InventoryItem item = items.Find(x => x.itemData == itemData);

        if (item == null)
            return 0;

        return item.amount;
    }

    public bool HasItem(ItemData itemData, int amount = 1)
    {
        return GetItemAmount(itemData) >= amount;
    }

    private void SortItemsByDataId()
    {
        items.Sort(CompareInventoryItemByDataId);
    }

    private int CompareInventoryItemByDataId(InventoryItem a, InventoryItem b)
    {
        string idA = GetDataId(a);
        string idB = GetDataId(b);

        bool aEmpty = string.IsNullOrWhiteSpace(idA);
        bool bEmpty = string.IsNullOrWhiteSpace(idB);

        if (aEmpty && bEmpty) return 0;
        if (aEmpty) return 1;
        if (bEmpty) return -1;

        string prefixA = GetIdPrefix(idA);
        string prefixB = GetIdPrefix(idB);

        int prefixCompare = string.Compare(prefixA, prefixB, StringComparison.OrdinalIgnoreCase);
        if (prefixCompare != 0)
            return prefixCompare;

        int numberA = GetIdNumber(idA);
        int numberB = GetIdNumber(idB);

        if (numberA != numberB)
            return numberA.CompareTo(numberB);

        return string.Compare(idA, idB, StringComparison.OrdinalIgnoreCase);
    }

    private string GetDataId(InventoryItem item)
    {
        if (item == null || item.itemData == null)
            return "";

        return item.itemData.dataId;
    }

    private string GetIdPrefix(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return "";

        id = id.Trim();

        int lastSpaceIndex = id.LastIndexOf(' ');

        if (lastSpaceIndex < 0)
            return id;

        return id.Substring(0, lastSpaceIndex).Trim();
    }

    private int GetIdNumber(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return int.MaxValue;

        id = id.Trim();

        int lastSpaceIndex = id.LastIndexOf(' ');

        if (lastSpaceIndex < 0)
            return int.MaxValue;

        string numberText = id.Substring(lastSpaceIndex + 1).Trim();

        if (int.TryParse(numberText, out int number))
            return number;

        return int.MaxValue;
    }
}