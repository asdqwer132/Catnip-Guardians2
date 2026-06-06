using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Target Specific Enemy Spawner", menuName = "Game/Buff/Target/Specific Enemy Spawner")]
public class SpecificEnemySpawnerTargetResolver : BuffTargetResolver
{
    public EnemySpawner targetEnemySpawner;

    public override void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results)
    {
        if (targetEnemySpawner == null)
            return;

        results.Add(BuffTargetHandle.EnemySpawner(targetEnemySpawner));
    }
}
