using System;
using System.Collections.Generic;
using UnityEngine;

public class BuffManager : MonoBehaviour
{
    [Header("UI")]
    public BuffUIManager buffUIManager;

    [Header("Debug")]
    [SerializeField] private bool useDebugInspector = true;
    [SerializeField] private List<ActiveBuff> debugAllActiveBuffs = new List<ActiveBuff>();
    [SerializeField] private List<DebugBuffGroup> debugBuffGroups = new List<DebugBuffGroup>();

    private BuffStorage storage;
    private BuffTicker ticker;
    private BuffQuery query;

    private readonly List<IDynamicBuffReceiver> dynamicBuffReceivers = new List<IDynamicBuffReceiver>();
    private readonly List<BuffTargetHandle> resolvedTargetsBuffer = new List<BuffTargetHandle>();
    private readonly List<ActiveBuff> consumedBuffer = new List<ActiveBuff>();

    public BuffStorage Storage => storage;

    private void Awake()
    {
        storage = new BuffStorage();
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
            NotifyBuffChanged(BuffNotifyScope.All);

        if (useDebugInspector)
            RefreshDebugInspector();
    }

    public void RegisterBuff(BuffEffect effect, ItemEffectContext itemContext)
    {
        if (effect == null || itemContext == null)
            return;

        if (!effect.HasValidModifier())
            return;

        BuffRegisterContext context = new BuffRegisterContext(itemContext, this);

        BuffInfo finalInfo = GetBuffedBuffInfo(
            effect.buffInfo,
            context.sourceItemData,
            context.sourceBag
        );

        if (finalInfo == null)
            return;

        finalInfo.Clamp();

        resolvedTargetsBuffer.Clear();

        List<BuffTargetHandle> targets = effect.ResolveTargets(context);

        if (targets == null || targets.Count <= 0)
            return;

        BuffNotifyScope notifyScope = BuffNotifyScope.Item;

        for (int i = 0; i < targets.Count; i++)
        {
            BuffTargetHandle target = targets[i];

            if (target == null)
                continue;

            ActiveBuff activeBuff = new ActiveBuff(
                effect.modifiers,
                finalInfo,
                context.sourceItemData,
                context.sourceBag,
                context.sourceEffectData,
                target,
                effect.includeSelf,
                effect.showInUI
            );

            storage.AddOrRefresh(activeBuff, finalInfo);
            notifyScope = MergeNotifyScope(notifyScope, GetNotifyScope(target));
        }

        NotifyBuffChanged(notifyScope);
    }

    public T GetBuffedStat<T>(
        T baseStat,
        BuffQueryContext context,
        BuffCalculationMode calculationMode = BuffCalculationMode.All,
        bool consumeUseCount = false
    ) where T : class, IGameStat<T>
    {
        if (baseStat == null)
            return null;

        if (storage == null)
            return baseStat;

        T result = baseStat.Clone();

        if (result == null)
            return baseStat;

        consumedBuffer.Clear();

        for (int i = 0; i < storage.activeBuffs.Count; i++)
        {
            ActiveBuff buff = storage.activeBuffs[i];

            if (!CanUseBuff(buff, context, calculationMode))
                continue;

            ApplyModifiers(buff, result, context);

            if (consumeUseCount && buff.useLimitType == BuffUseLimitType.UseCount)
                consumedBuffer.Add(buff);
        }

        for (int i = 0; i < consumedBuffer.Count; i++)
            consumedBuffer[i].ConsumeUse();

        if (consumeUseCount && consumedBuffer.Count > 0)
        {
            ticker.Tick(0f);
            NotifyBuffChanged(BuffNotifyScope.DynamicOnly);
        }

        result.Clamp();
        return result;
    }

    private void ApplyModifiers<T>(ActiveBuff buff, T stat, BuffQueryContext context) where T : class
    {
        if (buff == null || buff.modifiers == null || stat == null)
            return;

        for (int i = 0; i < buff.modifiers.Length; i++)
        {
            BuffModifier modifier = buff.modifiers[i];

            if (modifier == null)
                continue;

            if (!modifier.CanApplyTo(stat, context))
                continue;

            modifier.ApplyTo(stat, Mathf.Max(1, buff.stack), context);
        }
    }

