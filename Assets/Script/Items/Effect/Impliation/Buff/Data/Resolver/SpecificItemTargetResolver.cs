using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Target Specific Item", menuName = "Game/Buff/Target/Specific Item")]
public class SpecificItemTargetResolver : BuffTargetResolver
{
    public ItemData targetItemData;

    public override void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results)
    {
        if (targetItemData == null)
            return;

        results.Add(BuffTargetHandle.Item(targetItemData));
    }
}
