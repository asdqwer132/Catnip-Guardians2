using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RuntimeItemBuffList
{
    public List<RuntimeItemBuff> runtimeBuffs = new List<RuntimeItemBuff>();

    public int GetRuntimeBuffDisplayNumber(
        Object source,
        RuntimeBuffDurationType durationType,
        int currentCycleId
    )
    {
        if (runtimeBuffs == null || runtimeBuffs.Count == 0)
            return 0;

        RemoveExpiredBuffs(currentCycleId);

        int totalUseCount = 0;
        float maxRemainingTime = 0f;

        for (int i = runtimeBuffs.Count - 1; i >= 0; i--)
        {
            RuntimeItemBuff buff = runtimeBuffs[i];

            if (buff == null)
                continue;

            if (buff.source != source)
                continue;

            if (buff.durationType != durationType)
                continue;

            if (buff.IsExpired(currentCycleId))
                continue;

            if (durationType == RuntimeBuffDurationType.NextItemUse)
            {
                totalUseCount += buff.remainingUseCount;
            }
            else if (durationType == RuntimeBuffDurationType.Seconds)
            {
                maxRemainingTime = Mathf.Max(
                    maxRemainingTime,
                    buff.GetRemainingTime()
                );
            }
        }

        if (durationType == RuntimeBuffDurationType.Seconds)
            return Mathf.CeilToInt(maxRemainingTime);

        if (durationType == RuntimeBuffDurationType.NextItemUse)
            return totalUseCount;

        return 0;
    }

    public void AddRuntimeBuff(
        EffectStat bonus,
        RuntimeBuffDurationType durationType,
        float duration,
        int currentCycleId,
        Object source,
        int useCount
    )
    {
        if (bonus == null)
            return;

        EnsureList();

        RuntimeItemBuff buff = new RuntimeItemBuff();

        buff.source = source;
        buff.stat = EffectStatUtility.CloneStat(bonus);
        buff.durationType = durationType;
        buff.cycleId = currentCycleId;

        if (durationType == RuntimeBuffDurationType.Seconds)
            buff.endTime = Time.time + duration;
        else
            buff.endTime = 0f;

        if (durationType == RuntimeBuffDurationType.NextItemUse)
            buff.remainingUseCount = Mathf.Max(1, useCount);
        else
            buff.remainingUseCount = 0;

        runtimeBuffs.Add(buff);
    }

    public void AddOrReplaceRuntimeBuff(
        Object source,
        EffectStat bonus,
        RuntimeBuffDurationType durationType,
        float duration,
        int currentCycleId,
        int useCount
    )
    {
        if (bonus == null)
            return;

        if (source != null)
            RemoveRuntimeBuffFromSource(source);

        AddRuntimeBuff(
            bonus,
            durationType,
            duration,
            currentCycleId,
            source,
            useCount
        );
    }

    public void RemoveRuntimeBuffFromSource(Object source)
    {
        if (runtimeBuffs == null)
            return;

        if (source == null)
            return;

        for (int i = runtimeBuffs.Count - 1; i >= 0; i--)
        {
            RuntimeItemBuff buff = runtimeBuffs[i];

            if (buff == null)
                continue;

            if (buff.source == source)
                runtimeBuffs.RemoveAt(i);
        }
    }

    public void RemoveExpiredBuffs(int currentCycleId)
    {
        if (runtimeBuffs == null)
            return;

        for (int i = runtimeBuffs.Count - 1; i >= 0; i--)
        {
            RuntimeItemBuff buff = runtimeBuffs[i];

            if (buff == null || buff.IsExpired(currentCycleId))
            {
                runtimeBuffs.RemoveAt(i);
            }
        }
    }

    public void ConsumeNextItemUseBuffs()
    {
        if (runtimeBuffs == null)
            return;

        for (int i = runtimeBuffs.Count - 1; i >= 0; i--)
        {
            RuntimeItemBuff buff = runtimeBuffs[i];

            if (buff == null)
                continue;

            if (buff.durationType != RuntimeBuffDurationType.NextItemUse)
                continue;

            buff.remainingUseCount--;

            if (buff.remainingUseCount <= 0)
                runtimeBuffs.RemoveAt(i);
        }
    }

    private void EnsureList()
    {
        if (runtimeBuffs == null)
            runtimeBuffs = new List<RuntimeItemBuff>();
    }
}