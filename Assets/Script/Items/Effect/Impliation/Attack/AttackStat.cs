using System;
using UnityEngine;

[Serializable]
public class AttackStat : IGameStat<AttackStat>
{
    [Header("Attack Power")]
    public float attackPower = 0f;
    public float damageInterval = 0.5f;

    [Header("Effect")]
    public float attackRange = 0.5f;

    [Min(0.01f)]
    public float attackLifeTime = 0.5f;

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

    public void Clamp()
    {
        if (damageInterval < 0.01f)
            damageInterval = 0.01f;

        if (attackRange < 0f)
            attackRange = 0f;

        if (attackLifeTime < 0.01f)
            attackLifeTime = 0.01f;
    }
}
