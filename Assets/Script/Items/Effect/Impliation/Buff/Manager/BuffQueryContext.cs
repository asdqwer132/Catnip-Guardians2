public class BuffQueryContext
{
    public ItemData itemData;
    public EquipmentBag bag;
    public IBuffTarget buffTarget;
    public bool isBagQuery;

    public static BuffQueryContext ForItem(ItemData itemData, EquipmentBag bag)
    {
        return new BuffQueryContext
        {
            itemData = itemData,
            bag = bag
        };
    }

    public static BuffQueryContext ForTarget(IBuffTarget target)
    {
        return new BuffQueryContext
        {
            buffTarget = target
        };
    }

    public static BuffQueryContext ForBag(EquipmentBag bag)
        => new BuffQueryContext { bag = bag, isBagQuery = true };
}
