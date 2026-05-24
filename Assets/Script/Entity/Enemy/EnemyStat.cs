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

[Serializable]
public class EnemyBuffStat : EnemyStat, IBuffStat<EnemyStat>
{
    [Header("Enemy Target")]
    public bool affectDeadEnemies = false;
    public float enemyBuffRadius = 1f;
    public LayerMask enemyTargetLayer;

    [Header("Multiplier")]
    public float speedM = 0f;
    public float maxHpM = 0f;
    public float damageM = 0f;
    public float attackRangeM = 0f;
    public float attackCooldownM = 0f;

    public EnemyBuffStat()
    {
        speed = 0f;
        maxHp = 0f;
        damage = 0f;
        attackRange = 0f;
        attackCooldown = 0f;
    }

    public void ApplyTo(EnemyStat target)
    {
        if (target == null)
            return;

        target.speed += speed;
        target.maxHp += maxHp;
        target.damage += damage;
        target.attackRange += attackRange;
        target.attackCooldown += attackCooldown;

        target.speed *= 1f + speedM;
        target.maxHp *= 1f + maxHpM;
        target.damage *= 1f + damageM;
        target.attackRange *= 1f + attackRangeM;
        target.attackCooldown *= 1f + attackCooldownM;

        target.Clamp();
    }
}