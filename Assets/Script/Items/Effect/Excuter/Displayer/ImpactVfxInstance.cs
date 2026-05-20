using System.Collections;
using UnityEngine;

public class ImpactVfxInstance : MonoBehaviour
{
    private ItemEffectData effectData;
    private ItemEffectContext context;

    private Vector3 baseScale = Vector3.one;
    private bool useRadiusScale = true;

    private float lifeTime = 1f;
    private float timer = 0f;

    private Animator animator;

    public void Init(
        ItemEffectData effectData,
        ItemEffectContext context,
        Vector3 baseScale,
        bool useRadiusScale,
        float lifeTime,
        bool useAnimatorClipLifeTime
    )
    {
        this.effectData = effectData;
        this.context = context;
        this.baseScale = baseScale;
        this.useRadiusScale = useRadiusScale;
        this.lifeTime = Mathf.Max(0.01f, lifeTime);

        timer = 0f;

        animator = GetComponentInChildren<Animator>(true);

        RefreshScale();

        if (useAnimatorClipLifeTime)
            StartCoroutine(SetLifeTimeFromCurrentAnimatorClip());
    }

    private IEnumerator SetLifeTimeFromCurrentAnimatorClip()
    {
        // Animator가 Entry 상태에서 실제 기본 State로 진입할 시간 확보
        yield return null;

        float clipLifeTime = GetCurrentAnimatorClipLength();

        if (clipLifeTime > 0f)
            lifeTime = clipLifeTime;
    }

    private float GetCurrentAnimatorClipLength()
    {
        if (animator == null)
            return -1f;

        // 현재 상태를 즉시 평가
        animator.Update(0f);

        int layerIndex = 0;

        AnimatorClipInfo[] clipInfos = animator.GetCurrentAnimatorClipInfo(layerIndex);

        if (clipInfos == null || clipInfos.Length == 0)
            return -1f;

        AnimationClip clip = clipInfos[0].clip;

        if (clip == null)
            return -1f;

        float speed = Mathf.Abs(animator.speed);

        if (speed <= 0.0001f)
            speed = 1f;

        return clip.length / speed;
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