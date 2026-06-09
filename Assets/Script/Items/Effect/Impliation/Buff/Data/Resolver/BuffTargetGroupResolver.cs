using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuffTargetGroupResolver", menuName = "Game/Buff/Buff Target/Group")]
public class BuffTargetGroupResolver : BuffTargetResolver
{
    public string targetGroup;

    public override void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results)
    {
        if (results == null || string.IsNullOrEmpty(targetGroup))
            return;

        results.Add(BuffTargetHandle.Group(targetGroup));
    }
}
