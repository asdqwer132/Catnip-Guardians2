using System.Collections.Generic;
using UnityEngine;

public sealed class CraftRollResult
{
    public ItemData Item { get; private set; }
    public float RawChancePercent { get; private set; }
    public float EffectiveChancePercent => Mathf.Min(100f, RawChancePercent);
    public bool Succeeded { get; private set; }

    public CraftRollResult(ItemData item, float rawChancePercent, bool succeeded)
    {
        Item = item;
        RawChancePercent = rawChancePercent;
        Succeeded = succeeded;
    }
}

public sealed class CraftResult
{
    private readonly List<CraftRollResult> rolls = new List<CraftRollResult>();
    private readonly List<ItemData> craftedItems = new List<ItemData>();

    public IReadOnlyList<CraftRollResult> Rolls => rolls;
    public IReadOnlyList<ItemData> CraftedItems => craftedItems;
    public bool HasSuccess => craftedItems.Count > 0;
    public ItemData FailureItem { get; internal set; }

    internal void AddRoll(CraftRollResult roll)
    {
        rolls.Add(roll);
        if (roll.Succeeded)
            craftedItems.Add(roll.Item);
    }
}