    private bool CanUseBuff(
        ActiveBuff buff,
        BuffQueryContext context,
        BuffCalculationMode calculationMode
    )
    {
        if (buff == null || buff.IsExpired)
            return false;

        if (buff.modifiers == null || buff.modifiers.Length <= 0)
            return false;

        if (!CanUseByCalculationMode(buff, calculationMode))
            return false;

        if (!buff.MatchesQuery(context))
            return false;

        return true;
    }

    private bool CanUseByCalculationMode(
        ActiveBuff buff,
        BuffCalculationMode calculationMode
    )
    {
        if (calculationMode == BuffCalculationMode.All)
            return true;

        if (calculationMode == BuffCalculationMode.SnapshotOnly)
            return buff.applyTiming == BuffApplyTiming.Snapshot;

        if (calculationMode == BuffCalculationMode.DynamicOnly)
            return buff.applyTiming == BuffApplyTiming.Dynamic;

        return true;
    }

    #region Stat Query

    public AttackStat GetBuffedAttackStat(
        AttackStat baseStat,
        ItemData targetItemData,
        EquipmentBag targetBag
    )
    {
        return GetBuffedStat(
            baseStat,
            BuffQueryContext.ForItem(targetItemData, targetBag)
        );
    }

    public AttackStat GetSnapshotAttackStatAndConsume(
        AttackStat baseStat,
        ItemData targetItemData,
        EquipmentBag targetBag
    )
    {
        return GetBuffedStat(
            baseStat,
            BuffQueryContext.ForItem(targetItemData, targetBag),
            BuffCalculationMode.SnapshotOnly,
            true
        );
    }

