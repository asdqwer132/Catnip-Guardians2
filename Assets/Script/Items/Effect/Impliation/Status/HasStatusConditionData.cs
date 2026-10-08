using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[CreateAssetMenu(fileName = "HasStatusCondition", menuName = "GameData/Items/Conditions/Has Status Key")]
public sealed class HasStatusConditionData : ItemEffectConditionData
{
    public StatusDefinition status;
    public StatusQueryTarget target = StatusQueryTarget.PlayerStatus;
    [Min(1)] public int minimumStack = 1;

    internal struct Match
    {
        public ActiveBuff buff;
        public ulong version;
    }
    private sealed class Matches { public readonly List<Match> buffs = new List<Match>(); }
    private readonly ConditionalWeakTable<ItemEffectPlan, Matches> matches = new ConditionalWeakTable<ItemEffectPlan, Matches>();

    public override bool IsSatisfied(ItemEffectContext context)
    {
        if (context == null || context.plan == null) return false;
        Matches saved = matches.GetValue(context.plan, key => new Matches());
        saved.buffs.Clear();
        BuffQueryContext query = StatusTargetUtility.Resolve(target, context);
        if (query == null || context.buffManager == null || status == null) return false;
        List<ActiveBuff> found = new List<ActiveBuff>();
        context.buffManager.FindStatuses(status, query, found);
        long stack = 0;
        foreach (ActiveBuff buff in found)
        {
            stack += Mathf.Max(1, buff.stack);
            saved.buffs.Add(new Match { buff = buff, version = buff.RegistrationVersion });
        }
        if (stack >= Mathf.Max(1, minimumStack)) return true;
        saved.buffs.Clear();
        return false;
    }

    internal List<Match> TakeMatches(ItemEffectContext context)
    {
        Matches saved;
        if (context == null || context.plan == null || !matches.TryGetValue(context.plan, out saved)) return null;
        List<Match> result = new List<Match>(saved.buffs);
        saved.buffs.Clear();
        return result;
    }
}
