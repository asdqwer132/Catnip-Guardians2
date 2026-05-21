using UnityEngine;

[System.Serializable]
public class EnemySpawnInfo
{
    public GameObject enemyPrefab;
    public float spawnWeight = 1f;
}
[CreateAssetMenu(menuName = "Game/Plant Data")]
public class PlantData : DefaultData
{
    [Header("Reward")]
    public Cost[] reward;

    [Header("Stat")]
    public float maxHP = 100f;
    public float growTime = 60f;

    [Header("Buy")]
    public Cost[] buyCosts;

    [Header("Spawn Enemies")]
    public EnemySpawnInfo[] enemies;
}