    public AttackStat GetDynamicAttackStat(
        AttackStat baseStat,
        ItemData targetItemData,
        EquipmentBag targetBag
    )
    {
        return GetBuffedStat(
            baseStat,
            BuffQueryContext.ForItem(targetItemData, targetBag),
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
        return GetBuffedStat(
            baseInfo,
            BuffQueryContext.ForItem(targetItemData, targetBag),
            BuffCalculationMode.All,
            false
        );
    }

    public EnemyStat GetBuffedEnemyStat(EnemyStat baseStat, Enemy enemy)
    {
        return GetBuffedStat(
            baseStat,
            BuffQueryContext.ForEnemy(enemy),
            BuffCalculationMode.All,
            false
        );
    }

    public EnemySpawnerStat GetBuffedEnemySpawnerStat(
        EnemySpawnerStat baseStat,
        EnemySpawner spawner
    )
    {
        return GetBuffedStat(
            baseStat,
            BuffQueryContext.ForEnemySpawner(spawner),
            BuffCalculationMode.All,
            false
        );
    }

    public PlayerStat GetBuffedPlayerStat(PlayerStat baseStat, Player player)
    {
        return GetBuffedStat(
            baseStat,
            BuffQueryContext.ForPlayer(player),
            BuffCalculationMode.All,
            false
        );
    }

    #endregion

    #region Enemy Register

    public void RegisterEnemy(Enemy enemy)
    {
        if (storage == null || enemy == null)
            return;

        storage.RegisterEnemy(enemy);
        enemy.RefreshBuffedStat();

        RefreshDebugInspector();
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        if (storage == null || enemy == null)
            return;

        storage.UnregisterEnemy(enemy);
        NotifyBuffChanged(BuffNotifyScope.Enemy);
    }

    public void ClearEnemyBuffs(Enemy enemy)
    {
        if (storage == null || enemy == null)
            return;

        storage.RemoveBuffsForEnemy(enemy);
        enemy.RefreshBuffedStat();

        NotifyBuffChanged(BuffNotifyScope.Enemy);
    }

    public List<Enemy> GetRegisteredEnemiesUnsafe()
    {
        if (storage == null)
            return new List<Enemy>();

        return storage.registeredEnemies;
    }

    #endregion

    #region Enemy Spawner Register

    public void RegisterEnemySpawner(EnemySpawner spawner)
    {
        if (storage == null || spawner == null)
            return;

        storage.RegisterEnemySpawner(spawner);
        spawner.RefreshBuffedStat();

        RefreshDebugInspector();
    }

    public void UnregisterEnemySpawner(EnemySpawner spawner)
    {
        if (storage == null || spawner == null)
            return;

        storage.UnregisterEnemySpawner(spawner);
        NotifyBuffChanged(BuffNotifyScope.EnemySpawner);
    }

    #endregion

    #region Player Register

    public void RegisterPlayer(Player player)
    {
        if (storage == null || player == null)
            return;

        storage.RegisterPlayer(player);
        player.RefreshBuffedStat();

        RefreshDebugInspector();
    }

    public void UnregisterPlayer(Player player)
    {
        if (storage == null || player == null)
            return;

        storage.UnregisterPlayer(player);
        NotifyBuffChanged(BuffNotifyScope.Player);
    }

    public void ClearPlayerBuffs(Player player)
    {
        if (storage == null || player == null)
            return;

        storage.RemoveBuffsForPlayer(player);
        player.RefreshBuffedStat();

        NotifyBuffChanged(BuffNotifyScope.Player);
    }

    #endregion

    #region Dynamic Receiver

    public void RegisterDynamicBuffReceiver(IDynamicBuffReceiver receiver)
    {
        if (receiver == null || dynamicBuffReceivers.Contains(receiver))
            return;

        dynamicBuffReceivers.Add(receiver);
    }

    public void UnregisterDynamicBuffReceiver(IDynamicBuffReceiver receiver)
    {
        if (receiver == null)
            return;

        dynamicBuffReceivers.Remove(receiver);
    }

    #endregion

    #region Buff Query List

    public List<ActiveBuff> GetAllActiveBuffs()
    {
        return query != null ? query.GetAllActiveBuffs() : new List<ActiveBuff>();
    }

    public List<ActiveBuff> GetAllVisibleBuffs()
    {
        return query != null ? query.GetAllVisibleBuffs() : new List<ActiveBuff>();
    }

    public List<ActiveBuff> GetBagBuffsAsList(EquipmentBag bag)
    {
        return query != null ? query.GetBagBuffsAsList(bag) : new List<ActiveBuff>();
    }

    public List<ActiveBuff> GetVisibleBagBuffsAsList(EquipmentBag bag)
    {
        return query != null ? query.GetBagBuffsAsList(bag, true) : new List<ActiveBuff>();
    }

    public List<ActiveBuff> GetItemBuffsAsList(ItemData itemData)
    {
        return query != null ? query.GetItemBuffsAsList(itemData) : new List<ActiveBuff>();
    }

    public List<ActiveBuff> GetVisibleItemBuffsAsList(ItemData itemData)
    {
        return query != null ? query.GetItemBuffsAsList(itemData, true) : new List<ActiveBuff>();
    }

    public List<ActiveBuff> GetItemSeriesBuffsAsList(ItemSeries series)
    {
        return query != null ? query.GetItemSeriesBuffsAsList(series) : new List<ActiveBuff>();
    }

    public List<ActiveBuff> GetVisibleItemSeriesBuffsAsList(ItemSeries series)
    {
        return query != null ? query.GetItemSeriesBuffsAsList(series, true) : new List<ActiveBuff>();
    }

    public List<ActiveBuff> GetEnemyBuffsAsList(Enemy enemy)
    {
        return query != null ? query.GetEnemyBuffsAsList(enemy) : new List<ActiveBuff>();
    }

    public List<ActiveBuff> GetVisibleEnemyBuffsAsList(Enemy enemy)
    {
        return query != null ? query.GetEnemyBuffsAsList(enemy, true) : new List<ActiveBuff>();
    }

    public List<ActiveBuff> GetEnemySpawnerBuffsAsList(EnemySpawner spawner)
    {
        return query != null ? query.GetEnemySpawnerBuffsAsList(spawner) : new List<ActiveBuff>();
    }

    public List<ActiveBuff> GetVisibleEnemySpawnerBuffsAsList(EnemySpawner spawner)
    {
        return query != null ? query.GetEnemySpawnerBuffsAsList(spawner, true) : new List<ActiveBuff>();
    }

    #endregion

    public void ClearAllBuffs()
    {
        if (storage == null)
            return;

        storage.ClearAll();
        NotifyBuffChanged(BuffNotifyScope.All);
    }

    private BuffNotifyScope GetNotifyScope(BuffTargetHandle target)
    {
        if (target == null)
            return BuffNotifyScope.All;

        if (target.kind == BuffTargetKind.Enemy ||
            target.kind == BuffTargetKind.AllEnemiesIncludingFuture)
        {
            return BuffNotifyScope.Enemy;
        }

        if (target.kind == BuffTargetKind.EnemySpawner ||
            target.kind == BuffTargetKind.AllEnemySpawners)
        {
            return BuffNotifyScope.EnemySpawner;
        }

        if (target.kind == BuffTargetKind.Player ||
            target.kind == BuffTargetKind.AllPlayers)
        {
            return BuffNotifyScope.Player;
        }

        return BuffNotifyScope.Item;
    }

    private BuffNotifyScope MergeNotifyScope(BuffNotifyScope a, BuffNotifyScope b)
    {
        if (a == b)
            return a;

        if (a == BuffNotifyScope.All || b == BuffNotifyScope.All)
            return BuffNotifyScope.All;

        return BuffNotifyScope.All;
    }

    private void NotifyBuffChanged(BuffNotifyScope scope)
    {
        if (scope == BuffNotifyScope.All || scope == BuffNotifyScope.Enemy)
            RefreshAllRegisteredEnemyStats();

        if (scope == BuffNotifyScope.All || scope == BuffNotifyScope.EnemySpawner)
            RefreshAllRegisteredEnemySpawnerStats();

        if (scope == BuffNotifyScope.All || scope == BuffNotifyScope.Player)
            RefreshAllRegisteredPlayerStats();

        if (scope == BuffNotifyScope.All ||
            scope == BuffNotifyScope.Item ||
            scope == BuffNotifyScope.DynamicOnly)
        {
            NotifyDynamicBuffReceivers();
        }

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

    private void RefreshAllRegisteredPlayerStats()
    {
        if (storage == null)
            return;

        for (int i = storage.registeredPlayers.Count - 1; i >= 0; i--)
        {
            Player player = storage.registeredPlayers[i];

            if (player == null)
            {
                storage.registeredPlayers.RemoveAt(i);
                continue;
            }

            player.RefreshBuffedStat();
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

    private void RefreshUI()
    {
        if (buffUIManager != null)
            buffUIManager.RefreshCurrentMode();
    }

    private void RefreshDebugInspector()
    {
        if (!useDebugInspector || storage == null)
            return;

        debugAllActiveBuffs.Clear();
        debugBuffGroups.Clear();

        for (int i = 0; i < storage.activeBuffs.Count; i++)
        {
            ActiveBuff buff = storage.activeBuffs[i];

            if (buff == null || buff.IsExpired)
                continue;

            debugAllActiveBuffs.Add(buff);
            AddDebugGroup(buff);
        }
    }

    private void AddDebugGroup(ActiveBuff buff)
    {
        if (buff == null || buff.target == null)
            return;

        string groupType = buff.target.kind.ToString();
        string targetName = buff.target.GetDebugName();

        DebugBuffGroup group = null;

        for (int i = 0; i < debugBuffGroups.Count; i++)
        {
            if (debugBuffGroups[i].groupType == groupType &&
                debugBuffGroups[i].targetName == targetName)
            {
                group = debugBuffGroups[i];
                break;
            }
        }

        if (group == null)
        {
            group = new DebugBuffGroup
            {
                groupType = groupType,
                targetName = targetName
            };

            debugBuffGroups.Add(group);
        }

        group.buffs.Add(buff);
    }
}

[Serializable]
public class DebugBuffGroup
{
    public string groupType;
    public string targetName;
    public List<ActiveBuff> buffs = new List<ActiveBuff>();
}