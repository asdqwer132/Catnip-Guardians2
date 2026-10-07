using UnityEngine;

public enum CraftAmountComparison
{
    AtLeast,
    Exactly,
    AtMost,
    BetweenInclusive
}

[CreateAssetMenu(fileName = "MaterialAmountCondition", menuName = "GameData/Crafting/Conditions/Material Amount")]
public sealed class CraftMaterialAmountCondition : CraftCondition
{
    public ItemData material;
    public CraftAmountComparison comparison = CraftAmountComparison.AtLeast;

    [Min(0)] public int amount = 1;
    [Min(0)] public int maximum = 1;

    public override bool IsMet(CraftContext context)
    {
        if (context == null || material == null)
            return false;

        int actual = context.GetAmount(material);
        int threshold = Mathf.Max(0, amount);

        switch (comparison)
        {
            case CraftAmountComparison.AtLeast:
                return actual >= threshold;
            case CraftAmountComparison.Exactly:
                return actual == threshold;
            case CraftAmountComparison.AtMost:
                return actual <= threshold;
            case CraftAmountComparison.BetweenInclusive:
                return actual >= threshold && actual <= Mathf.Max(threshold, maximum);
            default:
                return false;
        }
    }
}
