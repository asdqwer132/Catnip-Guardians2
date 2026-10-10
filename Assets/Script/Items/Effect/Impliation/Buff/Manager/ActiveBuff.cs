using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ActiveBuff
{
    [Header("Source")]
    public ItemData sourceItemData;
    public EquipmentBag sourceBag;
    public ItemEffectData sourceEffectData;

    [Header("Target")]
    public BuffTargetHandle target = new BuffTargetHandle();
    public bool includeSelf;
    public bool showInUI = true;

    [Header("Runtime")]
    public StatusDefinition statusDefinition;
    public bool harmful;
    public bool dispellable = true;
    public BuffFlagDefinition[] flags;
    public BuffApplyTiming applyTiming = BuffApplyTiming.Snapshot;
    public BuffUseLimitType useLimitType = BuffUseLimitType.Time;
    public BuffStackMode stackMode = BuffStackMode.Refresh;

    [Min(1)] public int stack = 1;
    [Min(1)] public int maxStack = 1;

    [Min(0.01f)] public float maxTime = 1f;
    [Min(0f)] public float remainTime = 1f;

    [Min(1)] public int maxUseCount = 1;
    [Min(0)] public int remainUseCount = 1;

    public BuffUseCountConsumeMode useCountConsumeMode;

    [NonSerialized] public BuffModifier[] modifiers;
    [NonSerialized] internal bool isDirectStatus;
    [NonSerialized] internal string uiDisplayName;
    [NonSerialized] internal Sprite uiIcon;
    [NonSerialized] internal bool uiManagedLifetime;
    [NonSerialized] internal bool uiEnded;
    [NonSerialized] private HashSet<ItemData> consumeItemSet;
    internal readonly ItemEffectCompletionGroup completion = new ItemEffectCompletionGroup();
    internal BuffStorage StorageOwner { get; set; }
    internal ulong RegistrationVersion { get; private set; }
    public event Action<ActiveBuff, BuffRemovalReason> Removed;

    internal void NotifyRemoved(BuffRemovalReason reason)
    {
        Action<ActiveBuff, BuffRemovalReason> callbacks = Removed;
        Removed = null;
        callbacks?.Invoke(this, reason);
    }

    public bool IsInfinite => useLimitType == BuffUseLimitType.Infinite;

    public bool IsExpired
    {
        get
        {
            if (uiManagedLifetime) return uiEnded;
            if (IsInfinite)
                return false;

            if (useLimitType == BuffUseLimitType.UseCount)
                return remainUseCount <= 0;

            if (useLimitType == BuffUseLimitType.Time)
                return remainTime <= 0f;

            return false;
        }
    }

    public ActiveBuff(
        BuffModifier[] modifiers,
        BuffInfo info,
        ItemData sourceItemData,
        EquipmentBag sourceBag,
        ItemEffectData sourceEffectData,
        BuffTargetHandle target,
        bool includeSelf,
        bool showInUI
    )
    {
        this.modifiers = modifiers;
        this.sourceItemData = sourceItemData;
        this.sourceBag = sourceBag;
        this.sourceEffectData = sourceEffectData;
        this.target = target;
        this.includeSelf = includeSelf;
        this.showInUI = showInUI;

        ApplyInfo(info);
    }

    public void ApplyInfo(BuffInfo info)
    {
        if (info == null)
            info = new BuffInfo();

        statusDefinition = info.statusDefinition;
        BuffEffect effect = sourceEffectData as BuffEffect;
        harmful = (effect != null && effect.harmful) || (statusDefinition != null && statusDefinition.harmful);
        dispellable = (effect == null || effect.dispellable) &&
            (statusDefinition == null || statusDefinition.dispellable);
        // 실행 중 에셋의 배열을 변경해도 이미 등록된 버프의 분류는 유지한다.
        flags = effect != null && effect.flags != null ? (BuffFlagDefinition[])effect.flags.Clone() : null;
        applyTiming = info.applyTiming;
        useLimitType = info.useLimitType;
        stackMode = info.stackMode;
        maxStack = Mathf.Max(1, info.maxStack);

        if (stackMode == BuffStackMode.Refresh)
            maxStack = 1;

        maxTime = EffectStatUtility.Safe(info.duration, 0.01f, float.MaxValue, 1f);
        remainTime = maxTime;

        maxUseCount = Mathf.Max(1, info.maxUseCount);
        remainUseCount = maxUseCount;
        useCountConsumeMode = info.useCountConsumeMode;

        if (consumeItemSet != null)
            consumeItemSet.Clear();

        if (useCountConsumeMode == BuffUseCountConsumeMode.SpecificItemsUsed &&
            info.consumeItems != null)
        {
            if (consumeItemSet == null)
                consumeItemSet = new HashSet<ItemData>();

            for (int i = 0; i < info.consumeItems.Length; i++)
            {
                ItemData item = info.consumeItems[i];
                if (item != null)
                    consumeItemSet.Add(item);
            }
        }

        stack = Mathf.Clamp(stack, 1, maxStack);
        InvalidateRegistration();
    }

    public void RegisterAgain(BuffInfo info)
    {
        BuffUseLimitType previousLimit = useLimitType;
        float previousTime = remainTime;
        int previousUses = remainUseCount;
        bool wasExpired = IsExpired;
        ApplyInfo(info);

        if (!wasExpired && previousLimit == useLimitType && info != null &&
            info.reapplyMode == BuffReapplyMode.AddRemaining)
        {
            if (useLimitType == BuffUseLimitType.Time)
            {
                remainTime = (float)Math.Min(float.MaxValue, (double)Mathf.Max(0f, previousTime) + maxTime);
                maxTime = remainTime;
            }
            else if (useLimitType == BuffUseLimitType.UseCount)
            {
                remainUseCount = (int)Math.Min(int.MaxValue, (long)Mathf.Max(0, previousUses) + maxUseCount);
                maxUseCount = remainUseCount;
            }
        }

        if (stackMode == BuffStackMode.Stack)
            stack = (int)Math.Min((long)stack + 1, maxStack);
        else
            stack = 1;
    }

    public void Tick(float deltaTime)
    {
        if (useLimitType != BuffUseLimitType.Time)
            return;

        remainTime -= deltaTime;

        if (remainTime < 0f)
            remainTime = 0f;
    }

    public void ConsumeUse()
    {
        if (useLimitType != BuffUseLimitType.UseCount)
            return;

        remainUseCount = Mathf.Max(0, remainUseCount - 1);

        if (remainUseCount == 0 && StorageOwner != null)
            StorageOwner.HasExpiredUseCounts = true;
    }

    public bool ShouldConsumeOnItemUse(ItemData usedItem)
    {
        if (usedItem == null || useLimitType != BuffUseLimitType.UseCount || IsExpired)
            return false;

        if (useCountConsumeMode == BuffUseCountConsumeMode.AnyItemUsed)
            return true;

        return useCountConsumeMode == BuffUseCountConsumeMode.SpecificItemsUsed &&
               consumeItemSet != null && consumeItemSet.Contains(usedItem);
    }

    internal void InvalidateRegistration()
    {
        unchecked { RegistrationVersion++; }
    }

    public float GetTimeRate()
    {
        if (IsInfinite)
            return 1f;

        if (useLimitType == BuffUseLimitType.UseCount)
            return maxUseCount <= 0
                ? 0f
                : (float)remainUseCount / maxUseCount;

        return maxTime <= 0f
            ? 0f
            : remainTime / maxTime;
    }

    public string GetUIName()
    {
        if (!string.IsNullOrWhiteSpace(uiDisplayName)) return uiDisplayName;
        if (sourceItemData != null) return sourceItemData.GetDataName();
        return sourceEffectData != null ? sourceEffectData.name : "Buff";
    }

    public Sprite GetUIIcon()
    {
        Sprite icon = uiIcon;
        BuffEffect effect = sourceEffectData as BuffEffect;
        if (icon == null && effect != null) icon = effect.buffIcon;
        return icon != null ? icon : (sourceItemData != null ? sourceItemData.icon : null);
    }

    public bool HasFlag(BuffFlagDefinition flag)
    {
        if (flag == null || flags == null) return false;
        foreach (BuffFlagDefinition candidate in flags)
            if (candidate == flag) return true;
        return false;
    }

    public bool MatchesQuery(BuffQueryContext query)
    {
        if (target == null)
            return false;

        if (!target.Matches(query))
            return false;

        if (!includeSelf &&
            query != null &&
            query.itemData != null &&
            sourceItemData == query.itemData)
        {
            return false;
        }

        return true;
    }

    public bool IsSameBuff(
        ItemData sourceItemData,
        EquipmentBag sourceBag,
        ItemEffectData sourceEffectData,
        BuffTargetHandle target
    )
    {
        if (this.sourceItemData != sourceItemData)
            return false;

        if (this.sourceBag != sourceBag)
            return false;

        if (this.sourceEffectData != sourceEffectData)
            return false;

        if (this.target == null)
            return target == null;

        return this.target.SameTarget(target);
    }
}
