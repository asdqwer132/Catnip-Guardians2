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
        => ItemUseDeliverySettings.CanUse(ResolveItem(context), (ItemUseDeliveryMode)delivery, throwExecutor);

    // 기존 에셋의 필드와 Inspector를 유지하면서 공용 전달 코드에 설정을 넘깁니다.
    public ItemUseDeliverySettings CreateDeliverySettings() => new ItemUseDeliverySettings {
        mode = (ItemUseDeliveryMode)delivery, startAnchor = (ItemUsePositionAnchor)throwStartAnchor,
        targetAnchor = (ItemUsePositionAnchor)throwTargetAnchor, offsetSpace = (ItemUseOffsetSpace)throwOffsetSpace,
        startOffset = throwStartOffset, targetOffset = throwTargetOffset, placement = targetPlacement,
        directionMode = directionMode, fixedWorldDirection = fixedWorldDirection, directionAngle = directionAngle,
        forwardOffset = forwardOffset, sideOffset = sideOffset, radius = radius, spreadAngle = spreadAngle,
        shotgunRadiusOffset = shotgunRadiusOffset, shotgunDistribution = shotgunDistribution,
        spreadStartMode = spreadStartMode, clockwiseSpread = clockwiseSpread, throwOptions = throwOptions
    };

    public bool TryGetThrowPath(ItemEffectContext context, out Vector3 start, out Vector3 target)
        => CreateDeliverySettings().TryGetThrowPath(context, out start, out target);

    protected override bool Perform(ItemEffectContext context, ItemThrowExecutor throwExecutor)
        => CreateDeliverySettings().TryUse(ResolveItem(context), ReactionContext(context, consumeUseBuffs), throwExecutor, consumeUseBuffs);
}
