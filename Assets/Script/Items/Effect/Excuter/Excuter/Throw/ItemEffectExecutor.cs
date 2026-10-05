using UnityEngine;

public class ItemEffectExecutor : MonoBehaviour
{
    [Header("Managers")]
    public BuffManager buffManager;
    public void JustExcuteItem(ItemData itemData)
    {
        ExecuteItemEffect(itemData, Vector3.zero, Vector3.zero, Vector3.zero, gameObject, null, 0);
    }
    public void ExecuteItemEffect(
        ItemData itemData,
        Vector3 usePosition,
        Vector3 targetPosition,
        Vector3 direction,
        GameObject owner,
        EquipmentBag sourceBag,
        int currentCycleId
    )
    {
        if (!CanExecuteItemEffect(itemData))
            return;

        usePosition.z = 0f;
        targetPosition.z = 0f;
        direction.z = 0f;

        ItemEffectContext context = new ItemEffectContext(
            owner: owner,
            sourceItemData: itemData,
            usePosition: usePosition,
            targetPosition: targetPosition,
            sourceBag: sourceBag,
            currentEffectData: null,
            buffManager: buffManager != null ? buffManager : BuffManager.instance,
            direction: direction
        );

        BuffManager manager = context.buffManager;
        BuffItemUseToken token = manager != null
            ? manager.BeginItemUse(itemData, sourceBag)
            : default(BuffItemUseToken);
        bool succeeded = false;
        try
        {
            ExecuteItemEffectDatas(itemData, context);
            succeeded = true;
        }
        finally
        {
            // 마지막 1회까지 모든 효과에 버프를 적용한 후 정확히 한 번 차감한다.
            if (manager != null)
                manager.EndItemUse(token, succeeded);
        }
    }

    private void ExecuteItemEffectDatas(ItemData itemData, ItemEffectContext context)
    {
        if (itemData == null)
            return;

        if (context == null)
            return;

        if (itemData.effectDatas == null)
            return;

        //AudioManager.instance.PlaySfx("Item");

        for (int i = 0; i < itemData.effectDatas.Length; i++)
        {
            ItemEffectData effectData = itemData.effectDatas[i];

            if (effectData == null)
                continue;

            context.SetCurrentEffect(effectData);
            effectData.Execute(context);
        }
    }

    public static bool CanExecuteItemEffect(ItemData itemData)
    {
        if (itemData == null)
            return false;

        if (itemData.effectDatas == null || itemData.effectDatas.Length == 0)
            return false;

        for (int i = 0; i < itemData.effectDatas.Length; i++)
        {
            if (itemData.effectDatas[i] != null)
                return true;
        }

        return false;
    }
}
