public class BuffQueryContext
{
    public ItemData itemData;
    public EquipmentBag bag;
    public IBuffTarget buffTarget;
    public bool isBagQuery;
    // 전체 버프의 Include Self 판정에 사용하는 공격/소환물 출처입니다.
    public ItemData targetSourceItemData;

    public static BuffQueryContext ForItem(ItemData itemData, EquipmentBag bag)
    {
        return new BuffQueryContext
        {
            itemData = itemData,
            bag = bag
        };
    }

    public static BuffQueryContext ForTarget(IBuffTarget target, ItemData sourceItemData = null)
    {
        return new BuffQueryContext
        {
            buffTarget = target,
            targetSourceItemData = sourceItemData
        };
    }

    public static BuffQueryContext ForBag(EquipmentBag bag)
        => new BuffQueryContext { bag = bag, isBagQuery = true };
}
