using UnityEngine;

[System.Serializable]
public class InventoryItem : ISearchable
{
    public ItemData itemData;
    public int amount = 1;

    public InventoryItem(ItemData itemData, int amount)
    {
        this.itemData = itemData;
        this.amount = amount;
    }


    public ItemGrade GetGrade() { return itemData.grade; }
    public ItemCategory GetItemCategory() { return itemData.category; }
    public ItemSeries GetItemSeries() { return itemData.series; }

}