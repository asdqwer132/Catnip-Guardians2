public static class InventoryItemStatCalculator
{
    public static EffectStat GetFinalEffectStat(
        InventoryItem item,
        EffectStat baseEffectStat,
        EffectStat ownerStat = null,
        int currentCycleId = 0
    )
    {
        EffectStat finalStat = new EffectStat();

        if (baseEffectStat != null)
            finalStat.Add(baseEffectStat);

        if (item == null)
        {
            if (ownerStat != null)
                finalStat.Add(ownerStat);

            return finalStat;
        }

        if (item.runtimeBuffList != null)
            item.runtimeBuffList.RemoveExpiredBuffs(currentCycleId);

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