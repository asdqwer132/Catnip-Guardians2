using System;
using UnityEngine;

public enum ItemUseDeliveryMode { Immediate, Throw }
public enum ItemUsePositionAnchor { TargetPosition, OwnerPosition, UsePosition }
public enum ItemUseOffsetSpace { World, ForwardAndSide }

// 상태 반응과 소환 무리가 함께 사용하는 아이템 전달 설정입니다.
[Serializable]
public sealed class ItemUseDeliverySettings
{
    public ItemUseDeliveryMode mode;
    [Header("투척 시작/목표")]
    public ItemUsePositionAnchor startAnchor;
    public ItemUsePositionAnchor targetAnchor;
    public ItemUseOffsetSpace offsetSpace;
    public Vector3 startOffset = new Vector3(0f, 3f, 0f);
    public Vector3 targetOffset;
    [Header("목표 배치")]
    public AttackPlacementMode placement;
    public AttackDirectionMode directionMode;
    public Vector2 fixedWorldDirection = Vector2.right;
    public float directionAngle;
    public float forwardOffset, sideOffset;
    [Min(0f)] public float radius = 2f;
    [Range(0f, 360f)] public float spreadAngle = 360f;
    [Min(0f)] public float shotgunRadiusOffset;
    public AttackSpreadDistribution shotgunDistribution;
    public AttackSpreadStartMode spreadStartMode;
    public bool clockwiseSpread;
    [Header("투척 프리팹/모션")]
    public ItemThrowOptions throwOptions = new ItemThrowOptions();

    public static bool CanUse(ItemData item, ItemUseDeliveryMode mode, ItemThrowExecutor executor)
        => item != null && (mode == ItemUseDeliveryMode.Immediate ? ItemEffectExecutor.CanExecuteItemEffect(item) :
            executor != null && executor.itemEffectExecutor != null);

    public bool TryUse(ItemData item, ItemEffectContext context, ItemThrowExecutor executor = null, bool consumeUseBuffs = false)
    {
        if (context == null || !context.CanContinue || !CanUse(item, mode, executor)) return false;
        ItemEffectContext useContext = context.Copy(context.targetPosition, context.direction);
        useContext.consumeUseBuffs = consumeUseBuffs && context.consumeUseBuffs;
        if (mode == ItemUseDeliveryMode.Throw)
        {
            Vector3 start, target;
            if (!TryGetThrowPath(context, out start, out target)) return false;
            return executor.Throw(item, start, target, context.owner, context.sourceBag, 0, useContext,
                triggerSpecialItems: false, options: throwOptions);
        }
        ItemEffectExecutor.ExecuteItem(item, context.usePosition, context.targetPosition, context.direction,
            context.owner, context.sourceBag, context.buffManager, useContext,
            triggerSpecialItems: false, consumeUseBuffs: consumeUseBuffs);
        return context.CanContinue;
    }

    public bool TryGetThrowPath(ItemEffectContext context, out Vector3 start, out Vector3 target)
    {
        start = target = Vector3.zero;
        if (context == null) return false;
        Vector3 forward = AttackPlacement.Direction(context, directionMode, fixedWorldDirection,
            EffectStatUtility.Safe(directionAngle, -360f, 360f, 0f));
        start = Anchor(context, startAnchor) + Offset(startOffset, forward);
        Vector3 origin = Anchor(context, targetAnchor) + Offset(targetOffset, forward);
        target = AttackPlacement.Position(placement, origin, forward, placement == AttackPlacementMode.Forward ? 1 : 0, 1,
            EffectStatUtility.Safe(forwardOffset, -100f, 100f, 0f), EffectStatUtility.Safe(sideOffset, -100f, 100f, 0f),
            EffectStatUtility.Safe(radius, 0f, 100f, 2f), EffectStatUtility.Safe(spreadAngle, 0f, 360f, 360f),
            EffectStatUtility.Safe(shotgunRadiusOffset, 0f, 100f, 0f), shotgunDistribution, spreadStartMode, clockwiseSpread);
        start.z = target.z = 0f;
        return true;
    }

    private static Vector3 Anchor(ItemEffectContext context, ItemUsePositionAnchor anchor)
    {
        if (anchor == ItemUsePositionAnchor.OwnerPosition)
            return context.owner != null ? context.owner.transform.position : context.usePosition;
        return anchor == ItemUsePositionAnchor.UsePosition ? context.usePosition : context.targetPosition;
    }

    private Vector3 Offset(Vector3 offset, Vector3 forward)
    {
        float x = EffectStatUtility.Safe(offset.x, -10000f, 10000f, 0f);
        float y = EffectStatUtility.Safe(offset.y, -10000f, 10000f, 0f);
        return offsetSpace == ItemUseOffsetSpace.ForwardAndSide
            ? forward * x + new Vector3(forward.y, -forward.x, 0f) * y : new Vector3(x, y, 0f);
    }
}
