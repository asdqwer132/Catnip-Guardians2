using System;
using UnityEngine;

[Serializable]
public class BuffInfo : IGameStat<BuffInfo>
{
    [Header("Stack")]
    public BuffStackMode stackMode = BuffStackMode.Refresh;
    [Min(1)] public int maxStack = 1;

    [Header("Timing")]
    public BuffApplyTiming applyTiming = BuffApplyTiming.Snapshot;

    [Header("Limit")]
    public BuffUseLimitType useLimitType = BuffUseLimitType.Time;
    [Min(0.01f)] public float duration = 1f;
    [Min(1)] public int maxUseCount = 1;

    public BuffInfo Clone()
    {
        return new BuffInfo
        {
            stackMode = stackMode,
            maxStack = maxStack,
            applyTiming = applyTiming,
            useLimitType = useLimitType,
            duration = duration,
            maxUseCount = maxUseCount
        };
    }

    public void Clamp()
    {
        duration = Mathf.Max(0.01f, duration);
        maxStack = Mathf.Max(1, maxStack);
        maxUseCount = Mathf.Max(1, maxUseCount);

        if (stackMode == BuffStackMode.Refresh)
            maxStack = 1;
    }
}
