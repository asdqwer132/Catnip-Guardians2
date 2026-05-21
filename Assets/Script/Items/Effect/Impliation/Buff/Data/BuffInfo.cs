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

    public BuffInfo Clone()
    {
        return new BuffInfo
        {
            duration = duration,
            stackMode = stackMode,
            maxStack = maxStack
        };
    }

    public void Clamp()
    {
        if (duration < 0.01f)
            duration = 0.01f;

        if (maxStack < 1)
            maxStack = 1;

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