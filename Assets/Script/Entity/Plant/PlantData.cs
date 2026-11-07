using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class EnemySpawnInfo
{
    public EnemyDataSet dataSet;

    [Tooltip("일반 적 배열에서 랜덤 선택될 확률입니다.")]
    public float spawnWeight = 1f;

    [Tooltip("일반 스포너에서는 반복 스폰 간격, 미들보스/보스 스포너에서는 등장까지 대기 시간입니다.")]
    public float spawnRate = 1f;
}

[CreateAssetMenu(menuName = "01.Entities/Plant/Plant Data")]
public class PlantData : DefaultData
{
    [Header("Extra Data / Visual")]
    [Tooltip("Growth order: first is the seed, last is fully grown. Intermediate sprites change at equal progress intervals.")]
    public Sprite[] growthSprites = new Sprite[0];

    public Sprite GetGrowthSprite(float progress01)
    {
        if (growthSprites == null || growthSprites.Length == 0) return null;
        int index = Mathf.FloorToInt(Mathf.Clamp01(progress01) * (growthSprites.Length - 1));
        return growthSprites[index];
    }

    [Header("Extra Data / Battle And Growth")]
    [Min(1f)] public float maxHP = 100f;
    [FormerlySerializedAs("growTime")]
    [Min(0f)] public float nutrientsRequired = 60f;
    [Min(0f)] public float nutrientsPerSecond = 1f;

    [Header("Extra Data / Reward")]
    public Cost[] reward;

    [Header("Extra Data / Normal Enemy Spawn")]
    public int spawnNormalEnemyCount = 1;
    public EnemySpawnInfo[] normalEnemies;

    [Header("Extra Data / Special Enemy Spawn")]
    public EnemySpawnInfo middleBossEnemies;
    public EnemySpawnInfo bossEnemies;
}
