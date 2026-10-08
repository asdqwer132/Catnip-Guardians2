using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[CreateAssetMenu(fileName = "HasAreaCondition", menuName = "GameData/Items/Conditions/Has Area")]
public sealed class HasAreaConditionData : ItemEffectConditionData
{
    public AreaDefinition definition;
    public AreaQueryPosition position = AreaQueryPosition.ImpactPosition;
    public AreaOwnerFilter ownerFilter = AreaOwnerFilter.Own;
    [Min(1)] public int minimumCount = 1;
    private sealed class Matches { public readonly List<ReactiveGroundArea> areas = new List<ReactiveGroundArea>(); }
    private readonly ConditionalWeakTable<ItemEffectPlan, Matches> matches = new ConditionalWeakTable<ItemEffectPlan, Matches>();

    public override bool IsSatisfied(ItemEffectContext context)
    {
        if (context == null || context.plan == null) return false;
        Matches saved = matches.GetValue(context.plan, key => new Matches());
        AreaRegistry.Find(definition, context, position, ownerFilter, saved.areas);
        if (saved.areas.Count >= Mathf.Max(1, minimumCount)) return true;
        saved.areas.Clear();
        return false;
    }

    internal List<ReactiveGroundArea> TakeMatches(ItemEffectContext context)
    {
        Matches saved;
        if (context == null || context.plan == null || !matches.TryGetValue(context.plan, out saved)) return null;
        List<ReactiveGroundArea> result = new List<ReactiveGroundArea>(saved.areas);
        saved.areas.Clear();
        return result;
    }
}
