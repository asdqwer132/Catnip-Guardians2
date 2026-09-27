using System;
using UnityEngine;

[Serializable]
public sealed class CraftGaugeContribution
{
    public ItemData material;

    [Min(0)] public int pointsPerItem = 10;
}

/// <summary>Each material contributes points. Use overlapping ranges for additive bonuses.</summary>
[CreateAssetMenu(fileName = "GaugeRangeCondition", menuName = "GameData/Item/Crafting/Conditions/Gauge Range")]
public sealed class CraftGaugeRangeCondition : CraftCondition
{
    public CraftGaugeContribution[] contributions = new CraftGaugeContribution[0];

    [Min(0)] public int pointsForOtherMaterials = 1;
    [Min(0)] public int minimum;
    [Min(0)] public int maximum = 10;

    public override bool IsMet(CraftContext context)
    {
        if (context == null)
            return false;

        long points = GetGaugePoints(context);
        return points >= minimum && points <= maximum;
    }

    public long GetGaugePoints(CraftContext context)
    {
        if (context == null)
            return 0;

        long points = 0;
        foreach (var material in context.Materials)
        {
            int contribution = Mathf.Max(0, pointsForOtherMaterials);
            if (contributions != null)
            {
                foreach (CraftGaugeContribution entry in contributions)
                {
                    if (entry == null || entry.material == null || entry.material != material.Key)
                        continue;

                    contribution = Mathf.Max(0, entry.pointsPerItem);
                    break;
                }
            }

            points += (long)contribution * material.Value;
        }

        return points;
    }
}
