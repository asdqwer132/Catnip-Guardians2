
using UnityEngine;

[CreateAssetMenu(fileName = "Attack Stat Buff Modifier", menuName = "Game/Buff/Modifier/Attack Stat")]
public class AttackStatBuffModifier : BuffModifier
{
    [Header("Add")]
    public float attackPower;
    public float damageInterval;
    public float attackRange;
    public float attackLifeTime;

    [Header("Multiply")]
    public float attackPowerM;
    public float damageIntervalM;
    public float attackRangeM;
    public float attackLifeTimeM;

    public override bool CanApplyTo(object stat, BuffQueryContext query)
    {
        return stat is AttackStat;
    }

    public override void ApplyTo(object stat, int stack, BuffQueryContext query)
    {
        AttackStat target = stat as AttackStat;
        if (target == null)
            return;

        for (int i = 0; i < Mathf.Max(1, stack); i++)
        {
            target.attackPower += attackPower;
            target.damageInterval += damageInterval;
            target.attackRange += attackRange;
            target.attackLifeTime += attackLifeTime;

            target.attackPower *= 1f + attackPowerM;
            target.damageInterval *= 1f + damageIntervalM;
            target.attackRange *= 1f + attackRangeM;
            target.attackLifeTime *= 1f + attackLifeTimeM;
        }

        target.Clamp();
    }
}
