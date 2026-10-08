using System;
using UnityEngine;

[Serializable]
public class SummonStat : IGameStat<SummonStat>, IEffectScalableStat
{
    [Header("Summon Stat")]
    public float summonAttackPower = 0f;
    public float summonThrowInterval = 0.5f;
    public float summonAttackRange = 0.5f;
    public float summonLifeTime = 0.5f;
    public float summonDamageMultiplier = 1f;
    public float summonHealingMultiplier = 1f;


    public SummonStat Clone()
    {
        return new SummonStat
        {
            summonAttackPower = summonAttackPower,
            summonThrowInterval = summonThrowInterval,
            summonAttackRange = summonAttackRange,
            summonLifeTime = summonLifeTime,
            summonDamageMultiplier = summonDamageMultiplier,
            summonHealingMultiplier = summonHealingMultiplier,
        };
    }

    public void Clamp()
    {
        summonAttackPower = EffectStatUtility.Safe(summonAttackPower, 0f, 1000000f, 0f);
        summonThrowInterval = EffectStatUtility.Safe(summonThrowInterval, 0.01f, 60f, 0.5f);
        summonAttackRange = EffectStatUtility.Safe(summonAttackRange, 0.01f, 100f, 0.5f);
        summonLifeTime = EffectStatUtility.Safe(summonLifeTime, 0.01f, 600f, 5f);
        summonDamageMultiplier = EffectStatUtility.Safe(summonDamageMultiplier, 0f, 1000f, 1f);
        summonHealingMultiplier = EffectStatUtility.Safe(summonHealingMultiplier, 0f, 1000f, 1f);
    }

    // 피해/회복 배율은 공격 모듈 및 자식 아이템 컨텍스트에서 한 번만 적용한다.
    public void ApplyExecutionScale(EffectExecutionScale scale)
    {
        summonAttackRange *= scale.Range;
        summonLifeTime *= scale.Duration;
    }
}
