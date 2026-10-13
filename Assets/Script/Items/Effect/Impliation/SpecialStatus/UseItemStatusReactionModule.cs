using UnityEngine;

public enum StatusReactionItemSource { UsedItem, ConfiguredItem }
public enum StatusReactionItemDelivery { ExecuteAtImpact, ThrowAtImpact }

[CreateAssetMenu(fileName = "UseItemStatusReaction", menuName = "04.Items/Effects/Direct Effects/Special Status/Item Use Modules/Use Item")]
public sealed class UseItemStatusReactionModule : SpecialStatusItemUseModule
{
    [Header("추가 사용할 아이템")]
    public StatusReactionItemSource itemSource;
    [Tooltip("Item Source가 Configured Item일 때 사용합니다.")]
    public ItemData configuredItem;
    public StatusReactionItemDelivery delivery;
    [Tooltip("Throw At Impact: 도착 위치에 이 월드 오프셋을 더한 곳에서 던집니다.")]
    public Vector3 throwStartOffset = new Vector3(0f, 3f, 0f);
    [Tooltip("추가 사용도 횟수제 버프를 차감할지 설정합니다. 재고·가방 슬롯·쿨다운은 소비하지 않습니다.")]
    public bool consumeUseBuffs;

    public ItemData ResolveItem(ItemEffectContext context)
        => itemSource == StatusReactionItemSource.ConfiguredItem ? configuredItem : context?.sourceItemData;

    protected override bool CanPerform(ItemEffectContext context, ItemThrowExecutor throwExecutor)
    {
        ItemData item = ResolveItem(context);
        if (item == null) return false;
        if (delivery == StatusReactionItemDelivery.ExecuteAtImpact) return ItemEffectExecutor.CanExecuteItemEffect(item);
        return throwExecutor != null && throwExecutor.itemEffectExecutor != null &&
            ((Vector2)throwStartOffset).sqrMagnitude > 0.0001f;
    }

    protected override bool Perform(ItemEffectContext context, ItemThrowExecutor throwExecutor)
    {
        ItemData item = ResolveItem(context);
        ItemEffectContext child = ReactionContext(context, consumeUseBuffs);
        if (delivery == StatusReactionItemDelivery.ThrowAtImpact)
            return throwExecutor.Throw(item, context.targetPosition + throwStartOffset, context.targetPosition,
                context.owner, context.sourceBag, 0, child, triggerSpecialItems: false);
        ItemEffectExecutor.ExecuteItem(item, context.usePosition, context.targetPosition, context.direction,
            context.owner, context.sourceBag, context.buffManager, child,
            triggerSpecialItems: false, consumeUseBuffs: consumeUseBuffs);
        return context.CanContinue;
    }
}
