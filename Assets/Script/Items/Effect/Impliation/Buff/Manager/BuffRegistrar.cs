using System.Collections.Generic;
using UnityEngine;

public class BuffRegistrar
{
    private BuffStorage storage;
    private BuffStatCalculator calculator;

    public BuffRegistrar(BuffStorage storage, BuffStatCalculator calculator)
    {
        this.storage = storage;
        this.calculator = calculator;
    }

    public void RegisterBuff(BuffEffect effect, ItemEffectContext context)
    {
        if (effect == null || context == null || effect.bonus == null || effect.buffInfo == null)
            return;

        BuffInfo finalBuffInfo = calculator.GetBuffedBuffInfo(effect.buffInfo, context.sourceItemData, context.sourceBag);
        if (finalBuffInfo == null)
            return;

        finalBuffInfo.Clamp();

        if (finalBuffInfo.useLimitType == BuffUseLimitType.Time && finalBuffInfo.duration <= 0f)
            return;

        switch (effect.targetScope)
        {
            case BuffTarget.Self:
                RegisterOrUpdateItemBuff(context.sourceItemData, effect, context, finalBuffInfo);
                break;
            case BuffTarget.Item:
                RegisterOrUpdateItemBuff(effect.targetItemData, effect, context, finalBuffInfo);
                break;
            case BuffTarget.ItemSeries:
                RegisterOrUpdateItemSeriesBuff(effect.targetSeries, effect, context, finalBuffInfo);
                break;
            case BuffTarget.Bag:
                RegisterOrUpdateBagBuff(context.sourceBag, effect, context, finalBuffInfo);
                break;
            case BuffTarget.All:
                RegisterOrUpdateGlobalBuff(effect, context, finalBuffInfo);
                break;
            case BuffTarget.EnemyInRange:
                RegisterOrUpdateEnemiesInRange(effect, context, finalBuffInfo);
                break;
            case BuffTarget.AllEnemies:
                RegisterOrUpdateEnemyGlobalBuff(effect, context, finalBuffInfo, BuffTarget.AllEnemies);
                break;
            case BuffTarget.AllEnemiesIncludingFuture:
                RegisterOrUpdateEnemyGlobalBuff(effect, context, finalBuffInfo, BuffTarget.AllEnemiesIncludingFuture);
                break;
            case BuffTarget.EnemySpawner:
                RegisterOrUpdateEnemySpawnerBuff(effect.targetEnemySpawner, effect, context, finalBuffInfo);
                break;
            case BuffTarget.AllEnemySpawners:
                RegisterOrUpdateAllEnemySpawnersBuff(effect, context, finalBuffInfo);
                break;
        }
    }

    private void RegisterOrUpdateGlobalBuff(BuffEffect effect, ItemEffectContext context, BuffInfo finalBuffInfo)
    {
        RegisterOrUpdateList(storage.globalBuffs, effect, context, finalBuffInfo, BuffTarget.All);
    }

    private void RegisterOrUpdateEnemyGlobalBuff(BuffEffect effect, ItemEffectContext context, BuffInfo finalBuffInfo, BuffTarget targetScope)
    {
        RegisterOrUpdateList(storage.futureEnemyBuffs, effect, context, finalBuffInfo, targetScope);
    }

    private void RegisterOrUpdateAllEnemySpawnersBuff(BuffEffect effect, ItemEffectContext context, BuffInfo finalBuffInfo)
    {
        RegisterOrUpdateList(storage.globalEnemySpawnerBuffs, effect, context, finalBuffInfo, BuffTarget.AllEnemySpawners);
    }

    private void RegisterOrUpdateBagBuff(EquipmentBag bag, BuffEffect effect, ItemEffectContext context, BuffInfo finalBuffInfo)
    {
        List<ActiveBuff> list = storage.GetOrCreateBagBuffs(bag);
        RegisterOrUpdateList(list, effect, context, finalBuffInfo, BuffTarget.Bag, null, bag);
    }

    private void RegisterOrUpdateItemBuff(ItemData itemData, BuffEffect effect, ItemEffectContext context, BuffInfo finalBuffInfo)
    {
        List<ActiveBuff> list = storage.GetOrCreateItemBuffs(itemData);
        RegisterOrUpdateList(list, effect, context, finalBuffInfo, effect.targetScope, itemData);
    }

