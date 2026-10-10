using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AllItemsBuffTarget", menuName = "04.Items/Effects/Direct Effects/Buff/BuffSetting/Targets/All Items")]
public sealed class AllItemsBuffTargetResolver : BuffTargetResolver
{
    public override void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results)
    {
        if (results != null)
            results.Add(BuffTargetHandle.AllItems());
    }
}
