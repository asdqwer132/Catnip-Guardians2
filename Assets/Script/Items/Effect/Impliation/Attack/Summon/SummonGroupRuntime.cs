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
        public bool reactionsEnabled;
        public readonly SummonGroupReactionContext reaction = new SummonGroupReactionContext();
        public readonly Dictionary<SummonGroupReactionModule, SummonGroupReactionRuntime> modules =
            new Dictionary<SummonGroupReactionModule, SummonGroupReactionRuntime>();
        public readonly Dictionary<SummonGroupItemRule, SummonGroupActivationGate> gates =
            new Dictionary<SummonGroupItemRule, SummonGroupActivationGate>();
    }
    private static readonly List<ScopeState> scopes = new List<ScopeState>();
    private static int generation = ItemEffectRuntime.Generation;
    private static bool refreshing, refreshRequested;
    private static int lastFrame = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset()
    {
        ScopeState[] removed = scopes.ToArray();
        scopes.Clear(); generation = ItemEffectRuntime.Generation; lastFrame = -1;
        foreach (var scope in removed) DisposeModules(scope);
    }

    private static void EnsureGeneration()
    { if (generation != ItemEffectRuntime.Generation) Reset(); }

    public static SummonGroupPopulation GetPopulation(SummonGroupDefinition group, GameObject owner)
    {
        var population = new SummonGroupPopulation();
        if (group == null) return population;
        var kinds = new HashSet<SummonDefinition>();
        foreach (SummonActor actor in SummonRegistry.Active)
        {
            if (actor == null || !actor.IsGroupReady || !actor.CountsForSummonGroups || !actor.CanAct || actor.Definition == null ||
                actor.Definition.summonGroup != group ||
                (group.ownerScope == SummonOwnerScope.SameOwner && actor.Owner != owner)) continue;
            population.instanceCount++; kinds.Add(actor.Definition);
        }
        population.kindCount = kinds.Count;
        return population;
    }

    // 아이템은 이탈 시 재준비만 하고, 유지형 모듈은 조건 변화 양쪽 방향에 반응합니다.
    public static void NotifyFieldChanged()
    {
        EnsureGeneration();
        if (refreshing) { refreshRequested = true; return; }
        refreshing = true;
        try
        {
            do
            {
                refreshRequested = false;
                foreach (ScopeState scope in scopes.ToArray())
                {
                    if (!scopes.Contains(scope)) continue;
                    if (scope.group == null || scope.ownerScope != scope.group.ownerScope)
                    { scopes.Remove(scope); DisposeModules(scope); continue; }
                    SummonGroupPopulation population = GetPopulation(scope.group, scope.owner);
                    foreach (var entry in scope.gates) entry.Value.Observe(entry.Key.Matches(population));
                    scope.reaction.population = population;
                    RefreshModules(scope);
                    if (population.instanceCount == 0)
                    { scopes.Remove(scope); DisposeModules(scope); }
                }
            } while (refreshRequested);
        }
        finally { refreshing = false; }
    }

    private static void RefreshModules(ScopeState scope)
    {
        var configured = new HashSet<SummonGroupReactionModule>();
        foreach (var module in scope.group.reactionModules ?? new SummonGroupReactionModule[0])
            if (module != null) configured.Add(module);
        foreach (var entry in new Dictionary<SummonGroupReactionModule, SummonGroupReactionRuntime>(scope.modules))
            if (!configured.Contains(entry.Key)) { scope.modules.Remove(entry.Key); entry.Value.Dispose(); }
        if (!scope.reactionsEnabled) return;
        foreach (var module in scope.group.reactionModules ?? new SummonGroupReactionModule[0])
        {
            if (module == null || !configured.Remove(module)) continue;
            if (!scope.modules.TryGetValue(module, out var runtime))
            {
                runtime = module.CreateRuntime();
                if (runtime == null) continue;
                scope.modules.Add(module, runtime);
            }
            runtime.Refresh(scope.reaction);
        }
    }
    private static void DisposeModules(ScopeState scope)
    {
        var removed = new List<SummonGroupReactionRuntime>(scope.modules.Values);
        scope.modules.Clear();
        foreach (var runtime in removed) runtime.Dispose();
    }
    // 모든 소환물이 호출해도 프레임마다 한 번만 갱신합니다. 별도 씬 매니저가 필요 없습니다.
    public static void TickForFrame(int frame, float deltaTime)
    {
        EnsureGeneration();
        if (frame == lastFrame) return;
        lastFrame = frame;
        NotifyFieldChanged();
        foreach (ScopeState scope in scopes.ToArray())
            foreach (var runtime in new List<SummonGroupReactionRuntime>(scope.modules.Values)) runtime.Tick(deltaTime);
    }

    public static void NotifyReady(SummonActor source)
    {
        EnsureGeneration();
        if (source == null || !source.IsGroupReady || !source.CountsForSummonGroups || !source.CanAct || source.Definition == null)
        { NotifyFieldChanged(); return; }
        SummonGroupDefinition group = source.Definition.summonGroup;
        if (group == null) { NotifyFieldChanged(); return; }
        ItemEffectContext context = source.CreateContext(source.transform.position, Vector3.right);
        int sourceLife = source.LifeId;
        // 원본의 사용 방향과 출처를 유지합니다. 보상으로 생긴 자식도 개체 수에는 더합니다.
        if (source.TryGetSpawnDirection(out var direction)) context.direction = direction;
        GameObject owner = group.ownerScope == SummonOwnerScope.AllOwners ? null : source.Owner;
        ScopeState scope = scopes.Find(value => value.group == group && value.owner == owner);
        if (scope == null) { scope = new ScopeState { group = group, owner = owner, ownerScope = group.ownerScope }; scopes.Add(scope); }
        // 보상/특수 소환의 자식은 마지막 일반 소환 위치나 모듈의 원본 소유자를 덮어쓰지 않습니다.
        if (context.allowSummonGroupReactions)
        {
            scope.reactionsEnabled = true;
            scope.reaction.lastSummonPosition = source.transform.position;
            scope.reaction.source = context;
        }
        NotifyFieldChanged();
        if (group.itemRules == null) return;
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
