using System;
using UnityEngine;

[Serializable]
public class ActiveBuff
{
    [Header("Buff")]
    public BuffStat buffStat;
    public bool includeSelf;
    public bool showInUI = true;

    [Header("Source Info")]
    public ItemData sourceItemData;
    public EquipmentBag sourceBag;
    public ItemEffectData sourceEffectData;

    [Header("Apply Timing")]
    public BuffApplyTiming applyTiming = BuffApplyTiming.Snapshot;
    public BuffUseLimitType useLimitType = BuffUseLimitType.Time;

    [Header("Use Count")]
    public int maxUseCount = 1;
    public int remainUseCount = 1;

    [Header("Time")]
    public float maxTime;
    public float remainTime;

    [Header("Stack")]
    public BuffStackMode stackMode;
    public int stack = 1;
    public int maxStack = 1;


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
        BuffStat buffStat,
        BuffInfo buffInfo,
        ItemData sourceItemData,
        EquipmentBag sourceBag,
        ItemEffectData sourceEffectData,
        bool includeSelf,
        bool showInUI
    )
    {
        this.buffStat = buffStat;
        this.sourceItemData = sourceItemData;
        this.sourceBag = sourceBag;
        this.sourceEffectData = sourceEffectData;
        this.includeSelf = includeSelf;
        this.showInUI = showInUI;

        stackMode = buffInfo != null
            ? buffInfo.stackMode
            : BuffStackMode.Refresh;

        maxStack = buffInfo != null
            ? Mathf.Max(1, buffInfo.maxStack)
            : 1;

        if (stackMode == BuffStackMode.Refresh)
            maxStack = 1;

        stack = 1;

        maxTime = buffInfo != null
            ? Mathf.Max(0.01f, buffInfo.duration)
            : 0.01f;

        remainTime = maxTime;

        applyTiming = buffInfo != null
            ? buffInfo.applyTiming
            : BuffApplyTiming.Snapshot;

        useLimitType = buffInfo != null
            ? buffInfo.useLimitType
            : BuffUseLimitType.Time;

        maxUseCount = buffInfo != null
            ? Mathf.Max(1, buffInfo.maxUseCount)
            : 1;

        remainUseCount = maxUseCount;
    }

    public void Tick(float deltaTime)
    {
        if (useLimitType != BuffUseLimitType.Time)
            return;

        remainTime -= deltaTime;

        if (remainTime < 0f)
            remainTime = 0f;
    }

    public void RefreshTime()
    {
        remainTime = maxTime;
    }

    public void ConsumeUse()
    {
        if (useLimitType != BuffUseLimitType.UseCount)
            return;

        remainUseCount--;

        if (remainUseCount < 0)
            remainUseCount = 0;
    }

    public void ApplyRegisterAgain(BuffInfo buffInfo)
    {
        if (buffInfo != null)
        {
            maxTime = Mathf.Max(0.01f, buffInfo.duration);
            maxStack = Mathf.Max(1, buffInfo.maxStack);
            stackMode = buffInfo.stackMode;

            applyTiming = buffInfo.applyTiming;
            useLimitType = buffInfo.useLimitType;

            maxUseCount = Mathf.Max(1, buffInfo.maxUseCount);

            if (stackMode == BuffStackMode.Refresh)
                maxStack = 1;
        }

        if (stackMode == BuffStackMode.Stack)
        {
            stack++;

            if (stack > maxStack)
                stack = maxStack;
        }
        else
            stack = 1;       

        RefreshTime();

        if (useLimitType == BuffUseLimitType.UseCount)
            remainUseCount = maxUseCount;
    }

    public float GetTimeRate()
    {
        if (useLimitType == BuffUseLimitType.UseCount)
        {
            if (maxUseCount <= 0)
                return 0f;

            return (float)remainUseCount / maxUseCount;
        }

        if (maxTime <= 0f)
            return 0f;

        return remainTime / maxTime;
    }

    public bool IsSameBuff(ItemData itemData, EquipmentBag bag, ItemEffectData effectData)
    {
        return sourceItemData == itemData && sourceBag == bag && sourceEffectData == effectData;
    }
}