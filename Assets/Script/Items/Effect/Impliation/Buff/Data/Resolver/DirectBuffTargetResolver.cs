using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DirectBuffTargetResolver", menuName = "Game/Buff/Buff Target/Direct Buff Target")]
public class DirectBuffTargetResolver : BuffTargetResolver
{
    public Component targetComponent;

    public override void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results)
    {
        if (results == null || targetComponent == null)
            return;

        IBuffTarget target = targetComponent as IBuffTarget;

        if (target != null)
            results.Add(BuffTargetHandle.Target(target));
    }
}
