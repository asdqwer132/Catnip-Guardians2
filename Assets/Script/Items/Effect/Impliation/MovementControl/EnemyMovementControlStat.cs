using System;
using UnityEngine;

[Serializable]
public class EnemyMovementControlStat : IGameStat<EnemyMovementControlStat>, IEffectScalableStat
{
    [Tooltip("원형에서는 탐색 반지름. 다른 모양에서도 범위의 기본 크기로 사용합니다.")]
    [Min(0f)] public float range = 3f;
    [Tooltip("강제 이동 속도(유니티 거리/초). EaseOut에서는 시작 속도입니다.")]
    [Min(0f)] public float strength = 10f;
    [Min(0f)] public float duration = 0.35f;
    [Tooltip("이 효과로 움직일 최대 거리. 0이면 거리 제한 없이 지속시간까지 이동합니다.")]
    [Min(0f)] public float maxDistance = 3f;
    [Tooltip("당길 때 중심에서 이 거리만큼 떨어진 지점에 멈춥니다.")]
    [Min(0f)] public float pullStopDistance = 0.15f;

    [Header("Persistent Area")]
    [Tooltip("PersistentArea 모드에서 영역이 유지되는 시간(초). Duration은 1회 적용의 이동 시간입니다.")]
    [Min(0f)] public float areaDuration = 3f;
    [Tooltip("영역 안의 적을 다시 탐색하고 적용하는 간격(초). 최소 0.02초.")]
    [Min(0.02f)] public float tickInterval = 0.1f;

    public EnemyMovementControlStat Clone()
    {
        return new EnemyMovementControlStat
        {
            range = range,
            strength = strength,
            duration = duration,
            maxDistance = maxDistance,
            pullStopDistance = pullStopDistance,
            areaDuration = areaDuration,
            tickInterval = tickInterval
        };
    }

    public void Clamp()
    {
        range = NonNegative(range);
        strength = NonNegative(strength);
        duration = NonNegative(duration);
        maxDistance = NonNegative(maxDistance);
        pullStopDistance = NonNegative(pullStopDistance);
        areaDuration = NonNegative(areaDuration);
        tickInterval = Mathf.Max(0.02f, NonNegative(tickInterval));
    }

    private static float NonNegative(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
    }
    public void ApplyExecutionScale(EffectExecutionScale scale)
    {
        range *= scale.Range;
        maxDistance *= scale.Range;
        pullStopDistance *= scale.Range;
        duration *= scale.Duration;
        areaDuration *= scale.Duration;
    }
}
