using System.Collections.Generic;
using UnityEngine;

public class BuffStatCalculator
{
    private BuffStorage storage;

    public BuffStatCalculator(BuffStorage storage)
    {
        this.storage = storage;
    }

    public AttackStat GetBuffedAttackStat(
        AttackStat baseStat,
        ItemData targetItemData,
        EquipmentBag targetBag
    )
    {
        return GetBuffedAttackStat(
            baseStat,
            targetItemData,
            targetBag,
            BuffCalculationMode.All,
            false
        );
    }

    public AttackStat GetBuffedAttackStat(
        AttackStat baseStat,
        ItemData targetItemData,
        EquipmentBag targetBag,
        BuffCalculationMode calculationMode,
        bool consumeUseCount
    )
    {
        return GetBuffedItemStat(
            baseStat,
            targetItemData,
            targetBag,
            ApplyBuffToAttackStat,
            calculationMode,
            consumeUseCount
        );
    }

    public BuffInfo GetBuffedBuffInfo(
        BuffInfo baseInfo,
        ItemData targetItemData,
        EquipmentBag targetBag
    )
    {
        return GetBuffedItemStat(
            baseInfo,
            targetItemData,
            targetBag,
            ApplyBuffToBuffInfo,
            BuffCalculationMode.All,
            false
        );
    }

    public EnemyStat GetBuffedEnemyStat(
        EnemyStat baseStat,
        Enemy enemy
    )
    {
        if (baseStat == null)
            return null;

        EnemyStat result = baseStat.Clone();

        ApplyEnemyBuffs(result, storage.globalBuffs);
        ApplyEnemyBuffs(result, storage.futureEnemyBuffs);

        if (enemy != null && storage.enemyBuffs.ContainsKey(enemy))
        {
            ApplyEnemyBuffs(
                result,
                storage.enemyBuffs[enemy]
            );
        }

        result.Clamp();

        return result;
    }

    private T GetBuffedItemStat<T>(
        T baseStat,
        ItemData targetItemData,
        EquipmentBag targetBag,
        System.Action<BuffStat, T> applyAction,
        BuffCalculationMode calculationMode,
        bool consumeUseCount
    )
        where T : class, IGameStat<T>
    {
        if (baseStat == null)
            return null;

        T result = baseStat.Clone();

        ApplyItemBuffs(
            result,
            storage.globalBuffs,
            targetItemData,
            targetBag,
            applyAction,
            calculationMode,
            consumeUseCount
        );

        if (targetBag != null && storage.bagBuffs.ContainsKey(targetBag))
        {
            ApplyItemBuffs(
                result,
                storage.bagBuffs[targetBag],
                targetItemData,
                targetBag,
                applyAction,
                calculationMode,
                consumeUseCount
            );
        }

        if (targetItemData != null && storage.itemBuffs.ContainsKey(targetItemData))
        {
            ApplyItemBuffs(
                result,
                storage.itemBuffs[targetItemData],
                targetItemData,
                targetBag,
                applyAction,
                calculationMode,
                consumeUseCount
            );
        }

        if (targetItemData != null &&
            targetItemData.series != ItemSeries.None &&
            storage.itemSeriesBuffs.ContainsKey(targetItemData.series))
        {
            ApplyItemBuffs(
                result,
                storage.itemSeriesBuffs[targetItemData.series],
                targetItemData,
                targetBag,
                applyAction,
                calculationMode,
                consumeUseCount
            );
        }

        result.Clamp();

        return result;
    }

