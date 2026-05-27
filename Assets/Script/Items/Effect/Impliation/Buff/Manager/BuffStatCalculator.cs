using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BuffStatCalculator
/// 
/// 역할:
/// - 저장된 ActiveBuff들을 읽어서 최종 스탯을 계산한다.
/// - 버프를 등록하거나 삭제하지 않는다.
/// - 대상별 계산 규칙만 담당한다.
/// 
/// 계산 대상:
/// - AttackStat
/// - BuffInfo
/// - EnemyStat
/// - EnemySpawnerStat
/// 
/// 성능 주의:
/// - 스탯 계산은 자주 호출될 수 있으므로 Debug.Log를 넣지 않는다.
/// - ContainsKey 후 인덱서 접근 대신 TryGetValue를 사용한다.
/// </summary>
public class BuffStatCalculator
{
    private BuffStorage storage;

    public BuffStatCalculator(BuffStorage storage)
    {
        this.storage = storage;
    }

    public AttackStat GetBuffedAttackStat(AttackStat baseStat, ItemData targetItemData, EquipmentBag targetBag)
    {
        return GetBuffedAttackStat(baseStat, targetItemData, targetBag, BuffCalculationMode.All, false);
    }

    public AttackStat GetBuffedAttackStat(AttackStat baseStat, ItemData targetItemData, EquipmentBag targetBag, BuffCalculationMode calculationMode, bool consumeUseCount)
    {
        return GetBuffedItemStat(baseStat, targetItemData, targetBag, ApplyBuffToAttackStat, calculationMode, consumeUseCount);
    }

    public BuffInfo GetBuffedBuffInfo(BuffInfo baseInfo, ItemData targetItemData, EquipmentBag targetBag)
    {
        return GetBuffedItemStat(baseInfo, targetItemData, targetBag, ApplyBuffToBuffInfo, BuffCalculationMode.All, false);
    }

    public EnemyStat GetBuffedEnemyStat(EnemyStat baseStat, Enemy enemy)
    {
        if (baseStat == null)
            return null;

        EnemyStat result = baseStat.Clone();

        ApplyEnemyBuffs(result, storage.globalBuffs);
        ApplyEnemyBuffs(result, storage.futureEnemyBuffs);

        if (enemy != null && storage.enemyBuffs.TryGetValue(enemy, out List<ActiveBuff> enemyBuffs))
            ApplyEnemyBuffs(result, enemyBuffs);

        result.Clamp();
        return result;
    }

    public EnemySpawnerStat GetBuffedEnemySpawnerStat(EnemySpawnerStat baseStat, EnemySpawner spawner)
    {
        if (baseStat == null)
            return null;

        EnemySpawnerStat result = baseStat.Clone();

        ApplyEnemySpawnerBuffs(result, storage.globalEnemySpawnerBuffs);

        if (spawner != null && storage.enemySpawnerBuffs.TryGetValue(spawner, out List<ActiveBuff> spawnerBuffs))
            ApplyEnemySpawnerBuffs(result, spawnerBuffs);

        result.Clamp();
        return result;
    }

    private T GetBuffedItemStat<T>(T baseStat, ItemData targetItemData, EquipmentBag targetBag, System.Action<BuffStat, T> applyAction, BuffCalculationMode calculationMode, bool consumeUseCount) where T : class, IGameStat<T>
    {
        if (baseStat == null)
            return null;

        T result = baseStat.Clone();

        ApplyItemBuffs(result, storage.globalBuffs, targetItemData, targetBag, applyAction, calculationMode, consumeUseCount);

        if (targetBag != null && storage.bagBuffs.TryGetValue(targetBag, out List<ActiveBuff> bagBuffs))
            ApplyItemBuffs(result, bagBuffs, targetItemData, targetBag, applyAction, calculationMode, consumeUseCount);

        if (targetItemData != null && storage.itemBuffs.TryGetValue(targetItemData, out List<ActiveBuff> itemBuffs))
            ApplyItemBuffs(result, itemBuffs, targetItemData, targetBag, applyAction, calculationMode, consumeUseCount);

        if (targetItemData != null && targetItemData.series != ItemSeries.None && storage.itemSeriesBuffs.TryGetValue(targetItemData.series, out List<ActiveBuff> seriesBuffs))
            ApplyItemBuffs(result, seriesBuffs, targetItemData, targetBag, applyAction, calculationMode, consumeUseCount);

        result.Clamp();
        return result;
    }

    private void ApplyItemBuffs<T>(T target, List<ActiveBuff> buffs, ItemData targetItemData, EquipmentBag targetBag, System.Action<BuffStat, T> applyAction, BuffCalculationMode calculationMode, bool consumeUseCount) where T : class
    {
        if (target == null || buffs == null || applyAction == null)
            return;

        List<ActiveBuff> consumedBuffs = null;

        for (int i = 0; i < buffs.Count; i++)
        {
            ActiveBuff activeBuff = buffs[i];

            if (!CanUseBuff(activeBuff))
                continue;

            if (!CanUseBuffByCalculationMode(activeBuff, calculationMode))
                continue;

            if (!CanApplyBuffToItemTarget(activeBuff, targetItemData, targetBag))
                continue;

            int stackCount = Mathf.Max(1, activeBuff.stack);

            for (int stackIndex = 0; stackIndex < stackCount; stackIndex++)
                applyAction(activeBuff.buffStat, target);

            if (consumeUseCount && activeBuff.useLimitType == BuffUseLimitType.UseCount)
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

    private void ApplyEnemyBuffs(EnemyStat target, List<ActiveBuff> buffs)
    {
        if (target == null || buffs == null)
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

    private void ApplyEnemySpawnerBuffs(EnemySpawnerStat target, List<ActiveBuff> buffs)
    {
        if (target == null || buffs == null)
            return;

        for (int i = 0; i < buffs.Count; i++)
        {
            ActiveBuff activeBuff = buffs[i];

            if (!CanUseBuff(activeBuff))
                continue;

            int stackCount = Mathf.Max(1, activeBuff.stack);

            for (int stackIndex = 0; stackIndex < stackCount; stackIndex++)
                activeBuff.buffStat.ApplyToEnemySpawnerStat(target);
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

    private bool CanUseBuffByCalculationMode(ActiveBuff activeBuff, BuffCalculationMode calculationMode)
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

    private bool CanApplyBuffToItemTarget(ActiveBuff activeBuff, ItemData targetItemData, EquipmentBag targetBag)
    {
        if (activeBuff == null)
            return false;

        if (targetItemData == null)
            return false;

        if (activeBuff.targetScope == BuffTarget.Item && activeBuff.targetItemData != null && activeBuff.targetItemData != targetItemData)
            return false;

        if (activeBuff.targetScope == BuffTarget.ItemSeries)
        {
            if (activeBuff.targetSeries == ItemSeries.None)
                return false;

            if (targetItemData.series != activeBuff.targetSeries)
                return false;
        }

        bool isSelf = activeBuff.sourceItemData != null && activeBuff.sourceItemData == targetItemData;

        if (isSelf && !activeBuff.includeSelf)
            return false;

        return true;
    }

    private void ApplyBuffToAttackStat(BuffStat buffStat, AttackStat target)
    {
        if (buffStat == null)
            return;

        buffStat.ApplyToAttackStat(target);
    }

    private void ApplyBuffToBuffInfo(BuffStat buffStat, BuffInfo target)
    {
        if (buffStat == null)
            return;

        buffStat.ApplyToBuffInfo(target);
    }
}