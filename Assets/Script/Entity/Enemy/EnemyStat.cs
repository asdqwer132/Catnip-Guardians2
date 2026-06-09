using System;
using UnityEngine;

[Serializable]
public class EnemyStat : IGameStat<EnemyStat>
{
    [Header("Move")]
    public float speed = 2f;

    [Header("Health")]
    public float maxHp = 10f;

    [Header("Attack")]
    public float damage = 5f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;

    public EnemyStat Clone()
    {
        return new EnemyStat
        {
            speed = speed,
            maxHp = maxHp,
            damage = damage,
            attackRange = attackRange,
            attackCooldown = attackCooldown
        };
    }

    public void Clamp()
    {
        if (speed < 0f)
            speed = 0f;
        if (maxHp < 1f)
            maxHp = 1f;
        if (damage < 0f)
            damage = 0f;
        if (attackRange < 0f)
            attackRange = 0f;
        if (attackCooldown < 0.01f)
            attackCooldown = 0.01f;
    }
}
