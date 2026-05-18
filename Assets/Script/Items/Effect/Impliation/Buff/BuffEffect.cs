using System;
using System.Collections.Generic;
using UnityEngine;

public enum BuffTarget
{
    Self,
    SameBag,
    All,
    Item
}

[CreateAssetMenu(fileName = "BuffEffect", menuName = "Game/Item Effect/Buff Items")]
public class BuffEffect : ItemEffectData
{
    [Header("Info")]
    public BuffTarget targetScope = BuffTarget.SameBag;
    public bool includeSelf = false;
    public BuffInfo buffInfo;


    [Header("Buff")]
    public BuffStat bonus = new BuffStat();
    public override void Execute(ItemEffectContext context)
    {
        if (context == null)
            return;

        if (context.buffManager == null)
        {
            Debug.LogWarning("ItemEffectContext에 BuffManager가 없습니다.");
            return;
        }

        context.buffManager.RegisterBuff(this, context);
    }
}