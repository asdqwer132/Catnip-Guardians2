using System;
using UnityEngine;

[Serializable]
public class DamageAreaAttackStat : IGameStat<DamageAreaAttackStat>
{
    [Header("Damage Area")]
    public float damageAreaPower = 0f;
    public float damageAreaInterval = 0.5f;
    public float damageAreaRange = 0.5f;
    public float damageAreaLifeTime = 0.5f;


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
}