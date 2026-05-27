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
    [Header("Visual")]
    public Sprite seed;
    public Sprite[] growing;
    public Sprite grownUp;

    [Header("Stat")]
    public float maxHP = 100f;
    public float growTime = 60f;

    [Header("Resource")]
    public Cost[] reward;
    public Cost[] buyCosts;

    [Header("Spawn Enemies")]
    public EnemySpawnInfo[] enemies;
}