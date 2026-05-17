using System;
using UnityEngine;

[Serializable]
public class EffectStat
{
    [Header("Power")]
    public float attackPower = 0f;
    public float healPower = 0f;
    public float debuffPower = 0f;

    [Header("Effect Basic")]
    public float effectRadius = 0f;

    [Tooltip("이펙트 횟수 / 투사체 수 / 적용 수 등으로 사용")]
    public int effectCount = 0;

    [Tooltip("장판, 공격 판정 오브젝트 등이 살아있는 시간")]
    public float effectLifeTime = 0f;

    [Tooltip("버프, 디버프, 상태이상 등이 유지되는 시간")]
    public float effectDuration = 0f;

    [Tooltip("Periodic 데미지일 때 데미지가 들어가는 간격")]
    public float damageInterval = 0.5f;

    [Header("Defense / Speed")]
    public float defensePower = 0f;
    public float speedPower = 0f;

    [Header("Multiplier")]
    public float attackMultiplier = 1f;
    public float healMultiplier = 1f;
    public float debuffMultiplier = 1f;
    public float defenseMultiplier = 1f;
    public float speedMultiplier = 1f;

    public void Add(EffectStat other)
    {
        if (other == null)
            return;

        attackPower += other.attackPower;
        healPower += other.healPower;
        debuffPower += other.debuffPower;

        effectRadius += other.effectRadius;
        effectCount += other.effectCount;
        effectLifeTime += other.effectLifeTime;
        effectDuration += other.effectDuration;

        if (other.damageInterval > 0f)
        {
            if (damageInterval <= 0f)
                damageInterval = other.damageInterval;
            else
                damageInterval = Mathf.Min(damageInterval, other.damageInterval);
        }

        defensePower += other.defensePower;
        speedPower += other.speedPower;

        attackMultiplier += other.attackMultiplier - 1f;
        healMultiplier += other.healMultiplier - 1f;
        debuffMultiplier += other.debuffMultiplier - 1f;
        defenseMultiplier += other.defenseMultiplier - 1f;
        speedMultiplier += other.speedMultiplier - 1f;
    }

    public void Reset()
    {
        attackPower = 0f;
        healPower = 0f;
        debuffPower = 0f;

        effectRadius = 0f;
        effectCount = 0;
        effectLifeTime = 0f;
        effectDuration = 0f;
        damageInterval = 0.5f;

        defensePower = 0f;
        speedPower = 0f;

        attackMultiplier = 1f;
        healMultiplier = 1f;
        debuffMultiplier = 1f;
        defenseMultiplier = 1f;
        speedMultiplier = 1f;
    }

    public EffectStat Clone()
    {
        EffectStat clone = new EffectStat();

        clone.attackPower = attackPower;
        clone.healPower = healPower;
        clone.debuffPower = debuffPower;

        clone.effectRadius = effectRadius;
        clone.effectCount = effectCount;
        clone.effectLifeTime = effectLifeTime;
        clone.effectDuration = effectDuration;
        clone.damageInterval = damageInterval;

        clone.defensePower = defensePower;
        clone.speedPower = speedPower;

        clone.attackMultiplier = attackMultiplier;
        clone.healMultiplier = healMultiplier;
        clone.debuffMultiplier = debuffMultiplier;
        clone.defenseMultiplier = defenseMultiplier;
        clone.speedMultiplier = speedMultiplier;

        return clone;
    }

    public float GetAttackDamage()
    {
        return attackPower * attackMultiplier;
    }

    public float GetHealAmount()
    {
        return healPower * healMultiplier;
    }

    public float GetDebuffPower()
    {
        return debuffPower * debuffMultiplier;
    }

    public float GetDefenseValue()
    {
        return defensePower * defenseMultiplier;
    }

    public float GetSpeedValue()
    {
        return speedPower * speedMultiplier;
    }

    public float GetSafeRadius()
    {
        return Mathf.Max(0.01f, effectRadius);
    }

    public float GetSafeLifeTime()
    {
        return Mathf.Max(0.01f, effectLifeTime);
    }

    public float GetSafeDuration()
    {
        return Mathf.Max(0.01f, effectDuration);
    }

    public float GetSafeDamageInterval()
    {
        return Mathf.Max(0.01f, damageInterval);
    }
}