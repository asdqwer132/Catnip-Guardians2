using System;
using UnityEngine;

public enum BuffStackMode
{
    Refresh,
    Stack
}

[Serializable]
public class BuffInfo : IGameStat<BuffInfo>
{
    [Header("Time")]
    [Min(0.01f)]
    public float duration = 1f;

    [Header("Stack")]
    public BuffStackMode stackMode = BuffStackMode.Refresh;

    [Min(1)]
    public int maxStack = 1;

    [Header("Apply Timing")]
    public BuffApplyTiming applyTiming = BuffApplyTiming.Snapshot;

    [Header("Use Limit")]
    public BuffUseLimitType useLimitType = BuffUseLimitType.Time;

    [Min(1)]
    public int maxUseCount = 1;

    public BuffInfo Clone()
    {
        return new BuffInfo
        {
            duration = duration,

            stackMode = stackMode,
            maxStack = maxStack,

            applyTiming = applyTiming,
            useLimitType = useLimitType,
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

[Serializable]
public class BuffInfoBuffStat : IBuffStat<BuffInfo>
{
    [Header("Duration")]
    public float duration = 0f;
    public float durationM = 0f;

    public void ApplyTo(BuffInfo target)
    {
        if (target == null)
            return;

        target.duration += duration;
        target.duration *= 1f + durationM;

        target.Clamp();
    }

    public string GetSummaryText()
    {
        string result = "";

        if (duration != 0f)
            result += "버프시간 +" + duration + " ";

        if (durationM != 0f)
            result += "버프시간 x" + (1f + durationM).ToString("0.##") + " ";

        return result.Trim();
    }
}