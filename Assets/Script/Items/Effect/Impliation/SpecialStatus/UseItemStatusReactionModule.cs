using UnityEngine;

public enum StatusReactionItemSource { UsedItem, ConfiguredItem }
public enum StatusReactionItemDelivery { ExecuteAtImpact, ThrowAtImpact }
public enum StatusReactionThrowAnchor { ImpactPosition, OwnerPosition, OriginalUsePosition }
public enum StatusReactionOffsetSpace { World, ForwardAndSide }

[CreateAssetMenu(fileName = "UseItemStatusReaction", menuName = "04.Items/Effects/Direct Effects/Special Status/Item Use Modules/Use Item")]
public sealed class UseItemStatusReactionModule : SpecialStatusItemUseModule
{
    [Header("추가 사용할 아이템")]
    public StatusReactionItemSource itemSource;
    [Tooltip("Item Source가 Configured Item일 때 사용합니다.")]
    public ItemData configuredItem;
    public StatusReactionItemDelivery delivery;
    [Header("투척 시작/목표")]
    public StatusReactionThrowAnchor throwStartAnchor;
    public StatusReactionThrowAnchor throwTargetAnchor;
    [Tooltip("World: 월드 XY / Forward And Side: X=기준 방향 전방, Y=오른쪽 옆 방향")]
    public StatusReactionOffsetSpace throwOffsetSpace;
    public Vector3 throwStartOffset = new Vector3(0f, 3f, 0f);
    public Vector3 throwTargetOffset;
    [Header("목표 배치 · 기존 Repeat Item과 같은 계산")]
    public AttackPlacementMode targetPlacement;
    public AttackDirectionMode directionMode;
    public Vector2 fixedWorldDirection = Vector2.right;
    public float directionAngle;
    public float forwardOffset;
    public float sideOffset;
    [Min(0f)] public float radius = 2f;
    [Range(0f, 360f)] public float spreadAngle = 360f;
    [Min(0f)] public float shotgunRadiusOffset;
    public AttackSpreadDistribution shotgunDistribution;
    public AttackSpreadStartMode spreadStartMode;
    public bool clockwiseSpread;
    [Header("이 모듈의 투척 프리팹/모션")]
    public ItemThrowOptions throwOptions = new ItemThrowOptions();
    [Header("추가 사용 소비")]
    [Tooltip("추가 사용도 횟수제 버프를 차감할지 설정합니다. 재고·가방 슬롯·쿨다운은 소비하지 않습니다.")]
    public bool consumeUseBuffs;

    public ItemData ResolveItem(ItemEffectContext context)
        => itemSource == StatusReactionItemSource.ConfiguredItem ? configuredItem : context?.sourceItemData;

    protected override bool CanPerform(ItemEffectContext context, ItemThrowExecutor throwExecutor)
    {
        ItemData item = ResolveItem(context);
        if (item == null) return false;
        if (delivery == StatusReactionItemDelivery.ExecuteAtImpact) return ItemEffectExecutor.CanExecuteItemEffect(item);
        // 미리보기에서 산탄·원형 배치나 비행 시간의 난수를 소비하지 않습니다.
        return throwExecutor != null && throwExecutor.itemEffectExecutor != null;
    }

    public bool TryGetThrowPath(ItemEffectContext context, out Vector3 start, out Vector3 target)
    {
        start = target = Vector3.zero;
        if (context == null) return false;
        Vector3 forward = AttackPlacement.Direction(context, directionMode, fixedWorldDirection,
            EffectStatUtility.Safe(directionAngle, -360f, 360f, 0f));
        start = Anchor(context, throwStartAnchor) + Offset(throwStartOffset, forward);
        Vector3 origin = Anchor(context, throwTargetAnchor) + Offset(throwTargetOffset, forward);
        // Forward는 원본에서 한 단계 이동합니다. 다른 배치는 추가 아이템 한 발의 위치입니다.
        target = AttackPlacement.Position(targetPlacement, origin, forward, targetPlacement == AttackPlacementMode.Forward ? 1 : 0, 1,
            EffectStatUtility.Safe(forwardOffset, -100f, 100f, 0f), EffectStatUtility.Safe(sideOffset, -100f, 100f, 0f),
            EffectStatUtility.Safe(radius, 0f, 100f, 2f), EffectStatUtility.Safe(spreadAngle, 0f, 360f, 360f),
            EffectStatUtility.Safe(shotgunRadiusOffset, 0f, 100f, 0f), shotgunDistribution, spreadStartMode, clockwiseSpread);
        start.z = target.z = 0f;
        return true;
    }

    private static Vector3 Anchor(ItemEffectContext context, StatusReactionThrowAnchor anchor)
    {
        if (anchor == StatusReactionThrowAnchor.OwnerPosition)
            return context.owner != null ? context.owner.transform.position : context.usePosition;
        return anchor == StatusReactionThrowAnchor.OriginalUsePosition ? context.usePosition : context.targetPosition;
    }

    private Vector3 Offset(Vector3 offset, Vector3 forward)
    {
        float x = EffectStatUtility.Safe(offset.x, -10000f, 10000f, 0f);
        float y = EffectStatUtility.Safe(offset.y, -10000f, 10000f, 0f);
        return throwOffsetSpace == StatusReactionOffsetSpace.ForwardAndSide
            ? forward * x + new Vector3(forward.y, -forward.x, 0f) * y : new Vector3(x, y, 0f);
    }

    protected override bool Perform(ItemEffectContext context, ItemThrowExecutor throwExecutor)
    {
        ItemData item = ResolveItem(context);
        ItemEffectContext child = ReactionContext(context, consumeUseBuffs);
        if (delivery == StatusReactionItemDelivery.ThrowAtImpact)
        {
            Vector3 start, target;
            if (!TryGetThrowPath(context, out start, out target)) return false;
            return throwExecutor.Throw(item, start, target, context.owner, context.sourceBag, 0, child,
                triggerSpecialItems: false, options: throwOptions);
        }
        ItemEffectExecutor.ExecuteItem(item, context.usePosition, context.targetPosition, context.direction,
            context.owner, context.sourceBag, context.buffManager, child,
            triggerSpecialItems: false, consumeUseBuffs: consumeUseBuffs);
        return context.CanContinue;
    }
}
