using System;
using UnityEngine;

[Serializable]
public class EnemySpawnerStat : IGameStat<EnemySpawnerStat>
{
    [Header("Spawn")]
    public float spawnInterval = 1.5f;
    public float spawnDistance = 8f;
    
    public EnemySpawnerStat Clone()
    {
        return new EnemySpawnerStat
        {
            spawnInterval = spawnInterval,
            spawnDistance = spawnDistance
        };
    }

    public void Clamp()
    {
        spawnInterval = Mathf.Max(0.05f, spawnInterval);
        spawnDistance = Mathf.Max(0.01f, spawnDistance);
    }
}