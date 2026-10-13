using UnityEngine;

[CreateAssetMenu(fileName = "ExecuteEffectsStatusReaction", menuName = "04.Items/Effects/Direct Effects/Special Status/Item Use Modules/Execute Effects")]
public sealed class ExecuteEffectsStatusReactionModule : SpecialStatusItemUseModule
{
    [Header("아이템 사용 후 추가 효과")]
    [Tooltip("사용한 아이템의 소유자·가방·도착 위치를 유지해 기존 공격/회복/버프/소환 효과를 실행합니다.")]
    public ItemEffectData[] effects;

    protected override bool CanPerform(ItemEffectContext context, ItemThrowExecutor throwExecutor)
    {
        if (effects != null)
            foreach (ItemEffectData effect in effects) if (effect != null) return true;
        return false;
    }

    protected override bool Perform(ItemEffectContext context, ItemThrowExecutor throwExecutor)
    {
        ItemEffectUtility.Execute(ItemEffectUtility.Copy(effects), ReactionContext(context));
        return context.CanContinue;
    }
}
