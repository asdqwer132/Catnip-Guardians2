public static class InventoryItemStatCalculator
{
    public static EffectStat GetFinalEffectStat(
        InventoryItem item,
        EffectStat ownerStat = null,
        int currentCycleId = 0
    )
    {
        EffectStat finalStat = new EffectStat();

        if (item == null)
            return finalStat;

        if (item.runtimeBuffList != null)
            item.runtimeBuffList.RemoveExpiredBuffs(currentCycleId);

        if (item.itemData != null && item.itemData.effectStat != null)
            finalStat.Add(item.itemData.effectStat);

        if (
            item.runtimeBuffList != null &&
            item.runtimeBuffList.runtimeBuffs != null
        )
        {
            for (int i = 0; i < item.runtimeBuffList.runtimeBuffs.Count; i++)
            {
                RuntimeItemBuff buff = item.runtimeBuffList.runtimeBuffs[i];

                if (buff == null || buff.stat == null)
                    continue;

                finalStat.Add(buff.stat);
            }
        }

        if (ownerStat != null)
            finalStat.Add(ownerStat);

        return finalStat;
    }
}