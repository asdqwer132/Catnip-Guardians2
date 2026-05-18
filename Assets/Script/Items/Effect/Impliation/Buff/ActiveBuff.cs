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

    [Header("Time")]
    public float maxTime;
    public float remainTime;

    public bool IsExpired => remainTime <= 0f;

    public ActiveBuff(
        BuffStat buffStat,
        float duration,
        ItemData sourceItemData,
        EquipmentBag sourceBag,
        ItemEffectData sourceEffectData
    )
    {
        this.buffStat = buffStat;

        this.sourceItemData = sourceItemData;
        this.sourceBag = sourceBag;
        this.sourceEffectData = sourceEffectData;

        maxTime = duration;
        remainTime = duration;
    }

    public void Tick(float deltaTime)
    {
        remainTime -= deltaTime;

        if (remainTime < 0f)
            remainTime = 0f;
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

        return sourceItemData.itemName;
    }

    public string GetSourceBagName()
    {
        if (sourceBag == null)
            return "Unknown Bag";

        return sourceBag.name;
    }
}