using System;
using UnityEngine;

// 장면별 실행 정보다. 공유 ScriptableObject에 대상/타이머/공격 컨텍스트를 저장하지 않는다.
public sealed class EffectVisualContext
{
    public readonly Vector3 position;
    public readonly Quaternion rotation;
    public readonly Transform followTarget;
    public readonly bool hasFollowTarget;

    private readonly Func<Vector3, bool, Vector3> scaleResolver;
    private readonly Func<bool> isPlaybackValid;

    public EffectVisualContext(
        Vector3 position,
        Quaternion rotation,
        Func<Vector3, bool, Vector3> scaleResolver = null,
        Transform followTarget = null,
        Func<bool> isPlaybackValid = null
    )
    {
        this.position = position;
        this.rotation = rotation;
        this.scaleResolver = scaleResolver;
        this.followTarget = followTarget;
        this.isPlaybackValid = isPlaybackValid;
        hasFollowTarget = followTarget != null;
    }

    public bool CanContinuePlayback
    {
        get
        {
            if (isPlaybackValid != null && !isPlaybackValid())
                return false;

            return !hasFollowTarget ||
                (followTarget != null && followTarget.gameObject.activeInHierarchy);
        }
    }

    public Vector3 GetCurrentScale(Vector3 baseScale, bool useRadiusScale)
    {
        return scaleResolver != null ? scaleResolver(baseScale, useRadiusScale) : baseScale;
    }
}
