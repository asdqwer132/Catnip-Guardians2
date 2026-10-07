using UnityEngine;

[CreateAssetMenu(fileName = "BuffHitEffect", menuName = "GameData/Items/Hit Effects/Buff")]
public class BuffHitEffectData : HitEffectData
{
    [Header("Buff")]
    [Tooltip("기존 BuffEffect를 연결한다. Target Resolver 대신 명중한 적을 직접 대상으로 사용한다.")]
    public BuffEffect buffEffect;

    protected override bool ApplyEffect(HitEffectContext context)
    {
        if (buffEffect == null)
            return false;

        BuffManager manager = context.target.buffManager;
        if (manager == null)
            manager = context.buffManager != null ? context.buffManager : BuffManager.instance;

        if (manager == null)
        {
            Debug.LogWarning("BuffHitEffectData: BuffManager가 없습니다.", this);
            return false;
        }

        // 명중 버프는 Execute()를 거치지 않으므로 연결된 BuffEffect의 조건만 검사한다.
        // 연출이나 Target Resolver를 다시 실행하지 않는다.
        ItemEffectContext buffContext = context.CreateBuffContext(buffEffect, manager);
        if (!buffEffect.AreConditionsSatisfied(buffContext))
            return false;

        if (context.target.buffManager == null)
        {
            context.target.buffManager = manager;
            manager.RegisterBuffTarget(context.target);
        }

        return manager.RegisterBuffForTarget(
            buffEffect, buffContext, context.target
        );
    }
}
