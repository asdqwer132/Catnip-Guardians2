using System;
using UnityEngine;

/// <summary>
/// BuffStat
/// 
/// 역할:
/// - 하나의 버프가 실제로 어떤 스탯을 바꾸는지 담는 통합 데이터.
/// - 공격, 버프 정보, 적, 스포너 등 여러 대상의 변경값을 한 곳에 묶는다.
/// 
/// 주의:
/// - 이 클래스는 직접 대상을 찾지 않는다.
/// - 어떤 대상에게 적용할지는 BuffRegistrar와 BuffStatCalculator가 결정한다.
/// </summary>
[Serializable]
public class BuffStat
{
    [Header("Attack Buff")]
    public AttackBuffStat attackBuffStat = new AttackBuffStat();

    [Header("Buff Info Buff")]
    public BuffInfoBuffStat buffInfoBuffStat = new BuffInfoBuffStat();

    [Header("Enemy Buff")]
    public EnemyBuffStat enemyBuffStat = new EnemyBuffStat();

    [Header("Enemy Spawner Buff")]
    public EnemySpawnerBuffStat enemySpawnerBuffStat = new EnemySpawnerBuffStat();

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

    public void ApplyToEnemySpawnerStat(EnemySpawnerStat target)
    {
        ApplyBuff(enemySpawnerBuffStat, target);
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