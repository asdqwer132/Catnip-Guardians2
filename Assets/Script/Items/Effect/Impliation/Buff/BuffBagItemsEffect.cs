using UnityEngine;

[CreateAssetMenu(fileName = "BuffBagItemsEffect", menuName = "Game/Item Effect/Buff Bag Items")]
public class BuffBagItemsEffect : ItemEffectData
{
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

        if (context.bagItems == null)
            return;

        if (durationType == RuntimeBuffDurationType.NextItemUse)
        {
            ApplyNextItemUseBuff(context);
        }
        else
        {
            ApplyBuffToAllBagItems(context);
        }

        string itemName = "Unknown Item";

        if (context.itemData != null)
            itemName = context.itemData.itemName;

        Debug.Log(
            itemName +
            " 버프 적용 / 타입: " +
            durationType +
            " / 중첩 허용: " +
            allowStack
        );
    }

    private void ApplyNextItemUseBuff(ItemEffectContext context)
    {
        int targetCount = Mathf.Max(1, nextItemUseTargetCount);

        int startIndex = GetStartIndex(context);
        int appliedCount = 0;
        int totalCount = context.bagItems.Count;

        if (totalCount <= 0)
            return;

        for (int i = 0; i < totalCount; i++)
        {
            int index = (startIndex + i) % totalCount;

            InventoryItem item = context.bagItems[index];

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
    }

    private void ApplyBuffToAllBagItems(ItemEffectContext context)
    {
        for (int i = 0; i < context.bagItems.Count; i++)
        {
            InventoryItem item = context.bagItems[i];

            if (!CanBuffItem(item, context))
                continue;

            ApplyBuff(
                item,
                context,
                0
            );
        }
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

    private int GetStartIndex(ItemEffectContext context)
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

        if (!includeSelf && item == context.inventoryItem)
            return false;

        return true;
    }
}