using UnityEngine;

[CreateAssetMenu(fileName = "ThenEffect", menuName = "GameData/Items/Effects/Then Effects")]
public sealed class ThenEffectData : ItemEffectData
{
    public ItemEffectData[] effects;
    [Tooltip("하위 효과가 모두 자연 종료한 뒤 실행합니다. 장판만의 종료는 장판의 종료 설정을 사용하세요.")]
    public ItemEffectData[] afterCompletionEffects;
    public override void Prepare(ItemEffectContext context)
    {
        ItemEffectUtility.Prepare(context, effects);
        ItemEffectUtility.Prepare(context, afterCompletionEffects);
    }
    public override void ExecuteEffect(ItemEffectContext context)
    {
        ItemEffectContext child = context.Copy(context.targetPosition, context.direction);
        ItemEffectData[] next = ItemEffectUtility.Copy(afterCompletionEffects);
        ItemEffectLifetime scope = new ItemEffectLifetime(context.lifetime, () =>
        {
            if (context.CanContinue) ItemEffectUtility.Execute(next, context.Copy(child.targetPosition, child.direction));
        }, cancelOnChildFailure: true);
        child.lifetime = scope;
        bool succeeded = false;
        try { ItemEffectUtility.Execute(effects, child); succeeded = child.CanContinue; }
        finally { scope.Close(succeeded); }
    }
}
