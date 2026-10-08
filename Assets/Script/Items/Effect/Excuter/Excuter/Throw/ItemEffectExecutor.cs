using UnityEngine;

public class ItemEffectExecutor : MonoBehaviour
{
    [Header("Managers")]
    public BuffManager buffManager;

    public void JustExcuteItem(ItemData itemData)
    {
        ExecuteItemEffect(itemData, Vector3.zero, Vector3.zero, Vector3.zero, gameObject, null, 0);
    }

    public void ExecuteItemEffect(ItemData itemData, Vector3 usePosition, Vector3 targetPosition,
        Vector3 direction, GameObject owner, EquipmentBag sourceBag, int currentCycleId)
    {
        ExecuteItem(itemData, usePosition, targetPosition, direction, owner, sourceBag,
            buffManager != null ? buffManager : BuffManager.instance);
    }

    // 아이템 반복 사용과 소환수가 공통으로 호출한다. 인벤토리/가방 슬롯을 소비하지 않는다.
    public static void ExecuteItem(ItemData itemData, Vector3 usePosition, Vector3 targetPosition,
        Vector3 direction, GameObject owner, EquipmentBag sourceBag, BuffManager manager,
        ItemEffectContext parent = null, bool triggerSpecialItems = true, bool isThrownItem = false,
        bool consumeUseBuffs = true)
    {
        // 자체 효과가 없는 아이템도 투척되었다면 장판 반응의 재료가 될 수 있다.
        if (itemData == null || (!isThrownItem && !CanExecuteItemEffect(itemData)) || (parent != null && !parent.CanContinue))
            return;
        usePosition.z = targetPosition.z = direction.z = 0f;
        ItemEffectContext context = new ItemEffectContext(owner, itemData, usePosition,
            targetPosition, sourceBag, buffManager: manager, direction: direction);
        context.InheritExecution(parent);
        context.consumeUseBuffs = consumeUseBuffs && (parent == null || parent.consumeUseBuffs);
        EffectVisualData completionVisual = itemData.endVisualData;
        bool completionAtOwner = itemData.endVisualAtOwner;
        ItemData[] completionItems = itemData.afterCompletionItems != null
            ? (ItemData[])itemData.afterCompletionItems.Clone() : null;
        bool hasCompletionItems = HasCompletionItems(completionItems);
        bool completionItemsAtOwner = itemData.afterCompletionItemsAtOwner;
        bool completionConsumeUseBuffs = itemData.afterCompletionConsumeUseBuffs;
        bool completionTriggerSpecialItems = itemData.afterCompletionTriggerSpecialItems;
        ItemEffectLifetime scope = new ItemEffectLifetime(parent != null ? parent.lifetime : null,
            completionVisual != null || hasCompletionItems ? (System.Action)(() =>
        {
            if (!context.CanContinue)
                return;
            if (completionVisual != null && context.CanContinue)
                completionVisual.Play(new EffectVisualContext(
                    completionAtOwner && owner != null ? owner.transform.position : targetPosition,
                    Quaternion.identity));
            if (!hasCompletionItems || !context.CanContinue)
                return;
            Vector3 position = completionItemsAtOwner && owner != null ? owner.transform.position : targetPosition;
            ItemEffectContext continuation = context.Copy(position, direction);
            // 현재 scope는 이미 종료됐다. 살아 있는 바깥 scope에 연결해 Then 등도 후속 아이템을 기다린다.
            continuation.lifetime = parent != null ? parent.lifetime : null;
            if (completionItems != null)
                foreach (ItemData item in completionItems)
                {
                    if (!context.CanContinue) break;
                    if (!CanExecuteItemEffect(item) || !continuation.TryBeginCompletionItem(item)) continue;
                    ExecuteItem(item, position, position, direction, owner, sourceBag, manager, continuation,
                        completionTriggerSpecialItems, consumeUseBuffs: completionConsumeUseBuffs);
                }
        }) : null, trackCompletion: completionVisual != null || hasCompletionItems ||
            (parent != null && parent.lifetime != null && parent.lifetime.TracksCompletion),
            cancelOnChildFailure: hasCompletionItems);
        context.lifetime = scope;
        BuffItemUseToken token = manager != null && context.consumeUseBuffs ? manager.BeginItemUse(itemData, sourceBag) : default(BuffItemUseToken);
        bool succeeded = false;
        try
        {
            bool replaced = isThrownItem && ReactiveGroundArea.NotifyItemLanded(context);
            for (int i = 0; !replaced && itemData.effectDatas != null && i < itemData.effectDatas.Length && context.CanContinue; i++)
            {
                ItemEffectData effect = itemData.effectDatas[i];
                if (effect != null) effect.Execute(context);
            }
            succeeded = context.CanContinue;
        }
        finally
        {
            try
            {
                if (manager != null && context.consumeUseBuffs) manager.EndItemUse(token, succeeded);
                // 함수 종료 알림은 한 아이템당 한 번. 실제 수명 종료는 ItemEffectLifetime이 담당한다.
                if (succeeded && triggerSpecialItems && SpecialItemManager.Instance != null)
                    SpecialItemManager.Instance.Call(context);
            }
            finally { scope.Close(succeeded); }
        }
    }

    public static bool CanExecuteItemEffect(ItemData itemData)
    {
        if (itemData == null || itemData.effectDatas == null) return false;
        for (int i = 0; i < itemData.effectDatas.Length; i++)
            if (itemData.effectDatas[i] != null) return true;
        return false;
    }

    private static bool HasCompletionItems(ItemData[] items)
    {
        if (items != null)
            foreach (ItemData item in items)
                if (CanExecuteItemEffect(item)) return true;
        return false;
    }
}
