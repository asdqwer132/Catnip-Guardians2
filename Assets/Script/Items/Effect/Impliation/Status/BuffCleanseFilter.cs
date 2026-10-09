using System;
using UnityEngine;

public enum BuffCleanseMode { Harmful, Flags, HarmfulOrFlags, HarmfulAndFlags }
public enum BuffFlagMatchMode { Any, All }

[Serializable]
public sealed class BuffCleanseFilter
{
    [Tooltip("Harmful: 해로운 버프 / Flags: 지정 플래그 / HarmfulOrFlags: 둘 중 하나 / HarmfulAndFlags: 둘 다")]
    public BuffCleanseMode mode;
    [Tooltip("Any: 지정 플래그 중 하나라도 일치 / All: 지정한 모든 플래그가 일치")]
    public BuffFlagMatchMode flagMatch;
    [Tooltip("BuffEffect와 같은 플래그 에셋을 연결합니다. 빈 항목은 무시하며, 유효한 플래그가 없으면 플래그 조건은 불일치입니다.")]
    public BuffFlagDefinition[] flags;

    public bool Matches(ActiveBuff buff)
    {
        if (buff == null || !buff.dispellable) return false;
        switch (mode)
        {
            case BuffCleanseMode.Harmful: return buff.harmful;
            case BuffCleanseMode.Flags: return MatchesFlags(buff);
            case BuffCleanseMode.HarmfulOrFlags: return buff.harmful || MatchesFlags(buff);
            case BuffCleanseMode.HarmfulAndFlags: return buff.harmful && MatchesFlags(buff);
            default: return false;
        }
    }

    private bool MatchesFlags(ActiveBuff buff)
    {
        if (flags == null) return false;
        bool hasFlag = false;
        foreach (BuffFlagDefinition flag in flags)
        {
            if (flag == null) continue;
            hasFlag = true;
            bool matches = buff.HasFlag(flag);
            if (flagMatch == BuffFlagMatchMode.Any && matches) return true;
            if (flagMatch == BuffFlagMatchMode.All && !matches) return false;
        }
        return flagMatch == BuffFlagMatchMode.All && hasFlag;
    }
}
