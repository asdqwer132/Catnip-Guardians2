using UnityEngine;

public sealed class HitEffectContext
{
    private readonly ItemEffectContext sourceContext;
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

    public bool IsTargetValid => target != null &&
        target.CanReceiveHitEffects && target.HitEffectLifeId == targetLifeId;

    public HitEffectContext(Enemy target, ItemEffectContext sourceContext)
    {
        this.sourceContext = sourceContext;
        lifetime = sourceContext != null ? sourceContext.lifetime : null;
        this.target = target;
        targetLifeId = target != null ? target.HitEffectLifeId : 0;
        hitPosition = target != null ? target.transform.position : Vector3.zero;

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
        return result;
    }

    public HitEffectContext WithLifetime(ItemEffectLifetime scope)
    {
        return new HitEffectContext(target, sourceContext) { lifetime = scope };
    }

    public ItemEffectContext CreateItemContext()
    {
        return CreateBuffContext(null, buffManager);
    }
}
