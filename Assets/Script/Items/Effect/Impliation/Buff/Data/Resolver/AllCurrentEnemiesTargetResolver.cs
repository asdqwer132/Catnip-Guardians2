using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Target All Current Enemies", menuName = "Game/Buff/Target/All Current Enemies")]
public class AllCurrentEnemiesTargetResolver : BuffTargetResolver
{
    public override void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results)
    {
        if (context == null || context.buffManager == null)
            return;

        List<Enemy> enemies = context.buffManager.GetRegisteredEnemiesUnsafe();
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null)
                results.Add(BuffTargetHandle.Enemy(enemies[i]));
        }
    }
}
