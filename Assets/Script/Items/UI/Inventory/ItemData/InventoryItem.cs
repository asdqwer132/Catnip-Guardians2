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

    public void ConsumeNextItemUseBuffs() { runtimeBuffList.ConsumeNextItemUseBuffs(); }

    public int GetRuntimeBuffDisplayNumber(
        Object source,
        RuntimeBuffDurationType durationType,
        int currentCycleId
    )
    {
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
        runtimeBuffList.AddRuntimeBuff(
            bonus,
            durationType,
            duration,
            currentCycleId,
            source,
            useCount
        );
    }

    public EffectStat GetFinalEffectStat(
        EffectStat ownerStat = null,
        int currentCycleId = 0
    )
    {
        return InventoryItemStatCalculator.GetFinalEffectStat(
            this,
            ownerStat,
            currentCycleId
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
        runtimeBuffList.AddOrReplaceRuntimeBuff(
            source,
            bonus,
            durationType,
            duration,
            currentCycleId,
            useCount
        );
    }
}