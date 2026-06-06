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

[Serializable]
public class EnemySpawnerBuffStat : IBuffStat<EnemySpawnerStat>
{
    [Header("Spawn Interval")]
    public float spawnInterval = 0f;
    public float spawnIntervalM = 0f;

    [Header("Spawn Distance")]
    public float spawnDistance = 0f;
    public float spawnDistanceM = 0f;

    public void ApplyTo(EnemySpawnerStat target)
    {
        if (target == null)
            return;

        target.spawnInterval += spawnInterval;
        target.spawnInterval *= 1f + spawnIntervalM;

        target.spawnDistance += spawnDistance;
        target.spawnDistance *= 1f + spawnDistanceM;

        target.Clamp();
    }
}