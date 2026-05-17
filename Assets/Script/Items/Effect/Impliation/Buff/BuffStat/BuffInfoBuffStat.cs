using System;
using UnityEngine;

[Serializable]
public class BuffInfoBuffStat
{
    [Header("Buff Info Bonus")]
    public float bonusDuration = 0f;
    public int bonusNextItemUseTargetCount = 0;

    [Tooltip("켜면 최종 버프를 중첩 가능으로 강제")]
    public bool forceAllowStack = false;

    public void Add(BuffInfoBuffStat other)
    {
        if (other == null)
            return;

        bonusDuration += other.bonusDuration;
        bonusNextItemUseTargetCount += other.bonusNextItemUseTargetCount;

        if (other.forceAllowStack)
            forceAllowStack = true;
    }

    public void Reset()
    {
        bonusDuration = 0f;
        bonusNextItemUseTargetCount = 0;
        forceAllowStack = false;
    }

    public BuffInfoBuffStat Clone()
    {
        BuffInfoBuffStat clone = new BuffInfoBuffStat();

        clone.bonusDuration = bonusDuration;
        clone.bonusNextItemUseTargetCount = bonusNextItemUseTargetCount;
        clone.forceAllowStack = forceAllowStack;

        return clone;
    }

    public void ApplyTo(BuffInfoStat buffInfo)
    {
        if (buffInfo == null)
            return;

        buffInfo.bonusDuration += bonusDuration;
        buffInfo.bonusNextItemUseTargetCount += bonusNextItemUseTargetCount;

        if (forceAllowStack)
            buffInfo.forceAllowStack = true;
    }
}