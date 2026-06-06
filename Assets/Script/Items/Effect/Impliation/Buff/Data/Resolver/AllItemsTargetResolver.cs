using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Target All Items", menuName = "Game/Buff/Target/All Items")]
public class AllItemsTargetResolver : BuffTargetResolver
{
    public override void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results)
    {
        results.Add(BuffTargetHandle.AllItems());
    }
}
