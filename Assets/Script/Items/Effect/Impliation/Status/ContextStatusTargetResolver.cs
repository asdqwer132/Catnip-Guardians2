using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ContextStatusTargetResolver", menuName = "GameData/Buffs/Targets/Context Status")]
public sealed class ContextStatusTargetResolver : BuffTargetResolver
{
    public StatusQueryTarget[] targets = { StatusQueryTarget.PlayerStatus };

    public override void ResolveTargets(BuffRegisterContext context, List<BuffTargetHandle> results)
    {
        if (context == null || results == null || targets == null) return;
        ItemEffectContext execution = context.itemContext;
        for (int i = 0; i < targets.Length; i++)
        {
            BuffTargetHandle handle = StatusTargetUtility.Handle(StatusTargetUtility.Resolve(targets[i], execution));
            if (handle == null) continue;
            bool duplicate = false;
            for (int j = 0; j < results.Count; j++) if (results[j].SameTarget(handle)) { duplicate = true; break; }
            if (!duplicate) results.Add(handle);
        }
    }
}
