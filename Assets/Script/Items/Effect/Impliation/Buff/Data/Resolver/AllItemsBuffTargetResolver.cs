using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AllItemsBuffTarget", menuName = "GameData/Buffs/Targets/All Items")]
public sealed class AllItemsBuffTargetResolver : BuffTargetResolver
{
    public override void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results)
    {
        if (results != null)
            results.Add(BuffTargetHandle.AllItems());
    }
}
