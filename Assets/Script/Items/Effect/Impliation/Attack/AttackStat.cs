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

[Serializable]
public class AttackBuffStat : AttackStat, IBuffStat<AttackStat>
{
    [Header("Multiplier")]
    public float attackPowerM = 0f;
    public float damageIntervalM = 0f;
    public float attackRangeM = 0f;
    public float attackLifeTimeM = 0f;

    public void ApplyTo(AttackStat target)
    {
        if (target == null)
            return;

        target.attackPower += attackPower;
        target.damageInterval += damageInterval;
        target.attackRange += attackRange;
        target.attackLifeTime += attackLifeTime;

        target.attackPower *= 1f + attackPowerM;
        target.damageInterval *= 1f + damageIntervalM;
        target.attackRange *= 1f + attackRangeM;
        target.attackLifeTime *= 1f + attackLifeTimeM;

        target.Clamp();
    }

    public string GetSummaryText()
    {
        string result = "";

        if (attackPower != 0f)
            result += "공격력 +" + attackPower + " ";

        if (attackPowerM != 0f)
            result += "공격력 x" + (1f + attackPowerM).ToString("0.##") + " ";

        if (attackRange != 0f)
            result += "범위 +" + attackRange + " ";

        if (attackRangeM != 0f)
            result += "범위 x" + (1f + attackRangeM).ToString("0.##") + " ";

        if (attackLifeTime != 0f)
            result += "지속 +" + attackLifeTime + " ";

        if (attackLifeTimeM != 0f)
            result += "지속 x" + (1f + attackLifeTimeM).ToString("0.##") + " ";

        if (damageInterval != 0f)
            result += "간격 +" + damageInterval + " ";

        if (damageIntervalM != 0f)
            result += "간격 x" + (1f + damageIntervalM).ToString("0.##") + " ";

        return result.Trim();
    }
}