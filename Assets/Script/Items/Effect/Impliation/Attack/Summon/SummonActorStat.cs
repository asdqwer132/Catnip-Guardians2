using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;

[Serializable]
[MovedFrom(true, null, "Assembly-CSharp", "SummonStat")]
public sealed class SummonActorStat : IGameStat<SummonActorStat>, IEffectScalableStat
{
    [FormerlySerializedAs("summonAttackPower")]
    [Min(0f)] public float attackPower;
    [FormerlySerializedAs("summonThrowInterval")]
    [Min(0.01f)] public float attackInterval = 0.5f;
    [FormerlySerializedAs("summonAttackRange")]
    [Min(0.01f)] public float detectionRange = 0.5f;
    [FormerlySerializedAs("summonDamageMultiplier")]
    [Min(0f)] public float damageMultiplier = 1f;
    [FormerlySerializedAs("summonHealingMultiplier")]
    [Min(0f)] public float healingMultiplier = 1f;

    public SummonActorStat Clone() => (SummonActorStat)MemberwiseClone();
    public void Clamp()
    {
        attackPower = EffectStatUtility.Safe(attackPower, 0f, 1000000f, 0f);
        attackInterval = EffectStatUtility.Safe(attackInterval, 0.01f, 60f, 0.5f);
        detectionRange = EffectStatUtility.Safe(detectionRange, 0.01f, 100f, 0.5f);
        damageMultiplier = EffectStatUtility.Safe(damageMultiplier, 0f, 1000f, 1f);
        healingMultiplier = EffectStatUtility.Safe(healingMultiplier, 0f, 1000f, 1f);
    }

    // 피해/회복은 실제 공격 실행 경로에서 한 번 곱합니다. 소환 수명은 SpawnStat에 있습니다.
    public void ApplyExecutionScale(EffectExecutionScale scale) => detectionRange *= scale.Range;
}
