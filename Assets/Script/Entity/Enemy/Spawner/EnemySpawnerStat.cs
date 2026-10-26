using System;
using UnityEngine;

[Serializable]
public class EnemySpawnerStat : IGameStat<EnemySpawnerStat>
{
    [Header("Spawn")]
    public float spawnInterval = 1.5f;
    public float spawnDistance = 8f;

    [Header("Rectangle Size")]
    [Min(0.01f)] public float spawnWidth = 16f;
    [Min(0.01f)] public float spawnHeight = 2f;
    
    public EnemySpawnerStat Clone()
    {
        return new EnemySpawnerStat
        {
            spawnInterval = spawnInterval,
            spawnDistance = spawnDistance,
            spawnWidth = spawnWidth,
            spawnHeight = spawnHeight
        };
    }

    public void Clamp()
    {
        spawnInterval = Mathf.Max(0.05f, spawnInterval);
        spawnDistance = Mathf.Max(0.01f, spawnDistance);
        spawnWidth = EffectStatUtility.Safe(spawnWidth, 0.01f, 100000f, 16f);
        spawnHeight = EffectStatUtility.Safe(spawnHeight, 0.01f, 100000f, 2f);
    }
}
