using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ContextSpecialStatusTargetResolver", menuName = "04.Items/Effects/Direct Effects/Buff/BuffSetting/Targets/Execution Special Status Target")]
public sealed class ContextSpecialStatusTargetResolver : BuffTargetResolver
{
    public SpecialStatusQueryTarget[] targets = { SpecialStatusQueryTarget.PlayerStatus };

    public override void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results)
    {
        if (context == null || results == null || targets == null) return;
        ItemEffectContext execution = context.itemContext;
        for (int i = 0; i < targets.Length; i++)
        {
            BuffTargetHandle handle = SpecialStatusTargetUtility.Handle(SpecialStatusTargetUtility.Resolve(targets[i], execution));
            if (handle == null) continue;
            bool duplicate = false;
            for (int j = 0; j < results.Count; j++) if (results[j].SameTarget(handle)) { duplicate = true; break; }
            if (!duplicate) results.Add(handle);
        }
    }
}
