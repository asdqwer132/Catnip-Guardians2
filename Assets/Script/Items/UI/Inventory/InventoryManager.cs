using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance;

    [Header("Referrences")]
    public List<InventoryItem> items = new List<InventoryItem>();

    public Action onInventoryChanged;

    void Awake()
    {
        if (instance == null)
            instance = this;
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

        //Debug.Log($"{itemData.itemName} 획득! 현재 수량: {GetItemAmount(itemData)}");

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
        {
            //Debug.Log("해당 아이템이 인벤토리에 없습니다.");
            return false;
        }

        if (existingItem.amount < amount)
        {
            //Debug.Log("아이템 수량이 부족합니다.");
            return false;
        }

        existingItem.amount -= amount;

        if (existingItem.amount < 0)
            existingItem.amount = 0;

        //Debug.Log($"{itemData.itemName} 사용됨! 남은 수량: {GetItemAmount(itemData)}");

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

    public bool HasItem(ItemData itemData, int amount = 1) { return GetItemAmount(itemData) >= amount; }
}