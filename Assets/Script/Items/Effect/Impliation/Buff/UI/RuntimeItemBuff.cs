using System;
using UnityEngine;

public enum RuntimeBuffDurationType
{
    CurrentCycle,
    Seconds,
    NextItemUse
}

[Serializable]
public class RuntimeItemBuff
{
    [Header("Source")]
    public UnityEngine.Object source;

    [Header("Stat")]
    public EffectStat stat = new EffectStat();

    [Header("Duration")]
    public RuntimeBuffDurationType durationType;

    public float endTime;
    public int cycleId;

    [Header("Use Count")]
    public int remainingUseCount = 1;

    public bool IsExpired(int currentCycleId)
    {
        switch (durationType)
        {
            case RuntimeBuffDurationType.CurrentCycle:
                return currentCycleId != cycleId;

            case RuntimeBuffDurationType.Seconds:
                return Time.time >= endTime;

            case RuntimeBuffDurationType.NextItemUse:
                return remainingUseCount <= 0;
        }

        return true;
    }

    public float GetRemainingTime()
    {
        if (durationType != RuntimeBuffDurationType.Seconds)
            return 0f;

        return Mathf.Max(0f, endTime - Time.time);
    }

    public int GetUIDisplayNumber()
    {
        switch (durationType)
        {
            case RuntimeBuffDurationType.CurrentCycle:
                return 1;

            case RuntimeBuffDurationType.Seconds:
                return Mathf.CeilToInt(GetRemainingTime());

            case RuntimeBuffDurationType.NextItemUse:
                return Mathf.Max(0, remainingUseCount);
        }

        return 0;
    }
}