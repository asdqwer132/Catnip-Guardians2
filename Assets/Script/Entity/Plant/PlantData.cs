using UnityEngine;

[System.Serializable]
public class EnemySpawnInfo
{
    public GameObject enemyPrefab;

    [Tooltip("일반 적 배열에서 랜덤 선택될 확률입니다.")]
    public float spawnWeight = 1f;

    [Tooltip("일반 스포너에서는 반복 스폰 간격, 미들보스/보스 스포너에서는 등장까지 대기 시간입니다.")]
    public float spawnRate = 1f;
}

[CreateAssetMenu(menuName = "GameData/Plant Data")]
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

    [Header("Normal Enemy Spawn")]
    public int spawnNormalEnemyCount = 1;
    public EnemySpawnInfo[] normalEnemies;

    [Header("Special Enemy Spawn")]
    public EnemySpawnInfo middleBossEnemies;
    public EnemySpawnInfo bossEnemies;
}