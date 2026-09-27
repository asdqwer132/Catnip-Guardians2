using System;
using UnityEngine;

[Serializable]
public sealed class CraftChanceRule
{
    public CraftCondition condition;

    [Range(0f, 100f)]
    public float chancePercent;
}

[CreateAssetMenu(fileName = "CraftItem", menuName = "GameData/Item/Crafting/Item")]
public sealed class CraftItemData : ScriptableObject
{
    public ItemData resultItem;

    [Min(0f)]
    public float baseChancePercent;

    public CraftChanceRule[] chanceRules = new CraftChanceRule[0];

    /// <summary>Uncapped sum, useful for showing the actual condition bonuses in the UI.</summary>
    public float GetRawChancePercent(CraftContext context)
    {
        if (context == null)
            return 0f;

        double sum = Math.Max(0d, baseChancePercent);

        if (chanceRules != null)
        {
            foreach (CraftChanceRule rule in chanceRules)
            {
                if (rule == null || rule.condition == null || !rule.condition.IsMet(context))
                    continue;

                sum += Mathf.Clamp(rule.chancePercent, 0f, 100f);
            }
        }

        return (float)Math.Min(sum, float.MaxValue);
    }
}
