using UnityEngine;

// Add this asset to CraftItemData.chanceRules, together with its chance bonus.
[CreateAssetMenu(fileName = "GaugeRatioCondition", menuName = "GameData/Crafting/Conditions/Gauge Ratio")]
public sealed class CraftGaugeRatioCondition : CraftCondition
{
    public CraftGaugeType gaugeType;
    [Range(0f, 100f)] public float minimumPercent = 1f;
    [Range(0f, 100f)] public float maximumPercent = 100f;

    public override bool IsMet(CraftContext context)
    {
        if (context == null || context.Gauges == null ||
            context.Gauges.TotalWeightedAmount <= 0d || gaugeType == null)
            return false;

        float ratio = context.Gauges.GetPercentage(gaugeType);
        return ratio >= minimumPercent && ratio <= maximumPercent;
    }
}
