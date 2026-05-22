using UnityEngine;
public enum EnemyBuffApplyMode
{
    CurrentEnemiesOnly,
    AllEnemiesIncludingFuture
}

[CreateAssetMenu(fileName = "EnemyStatData", menuName = "Game/Enemy/Enemy Stat Data")]
public class EnemyStatData : ScriptableObject
{
    [Header("Move")]
    public float speed = 2f;

    [Header("Health")]
    public float maxHp = 10f;

    [Header("Attack")]
    public float damage = 5f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;

    [Header("Reward")]
    public Cost[] reward;
    public float growEx = 10f;

    public EnemyStat CreateStat()
    {
        EnemyStat stat = new EnemyStat();

        stat.speed = speed;
        stat.maxHp = maxHp;
        stat.damage = damage;
        stat.attackRange = attackRange;
        stat.attackCooldown = attackCooldown;

        stat.Clamp();

        return stat;
    }
}