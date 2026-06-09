using System;
using System.Collections.Generic;
using UnityEngine;

public class BuffManager : MonoBehaviour
{
    public static BuffManager instance;
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
    private readonly List<ActiveBuff> consumedBuffer = new List<ActiveBuff>();

    public BuffStorage Storage => storage;

    private void Awake()
    {
        instance = this;
        storage = new BuffStorage();
        ticker = new BuffTicker(storage);
        query = new BuffQuery(storage);

        RefreshDebugInspector();
    }

    private void Update()
    {
        if (ticker == null)
            return;

        if (ticker.Tick(Time.deltaTime))
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
        BuffInfo finalInfo = GetBuffedStatForItem(effect.buffInfo, context.sourceItemData, context.sourceBag);

        if (finalInfo == null)
            return;

        finalInfo.Clamp();

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

    private bool CanUseBuff(ActiveBuff buff, BuffQueryContext context, BuffCalculationMode calculationMode)
    {
        if (buff == null || buff.IsExpired)
            return false;

        if (buff.modifiers == null || buff.modifiers.Length <= 0)
            return false;

        if (!CanUseByCalculationMode(buff, calculationMode))
            return false;

        return buff.MatchesQuery(context);
    }

    private bool CanUseByCalculationMode(ActiveBuff buff, BuffCalculationMode calculationMode)
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


    #region Stat Query

    public T GetBuffedStatForItem<T>(
        T baseStat,
        ItemData targetItemData,
        EquipmentBag targetBag,
        BuffCalculationMode calculationMode = BuffCalculationMode.All,
        bool consumeUseCount = false
    ) where T : class, IGameStat<T>
    {
        return GetBuffedStat(
            baseStat,
            BuffQueryContext.ForItem(targetItemData, targetBag),
            calculationMode,
            consumeUseCount
        );
    }

    public T GetBuffedStatForTarget<T>(
        T baseStat,
        IBuffTarget target,
        BuffCalculationMode calculationMode = BuffCalculationMode.All,
        bool consumeUseCount = false
    ) where T : class, IGameStat<T>
    {
        return GetBuffedStat(
            baseStat,
            BuffQueryContext.ForTarget(target),
            calculationMode,
            consumeUseCount
        );
    }

    #endregion

    #endregion

    #region Buff Target Register

    public void RegisterBuffTarget(IBuffTarget target)
    {
        if (storage == null || target == null)
            return;

        storage.RegisterTarget(target);
        target.RefreshBuffedStat();
        RefreshDebugInspector();
    }

    public void UnregisterBuffTarget(IBuffTarget target)
    {
        if (storage == null || target == null)
            return;

        storage.UnregisterTarget(target);
        NotifyBuffChanged(BuffNotifyScope.Target);
    }

    public void ClearBuffsForTarget(IBuffTarget target)
    {
        if (storage == null || target == null)
            return;

        storage.RemoveBuffsForTarget(target);
        target.RefreshBuffedStat();
        NotifyBuffChanged(BuffNotifyScope.Target);
    }

    public List<IBuffTarget> GetRegisteredBuffTargetsUnsafe()
    {
        return storage != null ? storage.registeredTargets : new List<IBuffTarget>();
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

    public List<ActiveBuff> GetTargetBuffsAsList(IBuffTarget target)
    {
        return query != null ? query.GetTargetBuffsAsList(target) : new List<ActiveBuff>();
    }

    public List<ActiveBuff> GetVisibleTargetBuffsAsList(IBuffTarget target)
    {
        return query != null ? query.GetTargetBuffsAsList(target, true) : new List<ActiveBuff>();
    }

    public List<ActiveBuff> GetTargetGroupBuffsAsList(string targetGroup)
    {
        return query != null ? query.GetTargetGroupBuffsAsList(targetGroup) : new List<ActiveBuff>();
    }

    public List<ActiveBuff> GetVisibleTargetGroupBuffsAsList(string targetGroup)
    {
        return query != null ? query.GetTargetGroupBuffsAsList(targetGroup, true) : new List<ActiveBuff>();
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

        if (target.kind == BuffTargetKind.Target || target.kind == BuffTargetKind.Group)
            return BuffNotifyScope.Target;

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
        if (scope == BuffNotifyScope.All || scope == BuffNotifyScope.Target)
            RefreshAllRegisteredTargetStats();

        if (scope == BuffNotifyScope.All || scope == BuffNotifyScope.Item || scope == BuffNotifyScope.DynamicOnly)
            NotifyDynamicBuffReceivers();

        RefreshUI();
        RefreshDebugInspector();
    }

    private void RefreshAllRegisteredTargetStats()
    {
        if (storage == null)
            return;

        for (int i = storage.registeredTargets.Count - 1; i >= 0; i--)
        {
            IBuffTarget target = storage.registeredTargets[i];

            if (target == null || target.BuffTargetObject == null)
            {
                storage.registeredTargets.RemoveAt(i);
                continue;
            }

            target.RefreshBuffedStat();
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
            if (debugBuffGroups[i].groupType != groupType)
                continue;

            if (debugBuffGroups[i].targetName != targetName)
                continue;

            group = debugBuffGroups[i];
            break;
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
