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


    public DefaultData GetSearchData() => itemData;
    public Grade GetGrade() { return itemData.grade; }
    public DataType GetDataType() { return itemData.dataType; }
    public ItemSeries GetItemSeries() { return itemData.series; }

}