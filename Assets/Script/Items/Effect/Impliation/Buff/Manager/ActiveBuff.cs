using System;
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
    public BuffApplyTiming applyTiming = BuffApplyTiming.Snapshot;
    public BuffUseLimitType useLimitType = BuffUseLimitType.Time;
    public BuffStackMode stackMode = BuffStackMode.Refresh;

    [Min(1)] public int stack = 1;
    [Min(1)] public int maxStack = 1;

    [Min(0.01f)] public float maxTime = 1f;
    [Min(0f)] public float remainTime = 1f;

    [Min(1)] public int maxUseCount = 1;
    [Min(0)] public int remainUseCount = 1;

    [NonSerialized] public BuffModifier[] modifiers;

    public bool IsExpired
    {
        get
        {
            if (useLimitType == BuffUseLimitType.UseCount)
                return remainUseCount <= 0;

            return remainTime <= 0f;
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

        info.Clamp();

        applyTiming = info.applyTiming;
        useLimitType = info.useLimitType;
        stackMode = info.stackMode;
        maxStack = Mathf.Max(1, info.maxStack);

        if (stackMode == BuffStackMode.Refresh)
            maxStack = 1;

        maxTime = Mathf.Max(0.01f, info.duration);
        remainTime = maxTime;

        maxUseCount = Mathf.Max(1, info.maxUseCount);
        remainUseCount = maxUseCount;

        if (stack <= 0)
            stack = 1;
    }

    public void RegisterAgain(BuffInfo info)
    {
        ApplyInfo(info);

        if (stackMode == BuffStackMode.Stack)
            stack = Mathf.Min(stack + 1, maxStack);
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
    }

    public float GetTimeRate()
    {
        if (useLimitType == BuffUseLimitType.UseCount)
            return maxUseCount <= 0 ? 0f : (float)remainUseCount / maxUseCount;

        return maxTime <= 0f ? 0f : remainTime / maxTime;
    }

    public bool MatchesQuery(BuffQueryContext query)
    {
        if (target == null)
            return false;

        if (!target.Matches(query))
            return false;

        if (!includeSelf && query != null && query.itemData != null && sourceItemData == query.itemData)
            return false;

        return true;
    }

    public bool IsSameBuff(ItemData sourceItemData, EquipmentBag sourceBag, ItemEffectData sourceEffectData, BuffTargetHandle target)
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
