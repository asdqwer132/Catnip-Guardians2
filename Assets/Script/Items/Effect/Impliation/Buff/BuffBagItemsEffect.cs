using System;
using System.Collections.Generic;
using UnityEngine;

public enum BuffItemTargetScope
{
    CurrentBag,
    AllBags
}

[CreateAssetMenu(fileName = "BuffBagItemsEffect", menuName = "Game/Item Effect/Buff Bag Items")]
public class BuffBagItemsEffect : ItemEffectData, IItemEffectStatProvider
{
    public static event Action<RuntimeBuffUIInfo> OnBuffActivated;

    [Header("Buff Target Scope")]
    public BuffItemTargetScope targetScope = BuffItemTargetScope.CurrentBag;

    [Header("Buff Target")]
    public bool includeSelf = false;

    [Header("Buff Info")]
    public BuffInfoStat buffInfo = new BuffInfoStat();

    [Header("Buff Stat")]
    public BuffStat bonus = new BuffStat();

    public EffectStat GetBaseEffectStat()
    {
        return bonus;
    }

    public override void Execute(ItemEffectContext context)
    {
        if (context == null)
            return;

        List<InventoryItem> targetItems = GetTargetItems(context);

        if (targetItems == null)
            targetItems = new List<InventoryItem>();

        BuffStat finalBuffStat = GetFinalBuffStat(context);
        BuffInfoStat finalBuffInfo = GetFinalBuffInfo(finalBuffStat);

        int appliedCount = 0;

        if (finalBuffInfo.durationType == RuntimeBuffDurationType.NextItemUse)
        {
            appliedCount = ApplyNextItemUseBuff(
                context,
                targetItems,
                finalBuffStat,
                finalBuffInfo
            );
        }
        else
        {
            appliedCount = ApplyBuffToAllTargetItems(
                context,
                targetItems,
                finalBuffStat,
                finalBuffInfo
            );
        }
        if (appliedCount > 0)
        {
            NotifyBuffUI(
                context,
                targetItems,
                appliedCount,
                finalBuffInfo
            );
        }

        string itemName = "Unknown Item";

        if (context.itemData != null)
            itemName = context.itemData.itemName;

        Debug.Log(
            itemName +
            " 버프 적용 / 범위: " +
            targetScope +
            " / 타입: " +
            finalBuffInfo.durationType +
            " / 적용 수: " +
            appliedCount +
            " / 중첩 허용: " +
            finalBuffInfo.GetFinalAllowStack()
        );
    }

    private int ApplyNextItemUseBuff(
        ItemEffectContext context,
        List<InventoryItem> targetItems,
        BuffStat finalBuffStat,
        BuffInfoStat finalBuffInfo
    )
    {
        if (targetItems == null || finalBuffStat == null || finalBuffInfo == null)
            return 0;

        int targetCount = finalBuffInfo.GetFinalNextItemUseTargetCount();
        int appliedCount = 0;

        for (int i = 0; i < targetItems.Count; i++)
        {
            InventoryItem item = targetItems[i];

            if (!CanBuffItem(item, context))
                continue;

            ApplyBuff(
                item,
                context,
                finalBuffStat,
                finalBuffInfo,
                1
            );

            appliedCount++;

            if (appliedCount >= targetCount)
                break;
        }

        return appliedCount;
    }

    private int ApplyBuffToAllTargetItems(
        ItemEffectContext context,
        List<InventoryItem> targetItems,
        BuffStat finalBuffStat,
        BuffInfoStat finalBuffInfo
    )
    {
        if (targetItems == null || finalBuffStat == null || finalBuffInfo == null)
            return 0;

        int appliedCount = 0;

        for (int i = 0; i < targetItems.Count; i++)
        {
            InventoryItem item = targetItems[i];

            if (!CanBuffItem(item, context))
                continue;

            ApplyBuff(
                item,
                context,
                finalBuffStat,
                finalBuffInfo,
                0
            );

            appliedCount++;
        }

        return appliedCount;
    }

    private void ApplyBuff(
        InventoryItem item,
        ItemEffectContext context,
        BuffStat finalBuffStat,
        BuffInfoStat finalBuffInfo,
        int useCount
    )
    {
        if (item == null || finalBuffStat == null || finalBuffInfo == null)
            return;

        BuffStat buffToApply = finalBuffStat.CloneBuff();

        if (buffToApply == null)
            return;

        if (finalBuffInfo.GetFinalAllowStack())
        {
            item.AddRuntimeBuff(
                buffToApply,
                finalBuffInfo.durationType,
                finalBuffInfo.GetFinalDuration(),
                context.currentCycleId,
                this,
                useCount
            );
        }
        else
        {
            item.AddOrReplaceRuntimeBuff(
                this,
                buffToApply,
                finalBuffInfo.durationType,
                finalBuffInfo.GetFinalDuration(),
                context.currentCycleId,
                useCount
            );
        }
    }

