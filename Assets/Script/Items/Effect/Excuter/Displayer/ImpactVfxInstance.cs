using UnityEngine;

public class ImpactVfxInstance : MonoBehaviour
{
    private ItemEffectData effectData;
    private ItemEffectContext context;

    private Vector3 baseScale = Vector3.one;
    private bool useRadiusScale = true;

    private float lifeTime = 1f;
    private float timer = 0f;

    public void Init(
        ItemEffectData effectData,
        ItemEffectContext context,
        Vector3 baseScale,
        bool useRadiusScale,
        float lifeTime
    )
    {
        this.effectData = effectData;
        this.context = context;
        this.baseScale = baseScale;
        this.useRadiusScale = useRadiusScale;
        this.lifeTime = Mathf.Max(0.01f, lifeTime);

        timer = 0f;

        RefreshScale();
    }

    private void Update()
    {
        RefreshScale();

        timer += Time.deltaTime;

        if (timer >= lifeTime)
            Destroy(gameObject);
    }

    private void RefreshScale()
    {
        if (effectData == null || context == null)
            return;

        transform.localScale = effectData.GetCurrentImpactScale(
            context,
            baseScale,
            useRadiusScale
        );
    }
}