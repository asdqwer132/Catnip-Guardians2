using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public ItemData itemData;
    public int amount = 1;

    [Header("Runtime Buffs")]
    public RuntimeItemBuffList runtimeBuffList = new RuntimeItemBuffList();

    public InventoryItem(ItemData itemData, int amount)
    {
        this.itemData = itemData;
        this.amount = amount;
    }

    public void ConsumeNextItemUseBuffs()
    {
        if (runtimeBuffList == null)
            return;

        runtimeBuffList.ConsumeNextItemUseBuffs();
    }

    public int GetRuntimeBuffDisplayNumber(
        Object source,
        RuntimeBuffDurationType durationType,
        int currentCycleId
    )
    {
        if (runtimeBuffList == null)
            return 0;

        return runtimeBuffList.GetRuntimeBuffDisplayNumber(
            source,
            durationType,
            currentCycleId
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
        if (runtimeBuffList == null)
            runtimeBuffList = new RuntimeItemBuffList();

        runtimeBuffList.AddRuntimeBuff(
            bonus,
            durationType,
            duration,
            currentCycleId,
            source,
            useCount
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
        if (runtimeBuffList == null)
            runtimeBuffList = new RuntimeItemBuffList();

        runtimeBuffList.AddOrReplaceRuntimeBuff(
            source,
            bonus,
            durationType,
            duration,
            currentCycleId,
            useCount
        );
    }

    public EffectStat GetFinalEffectStat(
        EffectStat baseEffectStat,
        EffectStat ownerStat = null,
        int currentCycleId = 0
    )
    {
        return InventoryItemStatCalculator.GetFinalEffectStat(
            this,
            baseEffectStat,
            ownerStat,
            currentCycleId
        );
    }
}