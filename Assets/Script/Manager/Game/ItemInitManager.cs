using System;
using UnityEngine;
[Serializable]
public class TestItemSet
{
    public string memo;
    public bool isUse;
    public ItemData[] itemDatas;
}
public class ItemInitManager : MonoBehaviour
{
    public TestItemSet[] items;
    public int setAmount = 99;

    public void ApplyDefaultInventoryItems()
    {
        if (items == null)
            return;

        if (InventoryManager.instance == null)
        {
            Debug.LogWarning("InventoryManager가 없습니다.");
            return;
        }
        foreach(var list in items)
        {
            if(!list.isUse) 
                continue;
            foreach(ItemData item in list.itemDatas)
            {
                InventoryItem added = new InventoryItem(item, setAmount);

                if (item == null || added == null)
                    continue;

                InventoryManager.instance.AddItem(added);
            }
        }
    }
}