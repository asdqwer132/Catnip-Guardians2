using System;
using UnityEngine;

[Serializable]
public class AttackBuffStat
{
    [Header("Attack Bonus")]
    public float attackPower = 0f;
    public float attackMultiplier = 1f;

    [Header("Area Bonus")]
    public float effectRadius = 0f;
    public int effectCount = 0;
    public float effectLifeTime = 0f;

    [Header("Damage Tick Bonus")]
    [Tooltip("Periodic 데미지 간격 보너스. 낮을수록 강함")]
    public float damageIntervalMultiplier = 1f;

    public void Add(AttackBuffStat other)
    {
        if (other == null)
            return;

        attackPower += other.attackPower;
        attackMultiplier += other.attackMultiplier - 1f;

        effectRadius += other.effectRadius;
        effectCount += other.effectCount;
        effectLifeTime += other.effectLifeTime;

        damageIntervalMultiplier *= other.damageIntervalMultiplier;
    }

    public void Reset()
    {
        attackPower = 0f;
        attackMultiplier = 1f;

        effectRadius = 0f;
        effectCount = 0;
        effectLifeTime = 0f;

        damageIntervalMultiplier = 1f;
    }

    public AttackBuffStat Clone()
    {
        AttackBuffStat clone = new AttackBuffStat();

        clone.attackPower = attackPower;
        clone.attackMultiplier = attackMultiplier;

        clone.effectRadius = effectRadius;
        clone.effectCount = effectCount;
        clone.effectLifeTime = effectLifeTime;

        clone.damageIntervalMultiplier = damageIntervalMultiplier;

        return clone;
    }

    public void ApplyTo(AttackStat attackStat)
    {
        if (attackStat == null)
            return;

        attackStat.attackPower += attackPower;
        attackStat.attackMultiplier += attackMultiplier - 1f;

        attackStat.effectRadius += effectRadius;
        attackStat.effectCount += effectCount;
        attackStat.effectLifeTime += effectLifeTime;

        attackStat.damageInterval *= Mathf.Max(0.01f, damageIntervalMultiplier);
    }
}