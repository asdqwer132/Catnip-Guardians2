using System;
using UnityEngine;

[Serializable]
public class BuffInfoStat
{
    [Header("Duration")]
    public RuntimeBuffDurationType durationType = RuntimeBuffDurationType.NextItemUse;

    [Tooltip("Duration Type이 Seconds일 때만 사용")]
    [Min(0f)]
    public float duration = 5f;

    [Tooltip("Duration Type이 NextItemUse일 때 몇 개의 다음 아이템에 버프를 줄지")]
    [Min(1)]
    public int nextItemUseTargetCount = 1;

    [Header("Stack")]
    public bool allowStack = false;

    [Header("Runtime Bonus")]
    public float bonusDuration = 0f;
    public int bonusNextItemUseTargetCount = 0;
    public bool forceAllowStack = false;

    public void Reset()
    {
        durationType = RuntimeBuffDurationType.NextItemUse;
        duration = 5f;
        nextItemUseTargetCount = 1;
        allowStack = false;

        bonusDuration = 0f;
        bonusNextItemUseTargetCount = 0;
        forceAllowStack = false;
    }

    public BuffInfoStat Clone()
    {
        BuffInfoStat clone = new BuffInfoStat();

        clone.durationType = durationType;
        clone.duration = duration;
        clone.nextItemUseTargetCount = nextItemUseTargetCount;
        clone.allowStack = allowStack;

        clone.bonusDuration = bonusDuration;
        clone.bonusNextItemUseTargetCount = bonusNextItemUseTargetCount;
        clone.forceAllowStack = forceAllowStack;

        return clone;
    }

    public float GetFinalDuration()
    {
        return Mathf.Max(0f, duration + bonusDuration);
    }

    public int GetFinalNextItemUseTargetCount()
    {
        return Mathf.Max(
            1,
            nextItemUseTargetCount + bonusNextItemUseTargetCount
        );
    }

    public bool GetFinalAllowStack()
    {
        if (forceAllowStack)
            return true;

        return allowStack;
    }

    public int GetUIStartNumber()
    {
        if (durationType == RuntimeBuffDurationType.Seconds)
            return Mathf.CeilToInt(GetFinalDuration());

        if (durationType == RuntimeBuffDurationType.NextItemUse)
            return GetFinalNextItemUseTargetCount();

        return 1;
    }
}