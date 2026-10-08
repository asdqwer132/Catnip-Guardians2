using System.Collections.Generic;
using UnityEngine;

public class ItemEffectContext
{
    // 공유 ScriptableObject 대신 이번 아이템 실행 컨텍스트에 실행 경로를 보관한다.
    private HashSet<ItemEffectData> executingEffects;
    private ItemEffectContext executionParent;
    private int generation = ItemEffectRuntime.Generation;
    public ItemEffectLifetime lifetime;
    public ItemEffectPlan plan;
    public HitEffectUseState hitUseState = new HitEffectUseState();
    public Enemy hitTarget;
    public int hitTargetLifeId;
    public SummonItemThrower sourceSummon;
    public int sourceSummonLifeId;
    public float damageMultiplier = 1f;
    public float healingMultiplier = 1f;
    public float rangeMultiplier = 1f;
    public float durationMultiplier = 1f;
    public float? damageOverride;
    public bool consumeUseBuffs = true;
    private ExecutionBudget executionBudget = new ExecutionBudget();

    private sealed class ExecutionBudget { public int remaining = 4096; }

    public bool CanContinue => generation == ItemEffectRuntime.Generation &&
        (lifetime == null || !lifetime.IsCancelled);

    public ItemEffectLease RetainLifetime(bool completionOnly = false) =>
        lifetime != null && (!completionOnly || lifetime.TracksCompletion) ? lifetime.Retain() : null;

    public ItemEffectContext Copy(Vector3 position, Vector3 attackDirection)
    {
        return new ItemEffectContext(owner, sourceItemData, usePosition, position,
            sourceBag, currentEffectData, buffManager, attackDirection, plan)
        {
            lifetime = lifetime, plan = plan, executionParent = this, generation = generation,
            hitUseState = hitUseState, hitTarget = hitTarget, hitTargetLifeId = hitTargetLifeId,
            sourceSummon = sourceSummon, sourceSummonLifeId = sourceSummonLifeId,
            damageMultiplier = damageMultiplier, healingMultiplier = healingMultiplier,
            rangeMultiplier = rangeMultiplier, durationMultiplier = durationMultiplier,
            damageOverride = damageOverride, executionBudget = executionBudget,
            consumeUseBuffs = consumeUseBuffs
        };
    }

    public void InheritExecution(ItemEffectContext parent)
    {
        if (parent == null) return;
        lifetime = parent.lifetime;
        executionParent = parent;
        generation = parent.generation;
        hitUseState = parent.hitUseState;
        hitTarget = parent.hitTarget;
        hitTargetLifeId = parent.hitTargetLifeId;
        sourceSummon = parent.sourceSummon;
        sourceSummonLifeId = parent.sourceSummonLifeId;
        damageMultiplier = parent.damageMultiplier;
        healingMultiplier = parent.healingMultiplier;
        rangeMultiplier = parent.rangeMultiplier;
        durationMultiplier = parent.durationMultiplier;
        damageOverride = parent.damageOverride;
        executionBudget = parent.executionBudget;
        consumeUseBuffs = parent.consumeUseBuffs;
    }

    public T GetSnapshotStat<T>(ItemEffectData effect, T stat) where T : class, IGameStat<T>
    {
        T snapshot = plan.GetSnapshot(effect, stat, this);
        return EffectExecutionScaling.Apply(snapshot, this);
    }

    public T GetUnscaledSnapshotStat<T>(ItemEffectData effect, T stat) where T : class, IGameStat<T>
    {
        T snapshot = plan.GetSnapshot(effect, stat, this);
        return snapshot != null ? snapshot.Clone() : null;
    }

    public T GetCurrentStat<T>(ItemEffectData effect, T stat) where T : class, IGameStat<T>
    {
        T snapshot = GetUnscaledSnapshotStat(effect, stat);
        if (snapshot == null) return null;
        T current = buffManager != null
            ? buffManager.GetBuffedStatForItem(snapshot, sourceItemData, sourceBag, BuffCalculationMode.DynamicOnly)
            : snapshot.Clone();
        return EffectExecutionScaling.Apply(current, this);
    }

    public GameObject owner;
    public ItemData sourceItemData;
    public EquipmentBag sourceBag;
    public ItemEffectData currentEffectData;
    public BuffManager buffManager;
    public Vector3 usePosition;
    public Vector3 targetPosition;
    public Vector3 direction;

    public ItemEffectContext
    (
        GameObject owner,
        ItemData sourceItemData,
        Vector3 usePosition,
        Vector3 targetPosition,
        EquipmentBag sourceBag,
        ItemEffectData currentEffectData = null,
        BuffManager buffManager = null,
        Vector3 direction = default(Vector3),
        ItemEffectPlan sharedPlan = null
    )
    {
        plan = sharedPlan ?? new ItemEffectPlan();
        this.owner = owner;
        this.sourceItemData = sourceItemData;

        this.usePosition = usePosition;
        this.targetPosition = targetPosition;
        this.direction = direction;

        this.sourceBag = sourceBag;
        this.currentEffectData = currentEffectData;
        this.buffManager = buffManager;
    }

    // 투척 시 전달한 방향을 우선 사용하고, 없으면 두 위치로 계산한다.
    // 원래 생성자 호출은 새 선택 매개변수를 생략해도 그대로 동작한다.
    public bool TryGetDirection(out Vector3 attackDirection)
    {
        attackDirection = direction;
        attackDirection.z = 0f;

        if (attackDirection.sqrMagnitude < 0.000001f)
        {
            attackDirection = targetPosition - usePosition;
            attackDirection.z = 0f;
        }

        if (attackDirection.sqrMagnitude < 0.000001f)
            return false;

        attackDirection.Normalize();
        return true;
    }

    public void SetCurrentEffect(ItemEffectData effectData)
    {
        currentEffectData = effectData;
    }

    public bool TryBeginEffectExecution(ItemEffectData effectData)
    {
        if (effectData == null || !CanContinue)
            return false;
        int depth = 0;
        for (ItemEffectContext ancestor = executionParent; ancestor != null; ancestor = ancestor.executionParent)
        {
            if (ancestor.currentEffectData == effectData || ++depth > 64)
                return false;
        }

        if (executingEffects == null)
            executingEffects = new HashSet<ItemEffectData>();

        if (executionBudget.remaining <= 0 || !executingEffects.Add(effectData))
            return false;
        executionBudget.remaining--;
        return true;
    }

    public void EndEffectExecution(ItemEffectData effectData)
    {
        if (executingEffects != null)
            executingEffects.Remove(effectData);
    }
}
