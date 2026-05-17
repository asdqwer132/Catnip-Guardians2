using UnityEngine;

public static class ItemCooldownCalculator
{
    public static float GetFinalCooldown(
        InventoryItem item,
        float baseCooldown,
        int currentCycleId = 0
    )
    {
        float result = baseCooldown;

        if (item == null)
            return Mathf.Max(0.01f, result);

        if (item.runtimeBuffList != null)
            item.runtimeBuffList.RemoveExpiredBuffs(currentCycleId);

        if (
            item.runtimeBuffList == null ||
            item.runtimeBuffList.runtimeBuffs == null
        )
        {
            return Mathf.Max(0.01f, result);
        }

        for (int i = 0; i < item.runtimeBuffList.runtimeBuffs.Count; i++)
        {
            RuntimeItemBuff buff = item.runtimeBuffList.runtimeBuffs[i];

            if (buff == null)
                continue;

            BuffStat buffStat = buff.stat as BuffStat;

            if (buffStat == null)
                continue;

            result = buffStat.ApplyCooldownBuff(result);
        }

        return Mathf.Max(0.01f, result);
    }
}