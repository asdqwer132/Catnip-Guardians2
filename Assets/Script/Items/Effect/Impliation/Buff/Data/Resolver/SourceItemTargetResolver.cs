using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Target Source Item", menuName = "Game/Buff/Target/Source Item")]
public class SourceItemTargetResolver : BuffTargetResolver
{
    public override void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results)
    {
        if (context == null || context.sourceItemData == null)
            return;

        results.Add(BuffTargetHandle.Item(context.sourceItemData));
    }
}
