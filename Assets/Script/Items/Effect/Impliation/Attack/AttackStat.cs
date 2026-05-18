using System;
using UnityEngine;

[Serializable]
public class AttackStat
{
    [Header("Attack Power")]
    public float attackPower = 0f;
    public float damageInterval = 0f;

    [Header("Effect")]
    public float attackRange = 0f;
    public float attackLifeTime = 0f;

    public AttackStat Clone()
    {
        return new AttackStat
        {
            attackPower = attackPower,
            damageInterval = damageInterval,
            attackRange = attackRange,
            attackLifeTime = attackLifeTime
        };
    }
}

[Serializable]
public class AttackBuffStat : AttackStat
{
    [Header("Multiplier")]
    public float attackPowerM = 0f;
    public float damageIntervalM = 0f;
    public float attackRangeM = 0f;
    public float attackLifeTimeM = 0f;
}