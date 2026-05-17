using System;
using UnityEngine;

[Serializable]
public class EffectStat
{
    [Header("Effect Basic")]
    public float effectRadius = 0f;

    [Tooltip("이펙트 횟수 / 투사체 수 / 적용 수 등으로 사용")]
    public int effectCount = 0;

    [Tooltip("장판, 공격 판정 오브젝트 등이 살아있는 시간")]
    public float effectLifeTime = 0f;

    [Tooltip("버프, 디버프, 상태이상 등이 유지되는 시간")]
    public float effectDuration = 0f;

    public virtual void Add(EffectStat other)
    {
        if (other == null)
            return;

        effectRadius += other.effectRadius;
        effectCount += other.effectCount;
        effectLifeTime += other.effectLifeTime;
        effectDuration += other.effectDuration;
    }

    public virtual void Reset()
    {
        effectRadius = 0f;
        effectCount = 0;
        effectLifeTime = 0f;
        effectDuration = 0f;
    }

    public virtual EffectStat Clone()
    {
        EffectStat clone = new EffectStat();

        clone.effectRadius = effectRadius;
        clone.effectCount = effectCount;
        clone.effectLifeTime = effectLifeTime;
        clone.effectDuration = effectDuration;

        return clone;
    }

    public float GetSafeRadius()
    {
        return Mathf.Max(0.01f, effectRadius);
    }

    public float GetSafeLifeTime()
    {
        return Mathf.Max(0.01f, effectLifeTime);
    }

    public float GetSafeDuration()
    {
        return Mathf.Max(0.01f, effectDuration);
    }
}