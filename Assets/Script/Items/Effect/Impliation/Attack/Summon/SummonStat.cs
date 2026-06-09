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
        if (summonThrowInterval < 0.01f)
            summonThrowInterval = 0.1f;

        if (summonAttackRange < 0f)
            summonAttackRange = 0f;

        if (summonLifeTime < 0.01f)
            summonLifeTime = 0.01f;
    }
}