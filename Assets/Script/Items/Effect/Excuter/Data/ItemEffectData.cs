using UnityEngine;

public abstract class ItemEffectData : ScriptableObject
{
    [Header("Impact Visual")]
    public ImpactVfxInstance impactVfxPrefab;

    [Tooltip("임팩트 기본 스케일")]
    public Vector3 impactBaseScale = Vector3.one;

    [Tooltip("범위 기반으로 임팩트 크기 조절")]
    public bool scaleImpactVfxByRadius = true;

    [Min(0.01f)]
    public float impactVfxLifeTime = 1f;

    public void Execute(ItemEffectContext context)
    {
        if (context == null)
            return;

        PlayImpactVfx(context);

        ExecuteEffect(context);
    }

    public abstract void ExecuteEffect(ItemEffectContext context);

    protected virtual void PlayImpactVfx(ItemEffectContext context)
    {
        if (impactVfxPrefab == null)
            return;

        Vector3 position = context.targetPosition;
        position.z = 0f;

        ImpactVfxInstance impact = Instantiate(
            impactVfxPrefab,
            position,
            Quaternion.identity
        );

        impact.Init(
            effectData: this,
            context: context,
            baseScale: impactBaseScale,
            useRadiusScale: scaleImpactVfxByRadius,
            lifeTime: impactVfxLifeTime
        );
    }

    public Vector3 GetCurrentImpactScale(
        ItemEffectContext context,
        Vector3 baseScale,
        bool useRadiusScale
    )
    {
        Vector3 finalScale = baseScale;

        if (useRadiusScale)
        {
            float radius = GetImpactRadius(context);
            radius = Mathf.Max(0.01f, radius);

            float diameter = radius * 2f;

            finalScale = Vector3.Scale(
                finalScale,
                new Vector3(diameter, diameter, 1f)
            );
        }

        Vector3 additionalScale = GetAdditionalImpactScale(context);
        finalScale = Vector3.Scale(finalScale, additionalScale);

        return finalScale;
    }

    protected virtual float GetImpactRadius(ItemEffectContext context)
    {
        return 1f;
    }

    protected virtual Vector3 GetAdditionalImpactScale(ItemEffectContext context)
    {
        return Vector3.one;
    }
}