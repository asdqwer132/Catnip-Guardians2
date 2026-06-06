using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Target All Enemies Including Future", menuName = "Game/Buff/Target/All Enemies Including Future")]
public class AllEnemiesIncludingFutureTargetResolver : BuffTargetResolver
{
    public override void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results)
    {
        results.Add(BuffTargetHandle.AllEnemiesIncludingFuture());
    }
}
