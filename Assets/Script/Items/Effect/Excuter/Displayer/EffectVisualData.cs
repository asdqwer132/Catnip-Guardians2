using UnityEngine;

[CreateAssetMenu(fileName = "EffectVisual", menuName = "GameData/Effect/Visual")]
public class EffectVisualData : ScriptableObject
{
    [Header("Visual")]
    public ImpactVfxInstance impactVfxPrefab;
    public Vector3 impactBaseScale = Vector3.one;
    [Tooltip("아이템 연출: 공격 반경을 크기에 반영합니다. 명중 연출: 고정 크기를 사용합니다.")]
    public bool scaleImpactVfxByRadius = true;
    [Tooltip("Follow Target 재생의 World/Custom 파티클과 트레일도 각 VFX의 루트를 따라갑니다. 월드 잔상을 남길 연출은 끄세요.")]
    public bool attachParticlesToFollowTarget = true;

    [Header("Lifetime")]
    public bool useAnimatorClipLifeTime = true;
    [Min(0.01f)] public float impactVfxLifeTime = 1f;

    [Header("Audio")]
    [Tooltip("기존 AudioManager.PlaySfx에 전달하는 사운드 이름. 프리팹 없이 사운드만 재생할 수도 있습니다.")]
    public string audioSource;

    [Header("Pool")]
    [Min(0)]
    [Tooltip("앵커+VFX를 미리 준비할 개수. 0이면 필요한 수만 생성합니다. Prewarm()을 전투 전에 호출하면 처음 명중의 생성 비용도 줄어듭니다.")]
    public int prewarmCount;

    [Header("Debug")]
    [Tooltip("출력되는 Target/VFX/Anchor ID로 각 재생의 대상과 루트가 따로 연결되는지 확인합니다.")]
    public bool debugLog;

    public void Prewarm()
    {
        EffectVisualPool.Prewarm(impactVfxPrefab, prewarmCount);
    }

    public ImpactVfxInstance Play(EffectVisualContext context)
    {
        ImpactVfxInstance instance = PlayInternal(
            context, impactVfxPrefab, impactBaseScale,
            scaleImpactVfxByRadius, impactVfxLifeTime,
            useAnimatorClipLifeTime, audioSource, prewarmCount, attachParticlesToFollowTarget
        );

        if (debugLog && instance != null)
        {
            PooledObject pooled = ObjectPoolManager.FindManagedRoot(instance.gameObject);
            Transform target = instance.FollowTarget;
            string targetName = target != null ? target.name : "Position";
            Debug.Log("[EffectVisualData] " + name + " Target=" + targetName +
                " SpawnID=" + (pooled != null ? pooled.SpawnId : 0), instance);
        }
        return instance;
    }

    // 기존 에셋을 이전하기 전의 호환 경로도 동일한 생성 로직을 사용한다.
    public static ImpactVfxInstance PlayLegacy(
        EffectVisualContext context,
        ImpactVfxInstance prefab,
        Vector3 baseScale,
        bool useRadiusScale,
        float lifeTime,
        bool useAnimatorClipLifeTime,
        string audioSource
    )
    {
        return PlayInternal(
            context, prefab, baseScale, useRadiusScale, lifeTime,
            useAnimatorClipLifeTime, audioSource, 0, true
        );
    }

    private static ImpactVfxInstance PlayInternal(
        EffectVisualContext context,
        ImpactVfxInstance prefab,
        Vector3 baseScale,
        bool useRadiusScale,
        float lifeTime,
        bool useAnimatorClipLifeTime,
        string audioSource,
        int prewarmCount,
        bool attachParticlesToFollowTarget
    )
    {
        if (context == null || !context.CanContinuePlayback)
            return null;

        if (!string.IsNullOrWhiteSpace(audioSource) && AudioManager.instance != null)
            AudioManager.instance.PlaySfx(audioSource);

        if (prefab == null)
            return null;

        Vector3 position = context.position;
        position.z = 0f;
        EffectVisualAnchor anchor = EffectVisualPool.Spawn(
            prefab, position, context.rotation, prewarmCount
        );
        if (anchor == null)
            return null;

        ImpactVfxInstance instance = anchor.Visual;
        if (instance == null || !context.CanContinuePlayback)
        {
            anchor.ReturnToPool();
            return null;
        }

        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        instance.enabled = true;
        instance.gameObject.SetActive(true);

        anchor.Init(instance.transform, context);

        instance.Init(
            context, baseScale, useRadiusScale, lifeTime,
            useAnimatorClipLifeTime, anchor.transform, attachParticlesToFollowTarget
        );
        return anchor != null && anchor.IsCurrentPlayback(context) && instance.isActiveAndEnabled
            ? instance : null;
    }
}
