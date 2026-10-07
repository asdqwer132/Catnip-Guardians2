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
        ItemEffectContext parent = null, bool triggerSpecialItems = true)
    {
        if (!CanExecuteItemEffect(itemData) || (parent != null && !parent.CanContinue))
            return;
        usePosition.z = targetPosition.z = direction.z = 0f;
        ItemEffectContext context = new ItemEffectContext(owner, itemData, usePosition,
            targetPosition, sourceBag, buffManager: manager, direction: direction);
        context.InheritExecution(parent);
        EffectVisualData completionVisual = itemData.endVisualData;
        bool completionAtOwner = itemData.endVisualAtOwner;
        ItemEffectLifetime scope = new ItemEffectLifetime(parent != null ? parent.lifetime : null, completionVisual != null ? (System.Action)(() =>
        {
            if (completionVisual != null && context.CanContinue)
                completionVisual.Play(new EffectVisualContext(
                    completionAtOwner && owner != null ? owner.transform.position : targetPosition,
                    Quaternion.identity));
        }) : null, trackCompletion: completionVisual != null || (parent != null && parent.lifetime != null && parent.lifetime.TracksCompletion));
        context.lifetime = scope;
        BuffItemUseToken token = manager != null ? manager.BeginItemUse(itemData, sourceBag) : default(BuffItemUseToken);
        bool succeeded = false;
        try
        {
            for (int i = 0; i < itemData.effectDatas.Length && context.CanContinue; i++)
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
                if (manager != null) manager.EndItemUse(token, succeeded);
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
}
