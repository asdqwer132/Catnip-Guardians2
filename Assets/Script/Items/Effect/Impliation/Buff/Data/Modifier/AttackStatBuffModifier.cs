using UnityEngine;

[CreateAssetMenu(fileName = "AttackStatBuffModifier", menuName = "Game/Buff/Modifier/Attack Stat")]
public class AttackStatBuffModifier : BuffModifier
{
    [Header("Damage Area Power")]
    public float damageAreaPowerAdd;
    public float damageAreaPowerMultiply;

    [Header("Damage Area Interval")]
    public float damageAreaIntervalAdd;
    public float damageAreaIntervalMultiply;

    [Header("Damage Area Range")]
    public float damageAreaRangeAdd;
    public float damageAreaRangeMultiply;

    [Header("Damage Area Life Time")]
    public float damageAreaLifeTimeAdd;
    public float damageAreaLifeTimeMultiply;

    public override bool CanApplyTo(object stat, BuffQueryContext context)
    {
        return stat is DamageAreaAttackStat;
    }

    public override void ApplyTo(object stat, int stack, BuffQueryContext context)
    {
        DamageAreaAttackStat attackStat = stat as DamageAreaAttackStat;

        if (attackStat == null)
            return;

        int safeStack = Mathf.Max(1, stack);

        ApplyFloat(
            ref attackStat.damageAreaPower,
            damageAreaPowerAdd,
            damageAreaPowerMultiply,
            safeStack
        );

        ApplyFloat(
            ref attackStat.damageAreaInterval,
            damageAreaIntervalAdd,
            damageAreaIntervalMultiply,
            safeStack
        );

        ApplyFloat(
            ref attackStat.damageAreaRange,
            damageAreaRangeAdd,
            damageAreaRangeMultiply,
            safeStack
        );

        ApplyFloat(
            ref attackStat.damageAreaLifeTime,
            damageAreaLifeTimeAdd,
            damageAreaLifeTimeMultiply,
            safeStack
        );

        attackStat.Clamp();
    }

    private void ApplyFloat(ref float value, float add, float multiply, int stack)
    {
        value += add * stack;
        value *= 1f + multiply * stack;
    }
}