    private BuffStat GetFinalBuffStat(ItemEffectContext context)
    {
        if (context != null && context.effectStat is BuffStat contextBuff)
            return contextBuff.CloneBuff();

        if (bonus != null)
            return bonus.CloneBuff();

        return new BuffStat();
    }

    private BuffInfoStat GetFinalBuffInfo(BuffStat finalBuffStat)
    {
        BuffInfoStat result;

        if (buffInfo != null)
            result = buffInfo.Clone();
        else
            result = new BuffInfoStat();

        if (finalBuffStat != null)
            finalBuffStat.ApplyBuffInfoBuffTo(result);

        return result;
    }

    private List<InventoryItem> GetTargetItems(ItemEffectContext context)
    {
        if (targetScope == BuffItemTargetScope.CurrentBag)
            return GetCurrentBagTargetItems(context);

        return GetAllBagsTargetItems(context);
    }

    private List<InventoryItem> GetCurrentBagTargetItems(ItemEffectContext context)
    {
        List<InventoryItem> result = new List<InventoryItem>();

        if (context == null)
            return result;

        if (context.bagItems == null)
            return result;

        int totalCount = context.bagItems.Count;

        if (totalCount <= 0)
            return result;

        int startIndex = GetStartIndexInCurrentBag(context);

        for (int i = 0; i < totalCount; i++)
        {
            int index = (startIndex + i) % totalCount;
            result.Add(context.bagItems[index]);
        }

        return result;
    }

    private List<InventoryItem> GetAllBagsTargetItems(ItemEffectContext context)
    {
        List<InventoryItem> result = new List<InventoryItem>();

        if (EquipmentBagManager.instance == null)
            return result;

        EquipmentBag[] bags = EquipmentBagManager.instance.bags;

        if (bags == null || bags.Length <= 0)
            return result;

        List<InventoryItem> allItems = new List<InventoryItem>();

        for (int i = 0; i < bags.Length; i++)
        {
            EquipmentBag bag = bags[i];

            if (bag == null)
                continue;

            if (bag.equippedItems == null)
                continue;

            for (int j = 0; j < bag.equippedItems.Count; j++)
            {
                allItems.Add(bag.equippedItems[j]);
            }
        }

        if (allItems.Count <= 0)
            return result;

        int startIndex = 0;

        if (context != null && context.inventoryItem != null)
        {
            int selfIndex = allItems.IndexOf(context.inventoryItem);

            if (selfIndex >= 0)
            {
                if (includeSelf)
                    startIndex = selfIndex;
                else
                    startIndex = selfIndex + 1;
            }
        }

        for (int i = 0; i < allItems.Count; i++)
        {
            int index = (startIndex + i) % allItems.Count;
            result.Add(allItems[index]);
        }

        return result;
    }

    private int GetStartIndexInCurrentBag(ItemEffectContext context)
    {
        if (context == null || context.bagItems == null)
            return 0;

        int selfIndex = -1;

        if (context.inventoryItem != null)
            selfIndex = context.bagItems.IndexOf(context.inventoryItem);

        if (selfIndex == -1)
            return 0;

        if (includeSelf)
            return selfIndex;

        return selfIndex + 1;
    }

    private bool CanBuffItem(
        InventoryItem item,
        ItemEffectContext context
    )
    {
        if (item == null || item.itemData == null)
            return false;

        if (!includeSelf && context != null && item == context.inventoryItem)
            return false;

        return true;
    }

    private void NotifyBuffUI(
        ItemEffectContext context,
        List<InventoryItem> targetItems,
        int appliedCount,
        BuffInfoStat finalBuffInfo
    )
    {
        if (finalBuffInfo == null)
            return;

        Sprite icon = null;

        if (context != null && context.itemData != null)
            icon = context.itemData.icon;

        RuntimeBuffUIScope uiScope = RuntimeBuffUIScope.CurrentBag;
        EquipmentBag uiBag = null;

        if (targetScope == BuffItemTargetScope.AllBags)
        {
            uiScope = RuntimeBuffUIScope.AllBags;
            uiBag = null;
        }
        else
        {
            uiScope = RuntimeBuffUIScope.CurrentBag;

            if (context != null)
                uiBag = context.currentBag;
        }

        RuntimeBuffUIInfo info = new RuntimeBuffUIInfo(
            source: this,
            icon: icon,
            durationType: finalBuffInfo.durationType,
            scope: uiScope,
            currentBag: uiBag,
            bagItems: targetItems,
            currentCycleId: context != null ? context.currentCycleId : 0,
            startNumber: finalBuffInfo.GetUIStartNumber()
        );

        OnBuffActivated?.Invoke(info);
    }
}