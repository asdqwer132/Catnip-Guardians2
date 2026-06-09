using UnityEngine;

[CreateAssetMenu(fileName = "AttackStatBuffModifier", menuName = "Game/Buff Modifier/Attack Stat")]
public class AttackStatBuffModifier : BuffModifier
{
    [Header("Attack Power")]
    public float attackPowerAdd;
    public float attackPowerMultiply;

    [Header("Damage Interval")]
    public float damageIntervalAdd;
    public float damageIntervalMultiply;

    [Header("Attack Range")]
    public float attackRangeAdd;
    public float attackRangeMultiply;

    [Header("Attack Life Time")]
    public float attackLifeTimeAdd;
    public float attackLifeTimeMultiply;

    public override bool CanApplyTo(object stat, BuffQueryContext context)
    {
        return stat is AttackStat;
    }

    public override void ApplyTo(object stat, int stack, BuffQueryContext context)
    {
        AttackStat attackStat = stat as AttackStat;

        if (attackStat == null)
            return;

        int safeStack = Mathf.Max(1, stack);

        ApplyFloat(ref attackStat.attackPower, attackPowerAdd, attackPowerMultiply, safeStack);
        ApplyFloat(ref attackStat.damageInterval, damageIntervalAdd, damageIntervalMultiply, safeStack);
        ApplyFloat(ref attackStat.attackRange, attackRangeAdd, attackRangeMultiply, safeStack);
        ApplyFloat(ref attackStat.attackLifeTime, attackLifeTimeAdd, attackLifeTimeMultiply, safeStack);

        attackStat.Clamp();
    }

    private void ApplyFloat(ref float value, float add, float multiply, int stack)
    {
        value += add * stack;
        value *= 1f + multiply * stack;
    }
}