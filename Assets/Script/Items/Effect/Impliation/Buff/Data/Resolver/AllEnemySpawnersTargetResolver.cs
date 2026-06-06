using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Target All Enemy Spawners", menuName = "Game/Buff/Target/All Enemy Spawners")]
public class AllEnemySpawnersTargetResolver : BuffTargetResolver
{
    public override void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results)
    {
        results.Add(BuffTargetHandle.AllEnemySpawners());
    }
}
