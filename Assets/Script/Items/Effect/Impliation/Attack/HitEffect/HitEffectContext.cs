using UnityEngine;

public sealed class HitEffectContext
{
    private readonly ItemEffectContext sourceContext;
    public ItemEffectContext SourceContext => sourceContext;
    public readonly HitEffectAttackState attackState;
    public ItemEffectLifetime lifetime;
    public readonly Enemy target;
    public readonly int targetLifeId;
    public readonly GameObject owner;
    public readonly ItemData sourceItemData;
    public readonly EquipmentBag sourceBag;
    public readonly ItemEffectData sourceEffectData;
    public readonly BuffManager buffManager;
    public readonly Vector3 usePosition;
    public readonly Vector3 hitPosition;
    public readonly Vector3 attackDirection;

    public bool IsHitEventValid => (sourceContext == null || sourceContext.CanContinue) &&
        (lifetime == null || !lifetime.IsCancelled);

    public bool IsTargetValid => target != null &&
        target.CanReceiveHitEffects && target.HitEffectLifeId == targetLifeId;

    public HitEffectContext(Enemy target, ItemEffectContext sourceContext, HitEffectAttackState attackState = null)
        : this(target, sourceContext, target != null ? target.HitEffectLifeId : 0,
            target != null ? target.transform.position : Vector3.zero, attackState ?? new HitEffectAttackState()) { }

    private HitEffectContext(Enemy target, ItemEffectContext sourceContext, int lifeId,
        Vector3 position, HitEffectAttackState attackState)
    {
        this.sourceContext = sourceContext;
        this.attackState = attackState;
        lifetime = sourceContext != null ? sourceContext.lifetime : null;
        this.target = target;
        targetLifeId = lifeId;
        hitPosition = position;
        attackDirection = sourceContext != null ? sourceContext.direction : Vector3.zero;

        if (sourceContext == null)
            return;

        owner = sourceContext.owner;
        sourceItemData = sourceContext.sourceItemData;
        sourceBag = sourceContext.sourceBag;
        sourceEffectData = sourceContext.currentEffectData;
        buffManager = sourceContext.buffManager;
        usePosition = sourceContext.usePosition;
    }

    public ItemEffectContext CreateBuffContext(BuffEffect effect, BuffManager manager)
    {
        ItemEffectContext result = new ItemEffectContext(
            owner, sourceItemData, usePosition, hitPosition, sourceBag, effect, manager,
            sourceContext != null ? sourceContext.direction : Vector3.zero,
            sourceContext != null ? sourceContext.plan : null
        );
        result.InheritExecution(sourceContext);
        result.lifetime = lifetime;
        result.hitTarget = target;
        result.hitTargetLifeId = targetLifeId;
        return result;
    }

    public HitEffectContext WithLifetime(ItemEffectLifetime scope)
    {
        return new HitEffectContext(target, sourceContext, targetLifeId, hitPosition, attackState) { lifetime = scope };
    }

    public ItemEffectContext CreateItemContext()
    {
        return CreateBuffContext(null, buffManager);
    }
}
