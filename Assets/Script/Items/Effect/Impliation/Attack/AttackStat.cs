using System;
using UnityEngine;

[Serializable]
public class AttackStat : EffectStat
{
    [Header("Attack Power")]
    public float attackPower = 0f;

    [Header("Damage Apply")]
    public DamageApplyMode damageApplyMode = DamageApplyMode.HitOnce;

    [Tooltip("Periodic일 때 데미지가 들어가는 간격")]
    [Min(0.01f)]
    public float damageInterval = 0.5f;

    [Header("Multiplier")]
    public float attackMultiplier = 1f;

    public override void Add(EffectStat other)
    {
        if (other == null)
            return;

        base.Add(other);

        if (other is AttackStat otherAttack)
        {
            AddAttackOnly(otherAttack);
            return;
        }

        if (other is BuffStat buffStat)
        {
            buffStat.ApplyAttackBuffTo(this);
        }
    }

    private void AddAttackOnly(AttackStat otherAttack)
    {
        if (otherAttack == null)
            return;

        attackPower += otherAttack.attackPower;
        attackMultiplier += otherAttack.attackMultiplier - 1f;

        if (otherAttack.damageInterval > 0f)
        {
            if (damageInterval <= 0f)
                damageInterval = otherAttack.damageInterval;
            else
                damageInterval = Mathf.Min(damageInterval, otherAttack.damageInterval);
        }

        damageApplyMode = otherAttack.damageApplyMode;
    }

    public override void Reset()
    {
        base.Reset();

        attackPower = 0f;
        attackMultiplier = 1f;
        damageInterval = 0.5f;
        damageApplyMode = DamageApplyMode.HitOnce;
    }

    public override EffectStat Clone()
    {
        AttackStat clone = new AttackStat();

        clone.effectRadius = effectRadius;
        clone.effectCount = effectCount;
        clone.effectLifeTime = effectLifeTime;
        clone.effectDuration = effectDuration;

        clone.attackPower = attackPower;
        clone.attackMultiplier = attackMultiplier;
        clone.damageApplyMode = damageApplyMode;
        clone.damageInterval = damageInterval;

        return clone;
    }

    public AttackStat CloneAttack()
    {
        return Clone() as AttackStat;
    }

    public float GetAttackDamage()
    {
        return attackPower * attackMultiplier;
    }

    public float GetSafeDamageInterval()
    {
        return Mathf.Max(0.01f, damageInterval);
    }
}