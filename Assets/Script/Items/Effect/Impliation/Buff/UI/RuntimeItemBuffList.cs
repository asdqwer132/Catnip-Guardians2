using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RuntimeItemBuffList
{
    public List<RuntimeItemBuff> runtimeBuffs = new List<RuntimeItemBuff>();

    public void AddRuntimeBuff(
        EffectStat stat,
        RuntimeBuffDurationType durationType,
        float duration,
        int currentCycleId,
        Object source,
        int useCount
    )
    {
        if (stat == null)
            return;

        RuntimeItemBuff buff = new RuntimeItemBuff();

        buff.source = source;
        buff.stat = stat.Clone();
        buff.durationType = durationType;
        buff.cycleId = currentCycleId;

        if (durationType == RuntimeBuffDurationType.Seconds)
        {
            buff.endTime = Time.time + Mathf.Max(0.01f, duration);
        }
        else
        {
            buff.endTime = 0f;
        }

        if (durationType == RuntimeBuffDurationType.NextItemUse)
        {
            buff.remainingUseCount = Mathf.Max(1, useCount);
        }
        else
        {
            buff.remainingUseCount = 0;
        }

        runtimeBuffs.Add(buff);
        RemoveExpiredBuffs(currentCycleId);
    }

    public void AddOrReplaceRuntimeBuff(
        Object source,
        EffectStat stat,
        RuntimeBuffDurationType durationType,
        float duration,
        int currentCycleId,
        int useCount
    )
    {
        RemoveBuffsBySource(source);
        AddRuntimeBuff(
            stat,
            durationType,
            duration,
            currentCycleId,
            source,
            useCount
        );
    }

    public void ConsumeNextItemUseBuffs()
    {
        if (runtimeBuffs == null)
            return;

        for (int i = runtimeBuffs.Count - 1; i >= 0; i--)
        {
            RuntimeItemBuff buff = runtimeBuffs[i];

            if (buff == null)
            {
                runtimeBuffs.RemoveAt(i);
                continue;
            }

            if (buff.durationType != RuntimeBuffDurationType.NextItemUse)
                continue;

            buff.remainingUseCount--;

            if (buff.remainingUseCount <= 0)
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

            if (buff == null)
            {
                runtimeBuffs.RemoveAt(i);
                continue;
            }

            if (buff.IsExpired(currentCycleId))
                runtimeBuffs.RemoveAt(i);
        }
    }

    public void RemoveBuffsBySource(Object source)
    {
        if (runtimeBuffs == null)
            return;

        for (int i = runtimeBuffs.Count - 1; i >= 0; i--)
        {
            RuntimeItemBuff buff = runtimeBuffs[i];

            if (buff == null)
            {
                runtimeBuffs.RemoveAt(i);
                continue;
            }

            if (buff.source == source)
                runtimeBuffs.RemoveAt(i);
        }
    }

    public int GetRuntimeBuffDisplayNumber(
        Object source,
        RuntimeBuffDurationType durationType,
        int currentCycleId
    )
    {
        RemoveExpiredBuffs(currentCycleId);

        if (runtimeBuffs == null)
            return 0;

        int result = 0;

        for (int i = 0; i < runtimeBuffs.Count; i++)
        {
            RuntimeItemBuff buff = runtimeBuffs[i];

            if (buff == null)
                continue;

            if (buff.source != source)
                continue;

            if (buff.durationType != durationType)
                continue;

            result += buff.GetUIDisplayNumber();
        }

        return result;
    }

    public bool HasActiveBuff(
        Object source,
        RuntimeBuffDurationType durationType,
        int currentCycleId
    )
    {
        RemoveExpiredBuffs(currentCycleId);

        if (runtimeBuffs == null)
            return false;

        for (int i = 0; i < runtimeBuffs.Count; i++)
        {
            RuntimeItemBuff buff = runtimeBuffs[i];

            if (buff == null)
                continue;

            if (buff.source != source)
                continue;

            if (buff.durationType != durationType)
                continue;

            return true;
        }

        return false;
    }

    public bool HasAnyActiveBuff(int currentCycleId)
    {
        RemoveExpiredBuffs(currentCycleId);

        return runtimeBuffs != null && runtimeBuffs.Count > 0;
    }
}