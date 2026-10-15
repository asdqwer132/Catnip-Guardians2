using System.Collections.Generic;
using UnityEngine;

// 그룹/소유자별 실행 상태는 런타임에 보관합니다. 정의 에셋을 수정하지 않습니다.
public static class SummonGroupRuntime
{
    private sealed class ScopeState
    {
        public SummonGroupDefinition group;
        public GameObject owner;
        public SummonOwnerScope ownerScope;
        public readonly Dictionary<SummonGroupItemRule, SummonGroupActivationGate> gates =
            new Dictionary<SummonGroupItemRule, SummonGroupActivationGate>();
    }
    private static readonly List<ScopeState> scopes = new List<ScopeState>();
    private static int generation = ItemEffectRuntime.Generation;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset()
    { scopes.Clear(); generation = ItemEffectRuntime.Generation; }

    private static void EnsureGeneration()
    { if (generation != ItemEffectRuntime.Generation) Reset(); }

    public static SummonGroupPopulation GetPopulation(SummonGroupDefinition group, GameObject owner)
    {
        var population = new SummonGroupPopulation();
        if (group == null) return population;
        var kinds = new HashSet<SummonDefinition>();
        foreach (SummonActor actor in SummonRegistry.Active)
        {
            if (actor == null || !actor.IsGroupReady || !actor.CanAct || actor.Definition == null ||
                actor.Definition.summonGroup != group ||
                (group.ownerScope == SummonOwnerScope.SameOwner && actor.Owner != owner)) continue;
            population.instanceCount++; kinds.Add(actor.Definition);
        }
        population.kindCount = kinds.Count;
        return population;
    }

    // 이탈/재초기화 때는 발동하지 않고, 조건이 깨진 규칙만 다시 준비합니다.
    public static void NotifyFieldChanged()
    {
        EnsureGeneration();
        for (int i = scopes.Count - 1; i >= 0; i--)
        {
            ScopeState scope = scopes[i];
            if (scope.group == null || scope.ownerScope != scope.group.ownerScope) { scopes.RemoveAt(i); continue; }
            SummonGroupPopulation population = GetPopulation(scope.group, scope.owner);
            foreach (var entry in scope.gates) entry.Value.Observe(entry.Key.Matches(population));
            if (scope.group == null || population.instanceCount == 0) scopes.RemoveAt(i);
        }
    }

    public static void NotifyReady(SummonActor source)
    {
        NotifyFieldChanged();
        if (source == null || !source.IsGroupReady || !source.CanAct || source.Definition == null) return;
        SummonGroupDefinition group = source.Definition.summonGroup;
        if (group == null || group.itemRules == null) return;
        ItemEffectContext context = source.CreateContext(source.transform.position, Vector3.right);
        int sourceLife = source.LifeId;
        // 원본의 사용 방향과 출처를 유지합니다. 보상으로 생긴 자식도 개체 수에는 더합니다.
        if (source.TryGetSpawnDirection(out var direction)) context.direction = direction;
        GameObject owner = group.ownerScope == SummonOwnerScope.AllOwners ? null : source.Owner;
        ScopeState scope = scopes.Find(value => value.group == group && value.owner == owner);
        if (scope == null) { scope = new ScopeState { group = group, owner = owner, ownerScope = group.ownerScope }; scopes.Add(scope); }
        SummonGroupItemRule[] rules = (SummonGroupItemRule[])group.itemRules.Clone();
        var seen = new HashSet<SummonGroupItemRule>();
        foreach (SummonGroupItemRule rule in rules)
        {
            if (source == null || source.LifeId != sourceLife || !source.CanAct || !context.CanContinue) break;
            if (rule == null || !seen.Add(rule)) continue;
            if (!scope.gates.TryGetValue(rule, out var gate)) { gate = new SummonGroupActivationGate(); scope.gates.Add(rule, gate); }
            bool matches = rule.Matches(GetPopulation(group, owner));
            gate.Observe(matches);
            if (!context.CanContinue || !context.allowSummonGroupReactions || !source.CanAct ||
                !gate.TryBegin(matches, rule.triggerMode)) continue;
            bool succeeded = false;
            try
            {
                ItemEffectContext reward = context.Copy(source.transform.position, context.direction);
                reward.allowItemUseReactions = false;
                reward.allowSummonGroupReactions = false;
                reward.consumeUseBuffs = rule.consumeUseBuffs && context.consumeUseBuffs;
                ItemThrowExecutor executor = rule.itemUse != null && rule.itemUse.mode == ItemUseDeliveryMode.Throw
                    ? source.GetOrCreateItemThrowExecutor() : null;
                if (rule.itemUse != null) succeeded = rule.itemUse.TryUse(rule.item, reward, executor, rule.consumeUseBuffs);
            }
            finally {
                gate.Complete(succeeded); }
        }
    }
}
