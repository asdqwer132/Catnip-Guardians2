using UnityEngine;

// 공유 에셋에는 설정만 저장한다. 남은 시간과 적용 대상은 에셋에 저장하지 않는다.
public abstract class HitEffectData : ScriptableObject
{
    [Header("Application")]
    [Range(0f, 1f)] public float applyChance = 1f;

    [Header("Visual / Audio")]
    public EffectVisualData visualData;
    [Tooltip("켜면 연출이 적을 따라갑니다. 적이 죽거나 풀로 반환되면 연출도 정리됩니다.")]
    public bool followTarget = true;

    [Header("End Visual / Audio")]
    public EffectVisualData endVisualData;
    protected virtual bool OwnsEndVisual => true;
    protected virtual bool RequiresLivingTarget => true;
    protected virtual bool CanApply(HitEffectContext context) => true;

    public bool TryExecute(HitEffectContext context)
    {
        if (context == null || !context.IsHitEventValid ||
            (RequiresLivingTarget && !context.IsTargetValid) || !CanApply(context))
            return false;

        float chance = Mathf.Clamp01(applyChance);
        if (chance <= 0f)
            return false;

        if (chance < 1f && Random.value >= chance)
            return false;

        bool applied = false;
        bool succeeded = false;
        EffectVisualData endVisual = OwnsEndVisual ? endVisualData : null;
        ItemEffectLifetime scope = new ItemEffectLifetime(context.lifetime, endVisual != null ? (System.Action)(() =>
        {
            if (applied && endVisual != null && context.IsHitEventValid &&
                (!RequiresLivingTarget || context.IsTargetValid))
                endVisual.Play(new EffectVisualContext(context.IsTargetValid ? context.target.transform.position :
                    context.hitPosition, Quaternion.identity));
        }) : null, trackCompletion: endVisual != null || (context.lifetime != null && context.lifetime.TracksCompletion));
        HitEffectContext execution = context.WithLifetime(scope);
        try
        {
            applied = ApplyEffect(execution);
            succeeded = true;
            if (applied && execution.IsHitEventValid) PlayHitVisual(execution);
            return applied;
        }
        finally { scope.Close(succeeded); }
    }

    protected virtual void PlayHitVisual(HitEffectContext context)
    {
        if (visualData == null || context == null || !context.IsHitEventValid ||
            (RequiresLivingTarget && !context.IsTargetValid))
            return;

        Vector3 position = context.IsTargetValid ? context.target.transform.position : context.hitPosition;
        position.z = 0f;

        // 명중 연출은 적 주변의 고정 크기다. 원래 공격 반경으로 확대하지 않는다.
        visualData.Play(new EffectVisualContext(
            position,
            Quaternion.identity,
            followTarget: followTarget && context.IsTargetValid ? context.target.transform : null,
            isPlaybackValid: () => context.IsHitEventValid && (!RequiresLivingTarget || context.IsTargetValid)
        ));
    }

    protected abstract bool ApplyEffect(HitEffectContext context);
}
