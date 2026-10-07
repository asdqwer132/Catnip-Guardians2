using System;
using UnityEngine;

[Serializable]
public class SummonStat : IGameStat<SummonStat>
{
    [Header("Summon Stat")]
    public float summonAttackPower = 0f;
    public float summonThrowInterval = 0.5f;
    public float summonAttackRange = 0.5f;
    public float summonLifeTime = 0.5f;


    public SummonStat Clone()
    {
        return new SummonStat
        {
            summonAttackPower = summonAttackPower,
            summonThrowInterval = summonThrowInterval,
            summonAttackRange = summonAttackRange,
            summonLifeTime = summonLifeTime,
        };
    }

    public void Clamp()
    {
        summonAttackPower = EffectStatUtility.Safe(summonAttackPower, 0f, 1000000f, 0f);
        summonThrowInterval = EffectStatUtility.Safe(summonThrowInterval, 0.01f, 60f, 0.5f);
        summonAttackRange = EffectStatUtility.Safe(summonAttackRange, 0.01f, 100f, 0.5f);
        summonLifeTime = EffectStatUtility.Safe(summonLifeTime, 0.01f, 600f, 5f);
    }
}
