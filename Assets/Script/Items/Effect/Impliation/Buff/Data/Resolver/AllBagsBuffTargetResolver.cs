using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AllBagsBuffTarget", menuName = "GameData/Buffs/Targets/All Bags")]
public sealed class AllBagsBuffTargetResolver : BuffTargetResolver
{
    public override void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results)
    {
        if (results != null)
            results.Add(BuffTargetHandle.AllBags());
    }
}
