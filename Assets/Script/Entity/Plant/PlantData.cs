using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class EnemySpawnData
{
    public EnemyDataSet dataSet;
}

[System.Serializable]
public class EnemySpawnInfo : EnemySpawnData
{
    [Tooltip("일반 적 배열에서 랜덤 선택될 확률입니다.")]
    public float spawnWeight = 1f;

}

[System.Serializable]
public class SpecialEnemySpawnInfo : EnemySpawnData
{
    [Tooltip("Special enemy spawn interval in seconds. Independent of the normal enemy interval.")]
    [Min(.05f)]
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
    [Tooltip("Number of normal spawners to activate, starting from the first entry in Enemy Manager.")]
    [Min(0)]
    public int spawnNormalEnemyCount = 1;
    [Tooltip("Shared cooldown in seconds for all normal enemy choices and active normal spawners.")]
    [Min(.05f)] public float normalSpawnInterval = 1.5f;
    public EnemySpawnInfo[] normalEnemies;

    [Header("Extra Data / Special Enemy Spawn")]
    public SpecialEnemySpawnInfo[] middleBossEnemies = new SpecialEnemySpawnInfo[0];
    public SpecialEnemySpawnInfo[] bossEnemies = new SpecialEnemySpawnInfo[0];
}
