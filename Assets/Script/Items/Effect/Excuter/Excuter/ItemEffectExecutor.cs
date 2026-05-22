using UnityEngine;

public class ItemEffectExecutor : MonoBehaviour
{
    [Header("Managers")]
    public BuffManager buffManager;


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
        if (itemData == null)
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
            buffManager: buffManager
        );

        ExecuteItemEffectDatas(itemData, context);

        Debug.Log(itemData.dataName + " ¿Ã∆Â∆Æ Ω««‡");
    }

    public void ExecuteItemEffectWithContext(
        ItemData itemData,
        ItemEffectContext context
    )
    {
        if (itemData == null)
            return;

        if (context == null)
            return;

        ExecuteItemEffectDatas(itemData, context);

        Debug.Log(itemData.dataName + " ¿Ã∆Â∆Æ Ω««‡");
    }

    private void ExecuteItemEffectDatas(
        ItemData itemData,
        ItemEffectContext context
    )
    {
        if (itemData == null)
            return;

        if (context == null)
            return;

        if (itemData.effectDatas == null)
            return;

        for (int i = 0; i < itemData.effectDatas.Length; i++)
        {
            ItemEffectData effectData = itemData.effectDatas[i];

            if (effectData == null)
                continue;

            context.SetCurrentEffect(effectData);
            effectData.Execute(context);
        }
    }

    public static bool CanExecuteItemEffect(InventoryItem inventoryItem)
    {
        if (inventoryItem == null || inventoryItem.itemData == null)
            return false;

        return CanExecuteItemEffect(inventoryItem.itemData);
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