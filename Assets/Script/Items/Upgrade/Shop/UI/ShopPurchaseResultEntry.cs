using System;

[Serializable]
public struct ShopPurchaseResultEntry
{
    public ItemData itemData;
    public int count;

    public ShopPurchaseResultEntry(ItemData itemData, int count)
    {
        this.itemData = itemData;
        this.count = count;
    }
}
