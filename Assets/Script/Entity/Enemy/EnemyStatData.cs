using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyStatData",
    menuName = "Game/Enemy/Enemy Stat Data"
)]
public class EnemyStatData : ScriptableObject
{
    [Header("Stat")]
    public float speed = 2f;
    public float maxHp = 10f;

    [Header("Attack")]
    public float damage = 5f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;

    [Header("Reward")]
    public Cost[] reward;
    public float growEx = 10;
}