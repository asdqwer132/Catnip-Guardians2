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

    private readonly List<IDynamicBuffReceiver> dynamicBuffReceivers =
        new List<IDynamicBuffReceiver>();

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
            NotifyBuffChanged();

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

        NotifyBuffChanged();
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
    public EnemySpawnerStat GetBuffedEnemySpawnerStat(EnemySpawnerStat baseStat, EnemySpawner spawner)
    {
        if (calculator == null)
            return baseStat;

        return calculator.GetBuffedEnemySpawnerStat(baseStat, spawner);
    }

    public void RegisterEnemySpawner(EnemySpawner spawner)
    {
        if (storage == null)
            return;

        storage.RegisterEnemySpawner(spawner);

        if (spawner != null)
            spawner.RefreshBuffedStat();

        RefreshDebugInspector();
    }

    public void UnregisterEnemySpawner(EnemySpawner spawner)
    {
        if (storage == null)
            return;

        storage.UnregisterEnemySpawner(spawner);

        NotifyBuffChanged();
    }
    public AttackStat GetSnapshotAttackStatAndConsume(
        AttackStat baseStat,
        ItemData targetItemData,
        EquipmentBag targetBag
    )
    {
        if (calculator == null)
            return baseStat;

        AttackStat result = calculator.GetBuffedAttackStat(
            baseStat,
            targetItemData,
            targetBag,
            BuffCalculationMode.SnapshotOnly,
            true
        );

        /*
         * UseCount  ConsumeUse() 0 Ǿ
         * ðó  ӱ ٸ ʿ䰡 .
         * Tick(0f)     UseCount   Ѵ.
         */
        if (ticker != null)
            ticker.Tick(0f);

        NotifyBuffChanged();

        return result;
    }

    public AttackStat GetDynamicAttackStat(
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
            targetBag,
            BuffCalculationMode.DynamicOnly,
            false
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

        NotifyBuffChanged();
    }

    public void RegisterDynamicBuffReceiver(IDynamicBuffReceiver receiver)
    {
        if (receiver == null)
            return;

        if (dynamicBuffReceivers.Contains(receiver))
            return;

        dynamicBuffReceivers.Add(receiver);
    }

    public void UnregisterDynamicBuffReceiver(IDynamicBuffReceiver receiver)
    {
        if (receiver == null)
            return;

        dynamicBuffReceivers.Remove(receiver);
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

    public List<ActiveBuff> GetItemSeriesBuffsAsList(ItemSeries series)
    {
        if (query == null)
            return new List<ActiveBuff>();

        return query.GetItemSeriesBuffsAsList(series);
    }

    public List<ActiveBuff> GetVisibleItemSeriesBuffsAsList(ItemSeries series)
    {
        if (query == null)
            return new List<ActiveBuff>();

        return query.GetItemSeriesBuffsAsList(series, true);
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

        NotifyBuffChanged();
    }

    public void ClearAllBuffs()
    {
        if (storage == null)
            return;

        storage.ClearAll();

        NotifyBuffChanged();
    }

    private void NotifyBuffChanged()
    {
        RefreshAllRegisteredEnemyStats();
        RefreshAllRegisteredEnemySpawnerStats();
        RefreshUI();
        RefreshDebugInspector();
        NotifyDynamicBuffReceivers();
    }
    private void RefreshAllRegisteredEnemySpawnerStats()
    {
        if (storage == null)
            return;

        for (int i = storage.registeredEnemySpawners.Count - 1; i >= 0; i--)
        {
            EnemySpawner spawner = storage.registeredEnemySpawners[i];

            if (spawner == null)
            {
                storage.registeredEnemySpawners.RemoveAt(i);
                continue;
            }

            spawner.RefreshBuffedStat();
        }
    }
    private void NotifyDynamicBuffReceivers()
    {
        for (int i = dynamicBuffReceivers.Count - 1; i >= 0; i--)
        {
            IDynamicBuffReceiver receiver = dynamicBuffReceivers[i];

            if (receiver == null)
            {
                dynamicBuffReceivers.RemoveAt(i);
                continue;
            }

            receiver.OnDynamicBuffChanged();
        }
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

    #region Debug

    private void RefreshDebugInspector()
    {
        if (!useDebugInspector)
            return;

        if (storage == null)
            return;

        debugAllActiveBuffs.Clear();
        debugBuffGroups.Clear();

        AddDebugGroup("Global", "All", storage.globalBuffs);
        AddDebugGroup("Enemy Spawner Future", "All Spawners", storage.globalEnemySpawnerBuffs);

        AddDebugGroup("Enemy Future", "All Current + Future", storage.futureEnemyBuffs);

        foreach (KeyValuePair<EquipmentBag, List<ActiveBuff>> pair in storage.bagBuffs)
        {
            string targetName = pair.Key != null ? pair.Key.name : "Null Bag";
            AddDebugGroup("Bag", targetName, pair.Value);
        }
        foreach (KeyValuePair<EnemySpawner, List<ActiveBuff>> pair in storage.enemySpawnerBuffs)
        {
            string targetName = pair.Key != null ? pair.Key.name : "Null Spawner";
            AddDebugGroup("Enemy Spawner", targetName, pair.Value);
        }
        foreach (KeyValuePair<ItemData, List<ActiveBuff>> pair in storage.itemBuffs)
        {
            string targetName = pair.Key != null ? pair.Key.GetDataName() : "Null Item";
            AddDebugGroup("Item", targetName, pair.Value);
        }

        foreach (KeyValuePair<ItemSeries, List<ActiveBuff>> pair in storage.itemSeriesBuffs)
        {
            AddDebugGroup("Item Series", pair.Key.ToString(), pair.Value);
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

    #endregion
}

[Serializable]
public class DebugBuffGroup
{
    public string groupType;
    public string targetName;
    public List<ActiveBuff> buffs = new List<ActiveBuff>();
}