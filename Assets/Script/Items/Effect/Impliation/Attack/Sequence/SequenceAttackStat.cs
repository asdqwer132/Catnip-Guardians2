using System;
using UnityEngine;

[Serializable]
public class SequenceAttackStat : IGameStat<SequenceAttackStat>
{
    [Header("Sequence")]
    [Tooltip("바운스는 첫 폭발 포함 횟수, 분산은 생성할 폭탄 수입니다. 반올림해 사용합니다.")]
    public float attackCount = 3f;
    [Tooltip("바운스는 착지 뒤 다음 이동까지, 분산은 다음 폭탄 발사까지의 시간입니다.")]
    public float attackInterval = 0.15f;
    public float forwardOffset = 1.5f;
    public float sideOffset;
    public float directionAngle;
    [Header("Scatter")]
    public float scatterRadius = 2f;
    public float spreadAngle = 360f;
    [Header("Flight / Bomb")]
    public float flightTime = 0.3f;
    public float arcHeight = 0.8f;
    public float bombDelay;
    public float bombLifetime = 5f;
    public float triggerRadius = 0.5f;

    public SequenceAttackStat Clone() => (SequenceAttackStat)MemberwiseClone();
    public void Clamp()
    {
        attackCount = EffectStatUtility.Safe(attackCount, 1f, 128f, 1f);
        attackInterval = EffectStatUtility.Safe(attackInterval, 0f, 60f, 0.15f);
        forwardOffset = EffectStatUtility.Safe(forwardOffset, -100f, 100f, 1.5f);
        sideOffset = EffectStatUtility.Safe(sideOffset, -100f, 100f, 0f);
        directionAngle = EffectStatUtility.Safe(directionAngle, -360f, 360f, 0f);
        scatterRadius = EffectStatUtility.Safe(scatterRadius, 0f, 100f, 2f);
        spreadAngle = EffectStatUtility.Safe(spreadAngle, 0f, 360f, 360f);
        flightTime = EffectStatUtility.Safe(flightTime, 0.01f, 60f, 0.3f);
        arcHeight = EffectStatUtility.Safe(arcHeight, 0f, 100f, 0.8f);
        bombDelay = EffectStatUtility.Safe(bombDelay, 0f, 600f, 0f);
        bombLifetime = EffectStatUtility.Safe(bombLifetime, 0.01f, 600f, 5f);
        triggerRadius = EffectStatUtility.Safe(triggerRadius, 0f, 100f, 0.5f);
    }
}

public static class EffectStatUtility
{
    public static float Safe(float value, float min, float max, float fallback)
        => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
    public static int Count(float value) => Mathf.Clamp(Mathf.RoundToInt(value), 1, 128);
}
