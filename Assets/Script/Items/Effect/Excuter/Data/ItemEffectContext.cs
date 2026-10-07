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

    public bool CanContinue => generation == ItemEffectRuntime.Generation &&
        (lifetime == null || !lifetime.IsCancelled);

    public ItemEffectLease RetainLifetime(bool completionOnly = false) =>
        lifetime != null && (!completionOnly || lifetime.TracksCompletion) ? lifetime.Retain() : null;

    public ItemEffectContext Copy(Vector3 position, Vector3 attackDirection)
    {
        return new ItemEffectContext(owner, sourceItemData, usePosition, position,
            sourceBag, currentEffectData, buffManager, attackDirection, plan)
        { lifetime = lifetime, plan = plan, executionParent = this, generation = generation };
    }

    public void InheritExecution(ItemEffectContext parent)
    {
        if (parent == null) return;
        lifetime = parent.lifetime;
        executionParent = parent;
        generation = parent.generation;
    }

    public T GetSnapshotStat<T>(ItemEffectData effect, T stat) where T : class, IGameStat<T>
    {
        return plan.GetSnapshot(effect, stat, this);
    }

    public T GetCurrentStat<T>(ItemEffectData effect, T stat) where T : class, IGameStat<T>
    {
        T snapshot = GetSnapshotStat(effect, stat);
        if (snapshot == null) return null;
        return buffManager != null
            ? buffManager.GetBuffedStatForItem(snapshot, sourceItemData, sourceBag, BuffCalculationMode.DynamicOnly)
            : snapshot.Clone();
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

        return executingEffects.Add(effectData);
    }

    public void EndEffectExecution(ItemEffectData effectData)
    {
        if (executingEffects != null)
            executingEffects.Remove(effectData);
    }
}
