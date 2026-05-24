using UnityEngine;

public abstract class ItemEffectData : ScriptableObject
{
    [Header("Impact Visual")]
    public ImpactVfxInstance impactVfxPrefab;
    public Vector3 impactBaseScale = Vector3.one;
    public bool scaleImpactVfxByRadius = true;
    public bool useAnimatorClipLifeTime = true;
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

    #region Impact
    protected virtual void PlayImpactVfx(ItemEffectContext context)
    {
        if (impactVfxPrefab == null)
            return;

        Vector3 position = context.targetPosition;
        position.z = 0f;

        ImpactVfxInstance impact = Instantiate(impactVfxPrefab, position, Quaternion.identity);

        impact.Init(
            effectData: this,
            context: context,
            baseScale: impactBaseScale,
            useRadiusScale: scaleImpactVfxByRadius,
            lifeTime: impactVfxLifeTime,
            useAnimatorClipLifeTime: useAnimatorClipLifeTime
        );
    }
    public Vector3 GetCurrentImpactScale(ItemEffectContext context, Vector3 baseScale, bool useRadiusScale)
    {
        Vector3 finalScale = baseScale;

        if (useRadiusScale)
        {
            float radius = GetImpactRadius(context);
            radius = Mathf.Max(0.01f, radius);

            float diameter = radius * 2f;

            finalScale = Vector3.Scale(finalScale, new Vector3(diameter, diameter, 1f));
        }

        Vector3 additionalScale = GetAdditionalImpactScale(context);
        finalScale = Vector3.Scale(finalScale, additionalScale);

        return finalScale;
    }
    protected virtual float GetImpactRadius(ItemEffectContext context) { return 1f; }
    protected virtual Vector3 GetAdditionalImpactScale(ItemEffectContext context) { return Vector3.one; }
    #endregion
}