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
    [Tooltip("Relative chance in Weighted One mode. Zero excludes this entry from weighted selection.")]
    [Min(0f)] public float spawnWeight = 1f;
    [Tooltip("Interval in seconds used only in Independent Intervals mode.")]
    [Min(.05f)]
    public float spawnRate = 1f;
}

[CreateAssetMenu(menuName = "Entities/Plant/Plant Data")]
public class PlantData : DefaultData
{
    [Space(10)]
    [Tooltip("성장 순서입니다. 첫 번째는 씨앗이고 마지막은 성장 완료입니다.")]
    public PlantBattleVisualData[] growthVisuals = System.Array.Empty<PlantBattleVisualData>();

    public int GetGrowthStageIndex(float progress01)
    {
        if (growthVisuals == null || growthVisuals.Length == 0) return -1;
        return Mathf.FloorToInt(Mathf.Clamp01(progress01) * (growthVisuals.Length - 1));
    }

    public Sprite GetGrowthSprite(float progress01)
    {
        int index = GetGrowthStageIndex(progress01);
        return index >= 0 ? growthVisuals[index]?.sprite : null;
    }

    [Space(10)]
    [Min(1f)] public float maxHP = 100f;
    [FormerlySerializedAs("growTime")]
    [Min(0f)] public float nutrientsRequired = 60f;
    [Min(0f)] public float nutrientsPerSecond = 1f;

    [Space(10)]
    public Cost[] reward;

    [Space(10)]
    [Tooltip("Number of normal spawners to activate, starting from the first entry in Enemy Manager.")]
    [Min(0)]
    public int spawnNormalEnemyCount = 1;
    [Tooltip("Shared cooldown in seconds for all normal enemy choices and active normal spawners.")]
    [Min(.05f)] public float normalSpawnInterval = 1.5f;
    public EnemySpawnInfo[] normalEnemies;

    [Space(10)]
    public SpecialEnemySpawnSettings middleBossSpawnSettings = new SpecialEnemySpawnSettings();
    public SpecialEnemySpawnInfo[] middleBossEnemies = new SpecialEnemySpawnInfo[0];
    [Space(10)]
    public SpecialEnemySpawnSettings bossSpawnSettings = new SpecialEnemySpawnSettings();
    public SpecialEnemySpawnInfo[] bossEnemies = new SpecialEnemySpawnInfo[0];
}
