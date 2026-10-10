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
    [SerializeField] private List<ActiveBuff> debugNormalActiveBuffs = new List<ActiveBuff>();
    [SerializeField] private List<ActiveBuff> debugInfiniteActiveBuffs = new List<ActiveBuff>();
    [SerializeField] private List<DebugBuffGroup> debugBuffGroups = new List<DebugBuffGroup>();

    private BuffStorage storage;
    private BuffTicker ticker;
    private BuffQuery query;

    private readonly List<IDynamicBuffReceiver> dynamicBuffReceivers = new List<IDynamicBuffReceiver>();
    private readonly Stack<List<BuffStatEntry>> calculationPool = new Stack<List<BuffStatEntry>>();
    private readonly Stack<BuffItemUseSession> itemUsePool = new Stack<BuffItemUseSession>();
    private readonly List<BuffItemUseSession> itemUseStack = new List<BuffItemUseSession>();
    private readonly Dictionary<UnityEngine.Object, BuffEffect> statusMarkers = new Dictionary<UnityEngine.Object, BuffEffect>();
    private readonly List<EffectBuffUIHandle> effectUIEntries = new List<EffectBuffUIHandle>();
    private ulong nextItemUseId;
    private int notificationDepth;

    private struct BuffStatEntry
    {
        public ActiveBuff buff;
        public BuffModifier[] modifiers;
        public int stack;
        public ulong registrationVersion;
        public bool applied;
    }

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

        if (PruneEffectUI()) RefreshUI();

        // 디버그 목록은 등록/해제 시만 갱신. 시간/횟수는 같은 ActiveBuff 참조로 확인한다.
    }

    private void OnDestroy()
    {
        for (int i = 0; i < effectUIEntries.Count; i++) effectUIEntries[i].Invalidate();
        effectUIEntries.Clear();
        if (storage != null) storage.ClearAll();
        if (instance == this)
            instance = null;

        for (int i = 0; i < itemUseStack.Count; i++)
            itemUseStack[i].Clear();
        itemUseStack.Clear();
        foreach (BuffEffect marker in statusMarkers.Values)
            if (marker != null)
            {
                if (Application.isPlaying) Destroy(marker);
                else DestroyImmediate(marker);
            }
        statusMarkers.Clear();
    }

    // 스탯 조회/미리보기와 실제 아이템 사용을 분리한다.
    // 모든 효과를 실행하기 전에 시작하고, 모든 효과가 끝난 뒤 완료한다.
    public BuffItemUseToken BeginItemUse(ItemData itemData, EquipmentBag sourceBag)
    {
        if (storage == null || itemData == null)
            return default(BuffItemUseToken);

        BuffItemUseSession session = itemUsePool.Count > 0
            ? itemUsePool.Pop() : new BuffItemUseSession();
        session.Clear();
        session.manager = this;
        unchecked { session.id = ++nextItemUseId; }
        session.active = true;

        for (int i = 0; i < storage.useCountBuffs.Count; i++)
        {
            ActiveBuff buff = storage.useCountBuffs[i];
            if (buff == null || buff.IsExpired)
                continue;

            bool onItemUse = buff.ShouldConsumeOnItemUse(itemData);
            if (!onItemUse && buff.useCountConsumeMode != BuffUseCountConsumeMode.WhenBuffApplied)
                continue;

            session.candidates.Add(new BuffUseCandidate
            {
                buff = buff,
                registrationVersion = buff.RegistrationVersion,
                consumeOnComplete = onItemUse
            });
        }

        itemUseStack.Add(session);
        return new BuffItemUseToken(session, session.id);
    }

    public void EndItemUse(BuffItemUseToken token, bool succeeded = true)
    {
        BuffItemUseSession session = token.session;
        if (session == null || !session.active || session.id != token.id || session.manager != this)
            return;

        int last = itemUseStack.Count - 1;
        if (last < 0 || itemUseStack[last] != session)
        {
            Debug.LogWarning("BuffManager: 나중에 시작한 아이템 사용부터 완료해야 합니다.", this);
            return;
        }

        itemUseStack.RemoveAt(last);
        bool changed = false;
        bool removed = false;
        BuffNotifyScope scope = BuffNotifyScope.Item;

        try
        {
            if (succeeded)
            {
                for (int i = 0; i < session.candidates.Count; i++)
                {
                    BuffUseCandidate candidate = session.candidates[i];
                    ActiveBuff buff = candidate.buff;
                    if (buff.StorageOwner != storage || buff.IsExpired ||
                        buff.RegistrationVersion != candidate.registrationVersion)
                        continue;

                    if (!candidate.consumeOnComplete && !session.appliedBuffs.Contains(buff))
                        continue;

                    buff.ConsumeUse();
                    changed = true;
                    if (!buff.IsExpired)
                        continue;

                    BuffNotifyScope buffScope = GetNotifyScope(buff.target);
                    scope = removed ? MergeNotifyScope(scope, buffScope) : buffScope;
                    removed = true;
                    storage.RemoveBuff(buff, BuffRemovalReason.Consumed);
                }
            }
        }
        finally
        {
            session.Clear();
            itemUsePool.Push(session);
        }

        if (removed)
            NotifyBuffChanged(scope);
        else if (changed && buffUIManager != null)
            buffUIManager.RefreshRuntimeValues();
    }

    public void RegisterBuff(BuffEffect effect, ItemEffectContext itemContext)
    {
        if (effect == null || itemContext == null)
            return;

        if (!effect.HasRuntimePayload())
            return;

        BuffRegisterContext context = new BuffRegisterContext(itemContext, this);
        BuffInfo finalInfo = itemContext.GetCurrentStat(effect, effect.buffInfo);

        if (finalInfo == null)
            return;

        finalInfo.Clamp();

        List<BuffTargetHandle> targets = effect.ResolveTargets(context);

        if (targets == null || targets.Count <= 0)
            return;

        BuffNotifyScope notifyScope = BuffNotifyScope.Item;
        bool registered = false;

        for (int i = 0; i < targets.Count; i++)
        {
            BuffTargetHandle target = targets[i];

            if (target == null)
                continue;

            ActiveBuff active = storage.RegisterBuff(
                effect.modifiers,
                finalInfo,
                context.sourceItemData,
                context.sourceBag,
                effect,
                target,
                effect.includeSelf,
                effect.showInUI
            );

            TrackBuffCompletion(active, effect, itemContext);

            BuffNotifyScope targetScope = GetNotifyScope(target);
            notifyScope = registered ? MergeNotifyScope(notifyScope, targetScope) : targetScope;
            registered = true;
        }

        if (registered)
            NotifyBuffChanged(notifyScope);
    }
    // 명중 효과용: Resolver를 재탐색하지 않고 맞은 대상에게만 적용한다.
    public bool RegisterBuffForTarget(
        BuffEffect effect,
        ItemEffectContext itemContext,
        IBuffTarget buffTarget
    )
    {
        return RegisterBuffForTargetHandle(effect, itemContext, buffTarget) != null;
    }

    public ActiveBuff RegisterBuffForTargetHandle(
        BuffEffect effect,
        ItemEffectContext itemContext,
        IBuffTarget buffTarget
    )
    {
        if (storage == null || effect == null || itemContext == null ||
            buffTarget == null || buffTarget.BuffTargetObject == null)
            return null;

        if (!effect.HasRuntimePayload())
            return null;

        BuffInfo finalInfo = itemContext.GetCurrentStat(effect, effect.buffInfo);
        if (finalInfo == null)
            return null;

        finalInfo.Clamp();

        ActiveBuff active = storage.RegisterBuff(
            effect.modifiers,
            finalInfo,
            itemContext.sourceItemData,
            itemContext.sourceBag,
            effect,
            BuffTargetHandle.Target(buffTarget),
            effect.includeSelf,
            effect.showInUI
        );

        TrackBuffCompletion(active, effect, itemContext);

        // 같은 공격에 다른 디버프가 있어도 BuffEffect별로 독립적으로 구분한다.
        NotifyBuffChanged(BuffNotifyScope.Target);
        return active;
    }

    // 상태 전용 효과는 Modifier 없이 기존 만료/소비/정화/완료 처리를 공유합니다.
    public ActiveBuff RegisterStatus(ApplyStatusEffect effect, BuffInfo info,
        ItemEffectContext context, BuffTargetHandle target)
    {
        if (storage == null || effect == null || info == null || info.statusDefinition == null ||
            context == null || !context.CanContinue || target == null) return null;
        ActiveBuff active = storage.RegisterBuff(null, info, context.sourceItemData, context.sourceBag,
            effect, target, true, effect.showInUI);
        active.uiDisplayName = info.statusDefinition.displayName;
        active.uiIcon = effect.statusIcon;
        TrackBuffCompletion(active, effect, context);
        NotifyBuffChanged(GetNotifyScope(target));
        return active;
    }

    // 표식 등 이벤트 수신 상태도 기존 ActiveBuff 시간/중첩/횟수 규칙을 사용합니다.
    public ActiveBuff RegisterStatusForTarget(StatusDefinition status, BuffInfo info,
        ItemEffectContext context, IBuffTarget target, UnityEngine.Object registrationKey)
    {
        if (status == null || info == null || context == null || !context.CanContinue ||
            target == null || target.BuffTargetObject == null || registrationKey == null) return null;
        BuffEffect marker;
        if (!statusMarkers.TryGetValue(registrationKey, out marker) || marker == null)
        {
            marker = ScriptableObject.CreateInstance<BuffEffect>();
            marker.name = registrationKey.name + " Status";
            marker.hideFlags = HideFlags.HideAndDontSave;
            marker.includeSelf = true;
            statusMarkers[registrationKey] = marker;
        }
        marker.buffInfo = info.Clone();
        marker.buffInfo.statusDefinition = status;
        return RegisterBuffForTargetHandle(marker, context, target);
    }

    // 오라나 정화가 제거한 버프는 자연 완료로 처리하지 않습니다.
    public bool RemoveBuffHandle(ActiveBuff buff, BuffRemovalReason reason = BuffRemovalReason.Cancelled)
    {
        if (storage == null || buff == null || buff.StorageOwner != storage)
            return false;
        BuffNotifyScope scope = GetNotifyScope(buff.target);
        storage.RemoveBuff(buff, reason);
        NotifyBuffChanged(scope);
        return true;
    }

    public int RemoveBuffHandles(IList<ActiveBuff> buffs, BuffRemovalReason reason = BuffRemovalReason.Cancelled)
    {
        if (storage == null || buffs == null) return 0;
        int removed = 0;
        BuffNotifyScope scope = BuffNotifyScope.Item;
        for (int i = 0; i < buffs.Count; i++)
        {
            ActiveBuff buff = buffs[i];
            if (buff == null || buff.StorageOwner != storage) continue;
            BuffNotifyScope current = GetNotifyScope(buff.target);
            scope = removed > 0 ? MergeNotifyScope(scope, current) : current;
            storage.RemoveBuff(buff, reason);
            removed++;
        }
        if (removed > 0) NotifyBuffChanged(scope);
        return removed;
    }

    public int ConsumeSummonAttack(IBuffTarget target)
    {
        if (storage == null || target == null || target.BuffTargetObject == null)
            return 0;
        BuffQueryContext context = BuffQueryContext.ForTarget(target);
        int consumed = 0;
        bool removed = false;
        ActiveBuff[] candidates = storage.useCountBuffs.ToArray();
        for (int i = 0; i < candidates.Length; i++)
        {
            ActiveBuff buff = candidates[i];
            if (buff == null || buff.StorageOwner != storage || buff.IsExpired ||
                buff.useCountConsumeMode != BuffUseCountConsumeMode.SummonAttack ||
                !buff.MatchesQuery(context)) continue;
            buff.ConsumeUse();
            consumed++;
            if (!buff.IsExpired) continue;
            storage.RemoveBuff(buff, BuffRemovalReason.Consumed);
            removed = true;
        }
        if (removed) NotifyBuffChanged(BuffNotifyScope.Target);
        else if (consumed > 0 && buffUIManager != null) buffUIManager.RefreshRuntimeValues();
        return consumed;
    }

    public int GetStatusStack(StatusDefinition status, BuffQueryContext context)
    {
        if (storage == null || status == null || context == null) return 0;
        if (context.buffTarget != null && context.buffTarget.BuffTargetObject == null) return 0;
        if (context.buffTarget == null && context.itemData == null && context.bag == null) return 0;
        int total = 0;
        for (int i = 0; i < storage.activeBuffs.Count; i++)
        {
            ActiveBuff buff = storage.activeBuffs[i];
            if (buff == null || buff.IsExpired || buff.statusDefinition != status ||
                !buff.MatchesQuery(context)) continue;
            if (context.buffTarget == null && context.itemData == null && !buff.target.MatchesBag(context.bag)) continue;
            total = (int)System.Math.Min(int.MaxValue, (long)total + Mathf.Max(1, buff.stack));
        }
        return total;
    }

    public bool HasStatus(StatusDefinition status, BuffQueryContext context, int minimumStack = 1)
        => GetStatusStack(status, context) >= Mathf.Max(1, minimumStack);

    public void FindStatuses(StatusDefinition status, BuffQueryContext context, List<ActiveBuff> results)
    {
        if (results == null) return;
        results.Clear();
        if (storage == null || status == null || context == null) return;
        if (context.buffTarget != null && context.buffTarget.BuffTargetObject == null) return;
        if (context.buffTarget == null && context.itemData == null && context.bag == null) return;
        for (int i = 0; i < storage.activeBuffs.Count; i++)
        {
            ActiveBuff buff = storage.activeBuffs[i];
            if (buff != null && !buff.IsExpired && buff.statusDefinition == status && buff.MatchesQuery(context) &&
                (context.buffTarget != null || context.itemData != null || buff.target.MatchesBag(context.bag)))
                results.Add(buff);
        }
    }

    public int Cleanse(BuffTargetHandle selection, bool includeModifierBuffs = true)
        => Cleanse(selection, null, includeModifierBuffs);

    public int Cleanse(BuffTargetHandle selection, BuffCleanseFilter filter, bool includeModifierBuffs = true)
    {
        if (storage == null || selection == null) return 0;
        int removed = 0;
        ActiveBuff[] candidates = storage.activeBuffs.ToArray();
        for (int i = 0; i < candidates.Length; i++)
        {
            ActiveBuff buff = candidates[i];
            if (buff == null || buff.StorageOwner != storage || buff.IsExpired ||
                !(filter != null ? filter.Matches(buff) : buff.harmful && buff.dispellable) ||
                !StatusTargetUtility.MatchesSelection(buff.target, selection)) continue;
            if (!includeModifierBuffs && StatusTargetUtility.HasModifiers(buff.modifiers)) continue;
            storage.RemoveBuff(buff, BuffRemovalReason.Cleansed);
            removed++;
        }
        if (removed > 0) NotifyBuffChanged(BuffNotifyScope.All);
        return removed;
    }

    private static void TrackBuffCompletion(ActiveBuff buff, ItemEffectData effect, ItemEffectContext context)
    {
        if (buff == null) return;
        UnityEngine.Object obj = buff.target != null ? buff.target.targetObject : null;
        Component component = obj as Component;
        GameObject targetObject = obj as GameObject;
        Transform target = component != null ? component.transform :
            (targetObject != null ? targetObject.transform : (context.owner != null ? context.owner.transform : null));
        buff.completion.Track(context, effect.endVisualData, target);
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

        ApplyBuffsToStat(result, context, calculationMode, consumeUseCount);
        return result;
    }

    private bool ApplyModifiersAdditive<T>(BuffStatEntry entry, T stat, BuffQueryContext context) where T : class
    {
        bool applied = false;

        for (int i = 0; i < entry.modifiers.Length; i++)
        {
            BuffModifier modifier = entry.modifiers[i];
            if (modifier == null || !modifier.CanApplyTo(stat, context))
                continue;

            modifier.ApplyAdditiveTo(stat, entry.stack, context);
            applied = true;
        }
        return applied;
    }

    public void ApplyBuffsToStat<T>(
        T stat,
        BuffQueryContext context,
        BuffCalculationMode calculationMode = BuffCalculationMode.All,
        bool consumeUseCount = false
    ) where T : class, IGameStat<T>
    {
        if (stat == null || storage == null)
            return;

        List<BuffStatEntry> entries = calculationPool.Count > 0
            ? calculationPool.Pop() : new List<BuffStatEntry>(16);
        try
        {
            // 적용 대상을 한 번만 찾고, 중첩 조회도 각각의 버퍼를 사용한다.
            for (int i = 0; i < storage.activeBuffs.Count; i++)
            {
                ActiveBuff buff = storage.activeBuffs[i];
                if (!CanUseBuff(buff, context, calculationMode))
                    continue;

                entries.Add(new BuffStatEntry
                {
                    buff = buff,
                    modifiers = buff.modifiers,
                    stack = Mathf.Max(1, buff.stack),
                    registrationVersion = buff.RegistrationVersion
                });
            }

            // 모든 더하기 -> 모든 곱하기. 마지막 1회도 두 단계 모두 적용한다.
            for (int i = 0; i < entries.Count; i++)
            {
                BuffStatEntry entry = entries[i];
                entry.applied = ApplyModifiersAdditive(entry, stat, context);
                entries[i] = entry;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                BuffStatEntry entry = entries[i];
                entry.applied |= ApplyModifiersMultiplicative(entry, stat, context);
                entries[i] = entry;
            }

            stat.Clamp();

            // 변경 알림에 따른 재계산은 실제 사용이 아니므로 차감하지 않는다.
            if (notificationDepth > 0 || !consumeUseCount)
                return;

            if (itemUseStack.Count > 0)
            {
                BuffItemUseSession session = itemUseStack[itemUseStack.Count - 1];
                for (int i = 0; i < entries.Count; i++)
                {
                    BuffStatEntry entry = entries[i];
                    if (entry.applied && entry.buff.useLimitType == BuffUseLimitType.UseCount &&
                        entry.buff.useCountConsumeMode == BuffUseCountConsumeMode.WhenBuffApplied &&
                        entry.buff.RegistrationVersion == entry.registrationVersion)
                    {
                        session.appliedBuffs.Add(entry.buff);
                    }
                }
            }
            else if (consumeUseCount)
            {
                ConsumeAppliedBuffs(entries);
            }
        }
        finally
        {
            entries.Clear();
            calculationPool.Push(entries);
        }
    }

    private void ConsumeAppliedBuffs(List<BuffStatEntry> entries)
    {
        bool changed = false;
        bool removed = false;
        BuffNotifyScope scope = BuffNotifyScope.Item;
        for (int i = 0; i < entries.Count; i++)
        {
            BuffStatEntry entry = entries[i];
            ActiveBuff buff = entry.buff;
            if (!entry.applied || buff.StorageOwner != storage || buff.IsExpired ||
                buff.RegistrationVersion != entry.registrationVersion ||
                buff.useLimitType != BuffUseLimitType.UseCount ||
                buff.useCountConsumeMode != BuffUseCountConsumeMode.WhenBuffApplied)
                continue;

            buff.ConsumeUse();
            changed = true;
            if (!buff.IsExpired)
                continue;

            BuffNotifyScope buffScope = GetNotifyScope(buff.target);
            scope = removed ? MergeNotifyScope(scope, buffScope) : buffScope;
            removed = true;
            storage.RemoveBuff(buff, BuffRemovalReason.Consumed);
        }

        if (removed)
            NotifyBuffChanged(scope);
        else if (changed && buffUIManager != null)
            buffUIManager.RefreshRuntimeValues();
    }

    public void ApplyBuffsToStatForTarget<T>(
        T stat,
        IBuffTarget target,
        BuffCalculationMode calculationMode = BuffCalculationMode.All,
        bool consumeUseCount = false
    ) where T : class, IGameStat<T>
    {
        ApplyBuffsToStat(
            stat,
            BuffQueryContext.ForTarget(target),
            calculationMode,
            consumeUseCount
        );
    }
    private bool ApplyModifiersMultiplicative<T>(BuffStatEntry entry, T stat, BuffQueryContext context) where T : class
    {
        bool applied = false;

        for (int i = 0; i < entry.modifiers.Length; i++)
        {
            BuffModifier modifier = entry.modifiers[i];
            if (modifier == null || !modifier.CanApplyTo(stat, context))
                continue;

            modifier.ApplyMultiplicativeTo(stat, entry.stack, context);
            applied = true;
        }
        return applied;
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

        // 횟수제 아이템 버프는 생성되는 공격에 저장해야 마지막 1회도 유지된다.
        // SnapshotOnly/DynamicOnly를 함께 쓰는 공격에서 중복 적용도 방지한다.
        bool countedItemBuff = buff.useLimitType == BuffUseLimitType.UseCount &&
            buff.target != null && buff.target.kind != BuffTargetKind.Target &&
            buff.target.kind != BuffTargetKind.Group;
        bool snapshot = countedItemBuff || buff.applyTiming == BuffApplyTiming.Snapshot;

        if (calculationMode == BuffCalculationMode.SnapshotOnly)
            return snapshot;

        if (calculationMode == BuffCalculationMode.DynamicOnly)
            return !snapshot;

        return true;
    }

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

    #region Buff Target Register

    public void RegisterBuffTarget(IBuffTarget target)
    {
        if (storage == null || target == null)
            return;

        storage.RegisterTarget(target);
        RefreshTargetStat(target);
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
        if (!storage.registeredTargets.Contains(target))
            RefreshTargetStat(target);
        NotifyBuffChanged(BuffNotifyScope.Target);
    }

    public void ClearNormalBuffsForTarget(IBuffTarget target)
    {
        if (storage == null || target == null)
            return;

        storage.RemoveNormalBuffsForTarget(target);
        if (!storage.registeredTargets.Contains(target))
            RefreshTargetStat(target);
        NotifyBuffChanged(BuffNotifyScope.Target);
    }

    public void ClearInfiniteBuffsForTarget(IBuffTarget target)
    {
        if (storage == null || target == null)
            return;

        storage.RemoveInfiniteBuffsForTarget(target);
        if (!storage.registeredTargets.Contains(target))
            RefreshTargetStat(target);
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

    // 이펙트 조건용 조회: 횟수를 소비하거나 버프 변경 알림을 발생시키지 않는다.
    public bool HasActiveBuff(
        BuffEffect effect,
        BuffQueryContext context,
        int minimumStack = 1
    )
    {
        return HasActiveBuffInternal(effect, context, minimumStack, false);
    }

    // 가방 조건은 그 가방 또는 전체 가방에 등록된 버프만 확인한다.
    // AllItems나 개별 아이템/시리즈 버프를 가방 버프로 간주하지 않는다.
    public bool HasActiveBuffForBag(
        BuffEffect effect,
        EquipmentBag bag,
        int minimumStack = 1
    )
    {
        if (bag == null)
            return false;

        return HasActiveBuffInternal(
            effect, BuffQueryContext.ForBag(bag), minimumStack, true
        );
    }

    private bool HasActiveBuffInternal(
        BuffEffect effect,
        BuffQueryContext context,
        int minimumStack,
        bool bagOnly
    )
    {
        if (storage == null || effect == null || context == null)
            return false;

        if (context.buffTarget != null)
        {
            if (context.buffTarget.BuffTargetObject == null)
                return false;
        }
        else if (context.itemData == null && context.bag == null)
        {
            return false;
        }

        int requiredStack = Mathf.Max(1, minimumStack);
        for (int i = 0; i < storage.activeBuffs.Count; i++)
        {
            ActiveBuff buff = storage.activeBuffs[i];
            if (buff == null || buff.IsExpired || buff.target == null)
                continue;

            if (buff.sourceEffectData != effect || buff.stack < requiredStack)
                continue;

            if (bagOnly && !buff.target.MatchesBag(context.bag))
                continue;

            // includeSelf, 가방, 시리즈, 전체 아이템, 대상 그룹 규칙을 그대로 따른다.
            if (buff.MatchesQuery(context))
                return true;
        }

        return false;
    }

    public List<ActiveBuff> GetAllActiveBuffs()
    {
        return query != null ? query.GetAllActiveBuffs() : new List<ActiveBuff>();
    }

    public List<ActiveBuff> GetAllVisibleBuffs()
    {
        return AppendEffectUI(query != null ? query.GetAllVisibleBuffs() : new List<ActiveBuff>());
    }

    public List<ActiveBuff> GetNormalActiveBuffs()
    {
        return query != null ? query.GetNormalActiveBuffs() : new List<ActiveBuff>();
    }

    public List<ActiveBuff> GetNormalVisibleBuffs()
    {
        return AppendEffectUI(query != null ? query.GetNormalVisibleBuffs() : new List<ActiveBuff>());
    }

    public List<ActiveBuff> GetInfiniteActiveBuffs()
    {
        return query != null ? query.GetInfiniteActiveBuffs() : new List<ActiveBuff>();
    }

    public List<ActiveBuff> GetInfiniteVisibleBuffs()
    {
        return query != null ? query.GetInfiniteVisibleBuffs() : new List<ActiveBuff>();
    }

    public List<ActiveBuff> GetBagBuffsAsList(EquipmentBag bag)
    {
        return query != null ? query.GetBagBuffsAsList(bag) : new List<ActiveBuff>();
    }

    public List<ActiveBuff> GetVisibleBagBuffsAsList(EquipmentBag bag)
    {
        return AppendEffectUI(query != null ? query.GetBagBuffsAsList(bag, true) : new List<ActiveBuff>(),
            buff => bag != null && buff.sourceBag == bag);
    }

    public List<ActiveBuff> GetItemBuffsAsList(ItemData itemData)
    {
        return query != null ? query.GetItemBuffsAsList(itemData) : new List<ActiveBuff>();
    }

    public List<ActiveBuff> GetVisibleItemBuffsAsList(ItemData itemData)
    {
        return AppendEffectUI(query != null ? query.GetItemBuffsAsList(itemData, true) : new List<ActiveBuff>(),
            buff => itemData != null && buff.sourceItemData == itemData);
    }

    public List<ActiveBuff> GetItemSeriesBuffsAsList(ItemSeries series)
    {
        return query != null ? query.GetItemSeriesBuffsAsList(series) : new List<ActiveBuff>();
    }

    public List<ActiveBuff> GetVisibleItemSeriesBuffsAsList(ItemSeries series)
    {
        return AppendEffectUI(query != null ? query.GetItemSeriesBuffsAsList(series, true) : new List<ActiveBuff>(),
            buff => series != ItemSeries.None && buff.sourceItemData != null && buff.sourceItemData.series == series);
    }

    public List<ActiveBuff> GetTargetBuffsAsList(IBuffTarget target)
    {
        return query != null ? query.GetTargetBuffsAsList(target) : new List<ActiveBuff>();
    }

    public List<ActiveBuff> GetVisibleTargetBuffsAsList(IBuffTarget target)
    {
        return AppendEffectUI(query != null ? query.GetTargetBuffsAsList(target, true) : new List<ActiveBuff>(),
            buff => buff.target != null && buff.target.MatchesTarget(target));
    }

    public List<ActiveBuff> GetTargetGroupBuffsAsList(string targetGroup)
    {
        return query != null ? query.GetTargetGroupBuffsAsList(targetGroup) : new List<ActiveBuff>();
    }

    public List<ActiveBuff> GetVisibleTargetGroupBuffsAsList(string targetGroup)
    {
        BuffTargetHandle group = BuffTargetHandle.Group(targetGroup);
        return AppendEffectUI(query != null ? query.GetTargetGroupBuffsAsList(targetGroup, true) : new List<ActiveBuff>(),
            buff => group != null && buff.target != null && group.MatchesTarget(buff.target.GetCachedTarget()));
    }

    #endregion

    // UI-only entries stay outside gameplay buff queries, stacking, cleansing and ticking.
    public EffectBuffUIHandle RegisterEffectUI(ItemEffectData effect, ItemEffectContext context,
        IBuffTarget target, float duration, string displayName = null, Sprite icon = null)
    {
        if (effect == null || context == null || !context.CanContinue || storage == null ||
            duration <= 0f || float.IsNaN(duration) || float.IsInfinity(duration)) return null;
        BuffTargetHandle targetHandle = target != null ? BuffTargetHandle.Target(target) :
            (context.sourceItemData != null ? BuffTargetHandle.Item(context.sourceItemData) : null);
        if (target != null && targetHandle == null) return null;
        ActiveBuff record = new ActiveBuff(null,
            new BuffInfo { useLimitType = BuffUseLimitType.Time, duration = duration },
            context.sourceItemData, context.sourceBag, effect, targetHandle, true, true);
        // Preserve sub-frame durations instead of BuffInfo's gameplay minimum.
        record.maxTime = record.remainTime = duration;
        record.uiDisplayName = displayName;
        record.uiIcon = icon;
        record.uiManagedLifetime = true;
        EffectBuffUIHandle entry = new EffectBuffUIHandle(this, record, context);
        effectUIEntries.Add(entry);
        RefreshUI();
        return entry;
    }

    internal void RemoveEffectUI(EffectBuffUIHandle entry)
    {
        if (effectUIEntries.Remove(entry)) RefreshUI();
    }

    private bool PruneEffectUI()
    {
        bool changed = false;
        for (int i = effectUIEntries.Count - 1; i >= 0; i--)
        {
            EffectBuffUIHandle entry = effectUIEntries[i];
            if (entry.CanDisplay) continue;
            entry.Invalidate();
            effectUIEntries.RemoveAt(i);
            changed = true;
        }
        return changed;
    }

    private List<ActiveBuff> AppendEffectUI(List<ActiveBuff> result, Predicate<ActiveBuff> predicate = null)
    {
        PruneEffectUI();
        for (int i = 0; i < effectUIEntries.Count; i++)
        {
            ActiveBuff record = effectUIEntries[i].DisplayBuff;
            if (predicate == null || predicate(record)) result.Add(record);
        }
        return result;
    }

    public void ClearNormalBuffs()
    {
        if (storage == null)
            return;

        storage.ClearNormalBuffs();
        NotifyBuffChanged(BuffNotifyScope.All);
    }

    public void ClearInfiniteBuffs()
    {
        if (storage == null)
            return;

        storage.ClearInfiniteBuffs();
        NotifyBuffChanged(BuffNotifyScope.All);
    }

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
        notificationDepth++;
        try
        {
            if (scope == BuffNotifyScope.All || scope == BuffNotifyScope.Target)
                RefreshAllRegisteredTargetStats();

            if (scope == BuffNotifyScope.All || scope == BuffNotifyScope.Item || scope == BuffNotifyScope.DynamicOnly)
                NotifyDynamicBuffReceivers();

            RefreshUI();
            RefreshDebugInspector();
        }
        finally
        {
            notificationDepth--;
        }
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

            RefreshTargetStat(target);
        }
    }

    private void RefreshTargetStat(IBuffTarget target)
    {
        notificationDepth++;
        try { target.RefreshBuffedStat(); }
        finally { notificationDepth--; }
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
        debugNormalActiveBuffs.Clear();
        debugInfiniteActiveBuffs.Clear();
        debugBuffGroups.Clear();

        for (int i = 0; i < storage.activeBuffs.Count; i++)
        {
            ActiveBuff buff = storage.activeBuffs[i];

            if (buff == null || buff.IsExpired)
                continue;

            debugAllActiveBuffs.Add(buff);

            if (buff.IsInfinite)
                debugInfiniteActiveBuffs.Add(buff);
            else
                debugNormalActiveBuffs.Add(buff);

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
