public class BuffQueryContext
{
    public ItemData itemData;
    public IBuffTarget buffTarget;
    public bool allItems;
    public ItemData targetSourceItemData;
    public static BuffQueryContext ForItem(ItemData itemData) => new BuffQueryContext { itemData = itemData };
    public static BuffQueryContext ForTarget(IBuffTarget target, ItemData sourceItemData = null)
        => new BuffQueryContext { buffTarget = target, targetSourceItemData = sourceItemData };
    public static BuffQueryContext ForAllItems() => new BuffQueryContext { allItems = true };
}
