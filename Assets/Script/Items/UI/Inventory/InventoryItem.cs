using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public ItemData itemData;
    public int amount = 1;

    public InventoryItem(ItemData itemData, int amount)
    {
        this.itemData = itemData;
        this.amount = amount;
    }
}