    private void RegisterOrUpdateItemSeriesBuff(ItemSeries series, BuffEffect effect, ItemEffectContext context, BuffInfo finalBuffInfo)
    {
        List<ActiveBuff> list = storage.GetOrCreateItemSeriesBuffs(series);
        RegisterOrUpdateList(list, effect, context, finalBuffInfo, BuffTarget.ItemSeries, null, null, series);
    }

    private void RegisterOrUpdateEnemyBuff(Enemy enemy, BuffEffect effect, ItemEffectContext context, BuffInfo finalBuffInfo)
    {
        List<ActiveBuff> list = storage.GetOrCreateEnemyBuffs(enemy);
        RegisterOrUpdateList(list, effect, context, finalBuffInfo, BuffTarget.EnemyInRange, null, null, ItemSeries.None, enemy);
    }

    private void RegisterOrUpdateEnemySpawnerBuff(EnemySpawner spawner, BuffEffect effect, ItemEffectContext context, BuffInfo finalBuffInfo)
    {
        List<ActiveBuff> list = storage.GetOrCreateEnemySpawnerBuffs(spawner);
        RegisterOrUpdateList(list, effect, context, finalBuffInfo, BuffTarget.EnemySpawner, null, null, ItemSeries.None, null, spawner);
    }

    private void RegisterOrUpdateEnemiesInRange(BuffEffect effect, ItemEffectContext context, BuffInfo finalBuffInfo)
    {
        EnemyBuffStat enemyBuffStat = effect.GetEnemyBuffStat();
        if (enemyBuffStat == null)
            return;

        Vector3 position = context.targetPosition;
        position.z = 0f;

        float radius = Mathf.Max(0.01f, enemyBuffStat.enemyBuffRadius);
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, radius, enemyBuffStat.enemyTargetLayer);
        HashSet<Enemy> checkedEnemies = new HashSet<Enemy>();

        for (int i = 0; i < hits.Length; i++)
        {
            Enemy enemy = hits[i].GetComponent<Enemy>();
            if (enemy == null)
                enemy = hits[i].GetComponentInParent<Enemy>();
            if (enemy == null || checkedEnemies.Contains(enemy))
                continue;
            if (!enemyBuffStat.affectDeadEnemies && enemy.IsDead)
                continue;

            checkedEnemies.Add(enemy);
            RegisterOrUpdateEnemyBuff(enemy, effect, context, finalBuffInfo);
        }
    }

    private void RegisterOrUpdateList(List<ActiveBuff> list, BuffEffect effect, ItemEffectContext context, BuffInfo finalBuffInfo, BuffTarget targetScope, ItemData targetItemData = null, EquipmentBag targetBag = null, ItemSeries targetSeries = ItemSeries.None, Enemy targetEnemy = null, EnemySpawner targetEnemySpawner = null)
    {
        if (list == null)
            return;

        ActiveBuff existing = FindSameBuff(list, context.sourceItemData, context.sourceBag, context.currentEffectData);

        if (existing != null)
        {
            existing.targetScope = targetScope;
            existing.targetItemData = targetItemData;
            existing.targetBag = targetBag;
            existing.targetSeries = targetSeries;
            existing.targetEnemy = targetEnemy;
            existing.targetEnemySpawner = targetEnemySpawner;
            existing.ApplyRegisterAgain(finalBuffInfo);
            return;
        }

        list.Add(new ActiveBuff(effect.bonus, finalBuffInfo, context.sourceItemData, context.sourceBag, context.currentEffectData, effect.includeSelf, effect.showInUI, targetScope, targetItemData, targetBag, targetSeries, targetEnemy, targetEnemySpawner));
    }

    private ActiveBuff FindSameBuff(List<ActiveBuff> buffs, ItemData sourceItemData, EquipmentBag sourceBag, ItemEffectData sourceEffectData)
    {
        if (buffs == null)
            return null;

        for (int i = 0; i < buffs.Count; i++)
        {
            ActiveBuff buff = buffs[i];
            if (buff != null && !buff.IsExpired && buff.IsSameBuff(sourceItemData, sourceBag, sourceEffectData))
                return buff;
        }

        return null;
    }
}