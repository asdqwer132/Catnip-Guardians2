using System.Collections.Generic;
using UnityEngine;

public class BuffManager : MonoBehaviour
{
    [Header("UI")]
    public BuffUIManager buffUIManager;

    private BuffStorage storage;
    private BuffTicker ticker;
    private BuffStatCalculator calculator;
    private BuffRegistrar registrar;
    private BuffQuery query;

    public BuffStorage Storage => storage;

    private void Awake()
    {
        storage = new BuffStorage();

        calculator = new BuffStatCalculator(storage);
        registrar = new BuffRegistrar(storage, calculator);
        ticker = new BuffTicker(storage);
        query = new BuffQuery(storage);
    }

    private void Update()
    {
        if (ticker == null)
            return;

        bool changed = ticker.Tick(Time.deltaTime);

        if (changed)
            RefreshUI();
    }

    public void RegisterBuff(
        BuffEffect effect,
        ItemEffectContext context
    )
    {
        if (registrar == null)
            return;

        registrar.RegisterBuff(effect, context);
        RefreshUI();
    }

    public AttackStat GetBuffedAttackStat(
        AttackStat baseStat,
        ItemData targetItemData,
        EquipmentBag targetBag
    )
    {
        if (calculator == null)
            return baseStat;

        return calculator.GetBuffedAttackStat(
            baseStat,
            targetItemData,
            targetBag
        );
    }

    public BuffInfo GetBuffedBuffInfo(
        BuffInfo baseInfo,
        ItemData targetItemData,
        EquipmentBag targetBag
    )
    {
        if (calculator == null)
            return baseInfo;

        return calculator.GetBuffedBuffInfo(
            baseInfo,
            targetItemData,
            targetBag
        );
    }

    public EnemyStat GetBuffedEnemyStat(
        EnemyStat baseStat,
        Enemy enemy
    )
    {
        if (calculator == null)
            return baseStat;

        return calculator.GetBuffedEnemyStat(
            baseStat,
            enemy
        );
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (storage == null)
            return;

        storage.RegisterEnemy(enemy);
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        if (storage == null)
            return;

        storage.UnregisterEnemy(enemy);
        RefreshUI();
    }

    public List<ActiveBuff> GetAllActiveBuffs()
    {
        if (query == null)
            return new List<ActiveBuff>();

        return query.GetAllActiveBuffs();
    }

    public List<ActiveBuff> GetAllVisibleBuffs()
    {
        if (query == null)
            return new List<ActiveBuff>();

        return query.GetAllVisibleBuffs();
    }

    public List<ActiveBuff> GetBagBuffsAsList(EquipmentBag bag)
    {
        if (query == null)
            return new List<ActiveBuff>();

        return query.GetBagBuffsAsList(bag);
    }

    public List<ActiveBuff> GetVisibleBagBuffsAsList(EquipmentBag bag)
    {
        if (query == null)
            return new List<ActiveBuff>();

        return query.GetBagBuffsAsList(bag, true);
    }

    public List<ActiveBuff> GetItemBuffsAsList(ItemData itemData)
    {
        if (query == null)
            return new List<ActiveBuff>();

        return query.GetItemBuffsAsList(itemData);
    }

    public List<ActiveBuff> GetVisibleItemBuffsAsList(ItemData itemData)
    {
        if (query == null)
            return new List<ActiveBuff>();

        return query.GetItemBuffsAsList(itemData, true);
    }

    public List<ActiveBuff> GetEnemyBuffsAsList(Enemy enemy)
    {
        if (query == null)
            return new List<ActiveBuff>();

        return query.GetEnemyBuffsAsList(enemy);
    }

    public List<ActiveBuff> GetVisibleEnemyBuffsAsList(Enemy enemy)
    {
        if (query == null)
            return new List<ActiveBuff>();

        return query.GetEnemyBuffsAsList(enemy, true);
    }

    public void ClearEnemyBuffs(Enemy enemy)
    {
        if (storage == null)
            return;

        if (enemy == null)
            return;

        storage.enemyBuffs.Remove(enemy);
        RefreshUI();
    }

    public void ClearAllBuffs()
    {
        if (storage == null)
            return;

        storage.ClearAll();
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (buffUIManager == null)
            return;

        buffUIManager.RefreshCurrentMode();
    }
}