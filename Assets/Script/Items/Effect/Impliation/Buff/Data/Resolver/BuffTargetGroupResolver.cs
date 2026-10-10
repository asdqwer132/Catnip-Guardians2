using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuffTargetGroupResolver", menuName = "04.Items/Effects/Direct Effects/Buff/BuffSetting/Targets/Group")]
public class BuffTargetGroupResolver : BuffTargetResolver
{
    [BuffTargetGroupName]
    public string targetGroup;

    public override void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results)
    {
        if (results == null || string.IsNullOrEmpty(targetGroup))
            return;

        BuffTargetHandle handle = BuffTargetHandle.Group(targetGroup);
        if (handle != null) results.Add(handle);
    }
}