    private void ApplyItemBuffs<T>(
        T target,
        List<ActiveBuff> buffs,
        ItemData targetItemData,
        EquipmentBag targetBag,
        System.Action<BuffStat, T> applyAction,
        BuffCalculationMode calculationMode,
        bool consumeUseCount
    )
    {
        if (target == null)
            return;

        if (buffs == null)
            return;

        if (applyAction == null)
            return;

        List<ActiveBuff> consumedBuffs = null;

        for (int i = 0; i < buffs.Count; i++)
        {
            ActiveBuff activeBuff = buffs[i];

            if (!CanUseBuff(activeBuff))
                continue;

            if (!CanUseBuffByCalculationMode(
                    activeBuff,
                    calculationMode
                ))
            {
                continue;
            }

            if (!CanApplyBuffToItemTarget(
                    activeBuff,
                    targetItemData,
                    targetBag
                ))
            {
                continue;
            }

            int stackCount = Mathf.Max(1, activeBuff.stack);

            for (int stackIndex = 0; stackIndex < stackCount; stackIndex++)
                applyAction(activeBuff.buffStat, target);

            if (consumeUseCount &&
                activeBuff.useLimitType == BuffUseLimitType.UseCount)
            {
                if (consumedBuffs == null)
                    consumedBuffs = new List<ActiveBuff>();

                consumedBuffs.Add(activeBuff);
            }
        }

        if (consumedBuffs == null)
            return;

        for (int i = 0; i < consumedBuffs.Count; i++)
            consumedBuffs[i].ConsumeUse();
    }

    private void ApplyEnemyBuffs(
        EnemyStat target,
        List<ActiveBuff> buffs
    )
    {
        if (target == null)
            return;

        if (buffs == null)
            return;

        for (int i = 0; i < buffs.Count; i++)
        {
            ActiveBuff activeBuff = buffs[i];

            if (!CanUseBuff(activeBuff))
                continue;

            int stackCount = Mathf.Max(1, activeBuff.stack);

            for (int stackIndex = 0; stackIndex < stackCount; stackIndex++)
                activeBuff.buffStat.ApplyToEnemyStat(target);
        }
    }

    private bool CanUseBuff(ActiveBuff activeBuff)
    {
        if (activeBuff == null)
            return false;

        if (activeBuff.IsExpired)
            return false;

        if (activeBuff.buffStat == null)
            return false;

        return true;
    }

    private bool CanUseBuffByCalculationMode(
        ActiveBuff activeBuff,
        BuffCalculationMode calculationMode
    )
    {
        if (activeBuff == null)
            return false;

        if (calculationMode == BuffCalculationMode.All)
            return true;

        if (calculationMode == BuffCalculationMode.SnapshotOnly)
            return activeBuff.applyTiming == BuffApplyTiming.Snapshot;

        if (calculationMode == BuffCalculationMode.DynamicOnly)
            return activeBuff.applyTiming == BuffApplyTiming.Dynamic;

        return true;
    }

    private bool CanApplyBuffToItemTarget(
        ActiveBuff activeBuff,
        ItemData targetItemData,
        EquipmentBag targetBag
    )
    {
        if (activeBuff == null)
            return false;

        if (targetItemData == null)
            return false;

        if (activeBuff.targetScope == BuffTarget.Item &&
            activeBuff.targetItemData != null &&
            activeBuff.targetItemData != targetItemData)
        {
            return false;
        }

        if (activeBuff.targetScope == BuffTarget.ItemSeries)
        {
            if (activeBuff.targetSeries == ItemSeries.None)
                return false;

            if (targetItemData.series != activeBuff.targetSeries)
                return false;
        }

        bool isSelf =
            activeBuff.sourceItemData != null &&
            activeBuff.sourceItemData == targetItemData;

        if (isSelf && !activeBuff.includeSelf)
            return false;

        return true;
    }

    private void ApplyBuffToAttackStat(
        BuffStat buffStat,
        AttackStat target
    )
    {
        if (buffStat == null)
            return;

        buffStat.ApplyToAttackStat(target);
    }

    private void ApplyBuffToBuffInfo(
        BuffStat buffStat,
        BuffInfo target
    )
    {
        if (buffStat == null)
            return;

        buffStat.ApplyToBuffInfo(target);
    }
}