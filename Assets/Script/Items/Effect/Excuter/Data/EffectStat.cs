using System;
using UnityEngine;

[Serializable]
public class EffectStat
{
    [Header("Power")]
    public float attackPower = 0f;
    public float healPower = 0f;
    public float debuffPower = 0f;

    [Header("Effect")]
    [Tooltip("기본 아이템에서는 기본 범위, 버프/디버프에서는 범위 변화량")]
    public float effectRadius = 0f;

    [Tooltip("기본 아이템에서는 기본 횟수, 버프/디버프에서는 횟수 변화량")]
    public int effectCount = 0;

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

        clone.defensePower = defensePower;
        clone.speedPower = speedPower;

        clone.attackMultiplier = attackMultiplier;
        clone.healMultiplier = healMultiplier;
        clone.debuffMultiplier = debuffMultiplier;
        clone.defenseMultiplier = defenseMultiplier;
        clone.speedMultiplier = speedMultiplier;

        return clone;
    }
}