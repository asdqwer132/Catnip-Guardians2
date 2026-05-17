using System;
using System.Collections.Generic;
using UnityEngine;

public enum BuffItemTargetScope
{
    CurrentBag,
    AllBags
}

[CreateAssetMenu(fileName = "BuffBagItemsEffect", menuName = "Game/Item Effect/Buff Bag Items")]
public class BuffBagItemsEffect : ItemEffectData
{
    public static event Action<RuntimeBuffUIInfo> OnBuffActivated;

    [Header("Buff Target Scope")]
    public BuffItemTargetScope targetScope = BuffItemTargetScope.CurrentBag;

    [Header("Buff Target")]
    public bool includeSelf = false;

    [Tooltip("Duration Type이 NextItemUse일 때 몇 개의 다음 아이템에 버프를 줄지")]
    [Min(1)]
    public int nextItemUseTargetCount = 1;

    [Header("Stack")]
    public bool allowStack = false;

    [Header("Buff Duration")]
    public RuntimeBuffDurationType durationType = RuntimeBuffDurationType.NextItemUse;

    [Tooltip("Duration Type이 Seconds일 때만 사용")]
    public float duration = 5f;

    [Header("Buff Stat")]
    public EffectStat bonus = new EffectStat();

    public override void Execute(ItemEffectContext context)
    {
        if (context == null)
            return;

        List<InventoryItem> targetItems = GetTargetItems(context);

        if (targetItems == null)
            return;

        if (targetItems.Count <= 0)
        {
            NotifyBuffUI(
                context,
                targetItems,
                0
            );

            return;
        }

        int appliedCount = 0;

        if (durationType == RuntimeBuffDurationType.NextItemUse)
        {
            appliedCount = ApplyNextItemUseBuff(
                context,
                targetItems
            );
        }
        else
        {
            appliedCount = ApplyBuffToAllTargetItems(
                context,
                targetItems
            );
        }

        NotifyBuffUI(
            context,
            targetItems,
            appliedCount
        );

        string itemName = "Unknown Item";

        if (context.itemData != null)
            itemName = context.itemData.itemName;

        Debug.Log(
            itemName +
            " 버프 적용 / 범위: " +
            targetScope +
            " / 타입: " +
            durationType +
            " / 적용 수: " +
            appliedCount +
            " / 중첩 허용: " +
            allowStack
        );
    }

    private int ApplyNextItemUseBuff(
        ItemEffectContext context,
        List<InventoryItem> targetItems
    )
    {
        int targetCount = Mathf.Max(1, nextItemUseTargetCount);
        int appliedCount = 0;

        for (int i = 0; i < targetItems.Count; i++)
        {
            InventoryItem item = targetItems[i];

            if (!CanBuffItem(item, context))
                continue;

            ApplyBuff(
                item,
                context,
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
        List<InventoryItem> targetItems
    )
    {
        int appliedCount = 0;

        for (int i = 0; i < targetItems.Count; i++)
        {
            InventoryItem item = targetItems[i];

            if (!CanBuffItem(item, context))
                continue;

            ApplyBuff(
                item,
                context,
                0
            );

            appliedCount++;
        }

        return appliedCount;
    }

    private void ApplyBuff(
        InventoryItem item,
        ItemEffectContext context,
        int useCount
    )
    {
        if (item == null)
            return;

        if (allowStack)
        {
            item.AddRuntimeBuff(
                bonus,
                durationType,
                duration,
                context.currentCycleId,
                this,
                useCount
            );
        }
        else
        {
            item.AddOrReplaceRuntimeBuff(
                this,
                bonus,
                durationType,
                duration,
                context.currentCycleId,
                useCount
            );
        }
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
        int appliedCount
    )
    {
        Sprite icon = null;

        if (context != null && context.itemData != null)
            icon = context.itemData.icon;

        int startNumber = GetUIStartNumber();

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
            durationType: durationType,
            scope: uiScope,
            currentBag: uiBag,
            bagItems: targetItems,
            currentCycleId: context != null ? context.currentCycleId : 0,
            startNumber: startNumber
        );

        OnBuffActivated?.Invoke(info);
    }

    private int GetUIStartNumber()
    {
        if (durationType == RuntimeBuffDurationType.Seconds)
        {
            return Mathf.CeilToInt(duration);
        }

        if (durationType == RuntimeBuffDurationType.NextItemUse)
        {
            return Mathf.Max(1, nextItemUseTargetCount);
        }

        return 1;
    }
}