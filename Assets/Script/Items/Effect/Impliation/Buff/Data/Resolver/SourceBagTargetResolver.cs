using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Target Source Bag", menuName = "Game/Buff/Target/Source Bag")]
public class SourceBagTargetResolver : BuffTargetResolver
{
    public override void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results)
    {
        if (context == null || context.sourceBag == null)
            return;

        results.Add(BuffTargetHandle.Bag(context.sourceBag));
    }
}
