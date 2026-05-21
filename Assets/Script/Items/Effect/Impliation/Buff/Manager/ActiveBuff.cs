using System;
using UnityEngine;

[Serializable]
public class ActiveBuff
{
    [Header("Buff")]
    public BuffStat buffStat;

    [Header("Source Info")]
    public ItemData sourceItemData;
    public EquipmentBag sourceBag;
    public ItemEffectData sourceEffectData;

    [Header("Option")]
    public bool includeSelf;
    public bool showInUI = true;

    [Header("Stack")]
    public BuffStackMode stackMode;
    public int stack = 1;
    public int maxStack = 1;

    [Header("Time")]
    public float maxTime;
    public float remainTime;

    public bool IsExpired => remainTime <= 0f;

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

        stackMode = buffInfo != null ? buffInfo.stackMode : BuffStackMode.Refresh;
        maxStack = buffInfo != null ? Mathf.Max(1, buffInfo.maxStack) : 1;

        if (stackMode == BuffStackMode.Refresh)
            maxStack = 1;

        stack = 1;

        maxTime = buffInfo != null ? Mathf.Max(0.01f, buffInfo.duration) : 0.01f;
        remainTime = maxTime;
    }

    public void Tick(float deltaTime)
    {
        remainTime -= deltaTime;

        if (remainTime < 0f)
            remainTime = 0f;
    }

    public void RefreshTime()
    {
        remainTime = maxTime;
    }

    public void ApplyRegisterAgain(BuffInfo buffInfo)
    {
        if (buffInfo != null)
        {
            maxTime = Mathf.Max(0.01f, buffInfo.duration);
            maxStack = Mathf.Max(1, buffInfo.maxStack);
            stackMode = buffInfo.stackMode;

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
        {
            stack = 1;
        }

        RefreshTime();
    }

    public float GetTimeRate()
    {
        if (maxTime <= 0f)
            return 0f;

        return remainTime / maxTime;
    }

    public string GetSourceItemName()
    {
        if (sourceItemData == null)
            return "Unknown Item";

        return sourceItemData.dataName;
    }

    public string GetSourceBagName()
    {
        if (sourceBag == null)
            return "Unknown Bag";

        return sourceBag.name;
    }

    public bool IsSameBuff(
        ItemData itemData,
        EquipmentBag bag,
        ItemEffectData effectData
    )
    {
        return sourceItemData == itemData &&
               sourceBag == bag &&
               sourceEffectData == effectData;
    }
}