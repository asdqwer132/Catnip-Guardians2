using System;
using UnityEngine;

[Serializable]
public class DamageAreaAttackStat : IGameStat<DamageAreaAttackStat>, IEffectScalableStat
{
    [Header("Damage Area")]
    public float damageAreaPower = 0f;
    public float damageAreaInterval = 0f;
    [Min(0.1f)] public float damageAreaRange = 0.5f;
    [Min(0.1f)] public float damageAreaLifeTime = 0.1f;

    //

    public DamageAreaAttackStat Clone()
    {
        return new DamageAreaAttackStat
        {
            damageAreaPower = damageAreaPower,
            damageAreaInterval = damageAreaInterval,
            damageAreaRange = damageAreaRange,
            damageAreaLifeTime = damageAreaLifeTime,
        };
    }

    public void Clamp()
    {
        if (damageAreaInterval < 0.01f)
            damageAreaInterval = 0.01f;

        if (damageAreaRange < 0f)
            damageAreaRange = 0f;

        if (damageAreaLifeTime < 0.01f)
            damageAreaLifeTime = 0.01f;
    }
    public void ApplyExecutionScale(EffectExecutionScale scale)
    {
        damageAreaPower = scale.ScaleDamage(damageAreaPower);
        damageAreaRange *= scale.Range;
        damageAreaLifeTime *= scale.Duration;
    }
}
