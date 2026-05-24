using System;
using UnityEngine;

[Serializable]
public class BuffStat
{
    [Header("Attack Buff")]
    public AttackBuffStat attackBuffStat = new AttackBuffStat();

    [Header("Buff Info Buff")]
    public BuffInfoBuffStat buffInfoBuffStat = new BuffInfoBuffStat();

    [Header("Enemy Buff")]
    public EnemyBuffStat enemyBuffStat = new EnemyBuffStat();

    public void ApplyToAttackStat(AttackStat target)
    {
        ApplyBuff(attackBuffStat, target);
    }

    public void ApplyToBuffInfo(BuffInfo target)
    {
        ApplyBuff(buffInfoBuffStat, target);
    }

    public void ApplyToEnemyStat(EnemyStat target)
    {
        ApplyBuff(enemyBuffStat, target);
    }

    private void ApplyBuff<T>(IBuffStat<T> buffStat, T target)
    {
        if (buffStat == null)
            return;

        if (target == null)
            return;

        buffStat.ApplyTo(target);
    }
}