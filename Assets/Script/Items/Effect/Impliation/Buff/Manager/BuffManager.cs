using System;
using System.Collections.Generic;
using UnityEngine;

public class BuffManager : MonoBehaviour
{
    [Header("UI")]
    public BuffUIManager buffUIManager;

    [Header("Debug")]
    [SerializeField] private bool useDebugInspector = true;

    [SerializeField]
    private List<ActiveBuff> debugAllActiveBuffs =
        new List<ActiveBuff>();

    [SerializeField]
    private List<DebugBuffGroup> debugBuffGroups =
        new List<DebugBuffGroup>();

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

        RefreshDebugInspector();
    }

    private void Update()
    {
        if (ticker == null)
            return;

        bool changed = ticker.Tick(Time.deltaTime);

        if (changed)
        {
            RefreshAllRegisteredEnemyStats();
            RefreshUI();
            RefreshDebugInspector();
        }

        if (useDebugInspector)
            RefreshDebugInspector();
    }

    public void RegisterBuff(
        BuffEffect effect,
        ItemEffectContext context
    )
    {
        if (registrar == null)
            return;

        registrar.RegisterBuff(effect, context);

        RefreshAllRegisteredEnemyStats();
        RefreshUI();
        RefreshDebugInspector();
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

        if (enemy != null)
            enemy.RefreshBuffedStat();

        RefreshDebugInspector();
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        if (storage == null)
            return;

        storage.UnregisterEnemy(enemy);

        RefreshUI();
        RefreshDebugInspector();
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

        enemy.RefreshBuffedStat();

        RefreshUI();
        RefreshDebugInspector();
    }

    public void ClearAllBuffs()
    {
        if (storage == null)
            return;

        storage.ClearAll();

        RefreshUI();
        RefreshDebugInspector();
    }

    private void RefreshAllRegisteredEnemyStats()
    {
        if (storage == null)
            return;

        for (int i = storage.registeredEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = storage.registeredEnemies[i];

            if (enemy == null)
            {
                storage.registeredEnemies.RemoveAt(i);
                continue;
            }

            enemy.RefreshBuffedStat();
        }
    }

    private void RefreshUI()
    {
        if (buffUIManager == null)
            return;

        buffUIManager.RefreshCurrentMode();
    }

    private void RefreshDebugInspector()
    {
        if (!useDebugInspector)
            return;

        if (storage == null)
            return;

        debugAllActiveBuffs.Clear();
        debugBuffGroups.Clear();

        AddDebugGroup("Global", "All", storage.globalBuffs);
        AddDebugGroup("Enemy Future", "All Current + Future", storage.futureEnemyBuffs);

        foreach (KeyValuePair<EquipmentBag, List<ActiveBuff>> pair in storage.bagBuffs)
        {
            string targetName = pair.Key != null ? pair.Key.name : "Null Bag";
            AddDebugGroup("Bag", targetName, pair.Value);
        }

        foreach (KeyValuePair<ItemData, List<ActiveBuff>> pair in storage.itemBuffs)
        {
            string targetName = pair.Key != null ? pair.Key.dataName : "Null Item";
            AddDebugGroup("Item", targetName, pair.Value);
        }

        foreach (KeyValuePair<Enemy, List<ActiveBuff>> pair in storage.enemyBuffs)
        {
            string targetName = pair.Key != null ? pair.Key.name : "Null Enemy";
            AddDebugGroup("Enemy", targetName, pair.Value);
        }
    }

    private void AddDebugGroup(
        string groupType,
        string targetName,
        List<ActiveBuff> buffs
    )
    {
        if (buffs == null)
            return;

        if (buffs.Count <= 0)
            return;

        DebugBuffGroup group = new DebugBuffGroup();
        group.groupType = groupType;
        group.targetName = targetName;

        for (int i = 0; i < buffs.Count; i++)
        {
            ActiveBuff buff = buffs[i];

            if (buff == null)
                continue;

            if (buff.IsExpired)
                continue;

            group.buffs.Add(buff);
            debugAllActiveBuffs.Add(buff);
        }

        if (group.buffs.Count > 0)
            debugBuffGroups.Add(group);
    }
}

[Serializable]
public class DebugBuffGroup
{
    public string groupType;
    public string targetName;
    public List<ActiveBuff> buffs = new List<ActiveBuff>();
}