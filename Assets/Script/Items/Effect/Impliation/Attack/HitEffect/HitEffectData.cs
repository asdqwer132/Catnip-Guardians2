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

    public bool TryExecute(HitEffectContext context)
    {
        if (context == null || !context.IsTargetValid)
            return false;

        float chance = Mathf.Clamp01(applyChance);
        if (chance <= 0f)
            return false;

        if (chance < 1f && Random.value >= chance)
            return false;

        bool applied = ApplyEffect(context);

        // 확률 실패·면역·조건 실패 등 실제로 적용되지 않은 효과는 연출하지 않는다.
        // 효과 적용 중 사망/풀 반환이 일어나면 다른 생명에 연출을 붙이지 않는다.
        if (applied && context.IsTargetValid)
            PlayHitVisual(context);

        return applied;
    }

    protected virtual void PlayHitVisual(HitEffectContext context)
    {
        if (visualData == null || context == null || !context.IsTargetValid)
            return;

        Vector3 position = context.target.transform.position;
        position.z = 0f;

        // 명중 연출은 적 주변의 고정 크기다. 원래 공격 반경으로 확대하지 않는다.
        visualData.Play(new EffectVisualContext(
            position,
            Quaternion.identity,
            followTarget: followTarget ? context.target.transform : null,
            isPlaybackValid: () => context.IsTargetValid
        ));
    }

    protected abstract bool ApplyEffect(HitEffectContext context);
}
