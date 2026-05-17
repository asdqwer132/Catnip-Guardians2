using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public ItemData itemData;
    public int amount = 1;

    [Header("Runtime Buffs")]
    public List<RuntimeItemBuff> runtimeBuffs = new List<RuntimeItemBuff>();

    public InventoryItem(ItemData itemData, int amount)
    {
        this.itemData = itemData;
        this.amount = amount;
    }
    public bool HasRuntimeBuffFromSource(
    Object source,
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

            if (buff.source == source)
                return true;
        }

        return false;
    }

    public int GetRemainingUseCountFromSource(
        Object source,
        int currentCycleId
    )
    {
        RemoveExpiredBuffs(currentCycleId);

        if (runtimeBuffs == null)
            return 0;

        int count = 0;

        for (int i = 0; i < runtimeBuffs.Count; i++)
        {
            RuntimeItemBuff buff = runtimeBuffs[i];

            if (buff == null)
                continue;

            if (buff.source != source)
                continue;

            if (buff.durationType == RuntimeBuffDurationType.NextItemUse)
                count += Mathf.Max(0, buff.remainingUseCount);
        }

        return count;
    }

    public float GetRemainingTimeFromSource(
        Object source,
        int currentCycleId
    )
    {
        RemoveExpiredBuffs(currentCycleId);

        if (runtimeBuffs == null)
            return 0f;

        float maxTime = 0f;

        for (int i = 0; i < runtimeBuffs.Count; i++)
        {
            RuntimeItemBuff buff = runtimeBuffs[i];

            if (buff == null)
                continue;

            if (buff.source != source)
                continue;

            if (buff.durationType == RuntimeBuffDurationType.Seconds)
            {
                maxTime = Mathf.Max(
                    maxTime,
                    buff.GetRemainingTime()
                );
            }
        }

        return maxTime;
    }
    public void AddRuntimeBuff(
        EffectStat bonus,
        RuntimeBuffDurationType durationType,
        float duration,
        int currentCycleId
    )
    {
        AddRuntimeBuff(
            bonus,
            durationType,
            duration,
            currentCycleId,
            null,
            1
        );
    }

    public void AddRuntimeBuff(
        EffectStat bonus,
        RuntimeBuffDurationType durationType,
        float duration,
        int currentCycleId,
        Object source
    )
    {
        AddRuntimeBuff(
            bonus,
            durationType,
            duration,
            currentCycleId,
            source,
            1
        );
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

        if (runtimeBuffs == null)
            runtimeBuffs = new List<RuntimeItemBuff>();

        RuntimeItemBuff buff = new RuntimeItemBuff();

        buff.source = source;
        buff.stat = CloneStat(bonus);
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
        int currentCycleId
    )
    {
        AddOrReplaceRuntimeBuff(
            source,
            bonus,
            durationType,
            duration,
            currentCycleId,
            1
        );
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

    public void ClearRuntimeBuffs()
    {
        if (runtimeBuffs == null)
            runtimeBuffs = new List<RuntimeItemBuff>();

        runtimeBuffs.Clear();
    }

    public EffectStat GetFinalEffectStat(
        EffectStat ownerStat = null,
        int currentCycleId = 0
    )
    {
        RemoveExpiredBuffs(currentCycleId);

        EffectStat finalStat = new EffectStat();

        if (itemData != null && itemData.effectStat != null)
            finalStat.Add(itemData.effectStat);

        if (runtimeBuffs != null)
        {
            for (int i = 0; i < runtimeBuffs.Count; i++)
            {
                RuntimeItemBuff buff = runtimeBuffs[i];

                if (buff == null || buff.stat == null)
                    continue;

                finalStat.Add(buff.stat);
            }
        }

        if (ownerStat != null)
            finalStat.Add(ownerStat);

        return finalStat;
    }

    public bool HasRuntimeBuff(int currentCycleId)
    {
        RemoveExpiredBuffs(currentCycleId);

        return runtimeBuffs != null && runtimeBuffs.Count > 0;
    }

    public int GetTotalRemainingUseBuffCount(int currentCycleId)
    {
        RemoveExpiredBuffs(currentCycleId);

        if (runtimeBuffs == null)
            return 0;

        int count = 0;

        for (int i = 0; i < runtimeBuffs.Count; i++)
        {
            RuntimeItemBuff buff = runtimeBuffs[i];

            if (buff == null)
                continue;

            if (buff.durationType == RuntimeBuffDurationType.NextItemUse)
                count += Mathf.Max(0, buff.remainingUseCount);
        }

        return count;
    }

    public float GetLongestRemainingBuffTime(int currentCycleId)
    {
        RemoveExpiredBuffs(currentCycleId);

        if (runtimeBuffs == null)
            return 0f;

        float maxTime = 0f;

        for (int i = 0; i < runtimeBuffs.Count; i++)
        {
            RuntimeItemBuff buff = runtimeBuffs[i];

            if (buff == null)
                continue;

            if (buff.durationType == RuntimeBuffDurationType.Seconds)
            {
                maxTime = Mathf.Max(
                    maxTime,
                    buff.GetRemainingTime()
                );
            }
        }

        return maxTime;
    }

    public RuntimeBuffDurationType GetPrimaryBuffDurationType(int currentCycleId)
    {
        RemoveExpiredBuffs(currentCycleId);

        if (runtimeBuffs == null || runtimeBuffs.Count == 0)
            return RuntimeBuffDurationType.NextItemUse;

        return runtimeBuffs[0].durationType;
    }

    private EffectStat CloneStat(EffectStat source)
    {
        EffectStat result = new EffectStat();

        if (source == null)
            return result;

        result.attackPower = source.attackPower;
        result.healPower = source.healPower;
        result.debuffPower = source.debuffPower;

        result.effectRadius = source.effectRadius;
        result.effectCount = source.effectCount;

        result.defensePower = source.defensePower;
        result.speedPower = source.speedPower;

        result.attackMultiplier = source.attackMultiplier;
        result.healMultiplier = source.healMultiplier;
        result.debuffMultiplier = source.debuffMultiplier;
        result.defenseMultiplier = source.defenseMultiplier;
        result.speedMultiplier = source.speedMultiplier;

        return result;
    }
}