using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyStatData",
    menuName = "Game/Enemy/Enemy Stat Data"
)]
public class EnemyStatData : ScriptableObject
{
    [Header("Move")]
    public float speed = 2f;

    [Header("HP")]
    public float maxHp = 10f;

    [Header("Attack")]
    public float damage = 5f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;

    [Header("Reward")]
    public int goldReward = 10;
}