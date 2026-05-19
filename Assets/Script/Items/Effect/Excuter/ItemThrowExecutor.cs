using UnityEngine;

public class ItemThrowExecutor : MonoBehaviour
{
    [Header("Throw")]
    public ItemThrowMover throwMoverPrefab;
    public bool useItemIconWhenPrefabMissing = true;

    [Header("Target Range Indicator")]
    public TargetRangeIndicator targetRangeIndicatorPrefab;
    public bool showTargetRange = true;
    public float defaultTargetRangeRadius = 1f;

    [Header("Managers")]
    public BuffManager buffManager;

    public void Throw(
        InventoryItem inventoryItem,
        Vector3 startPosition,
        Vector3 targetPosition,
        Vector3 direction,
        GameObject owner,
        EquipmentBag currentBag,
        int currentCycleId
    )
    {
        if (inventoryItem == null || inventoryItem.itemData == null)
            return;

        startPosition.z = 0f;
        targetPosition.z = 0f;
        direction.z = 0f;

        ItemThrowMover mover = CreateThrowMover(startPosition);

        if (mover == null)
            return;

        Sprite itemSprite = GetItemSprite(inventoryItem);

        TargetRangeIndicator rangeIndicator = CreateTargetRangeIndicator(
            targetPosition
        );

        mover.Init(
            startPosition,
            targetPosition,
            itemSprite,
            () =>
            {
                if (rangeIndicator != null)
                    Destroy(rangeIndicator.gameObject);

                ExecuteItemEffectAtArrivePosition(
                    inventoryItem,
                    targetPosition,
                    direction,
                    owner,
                    currentBag,
                    currentCycleId
                );
            }
        );
    }


    private void ExecuteItemEffectAtArrivePosition(
        InventoryItem inventoryItem,
        Vector3 arrivePosition,
        Vector3 direction,
        GameObject owner,
        EquipmentBag currentBag,
        int currentCycleId
    )
    {
        if (inventoryItem == null || inventoryItem.itemData == null)
            return;

        arrivePosition.z = 0f;
        direction.z = 0f;

        ItemData itemData = inventoryItem.itemData;

        ItemEffectContext context = new ItemEffectContext(
            owner: owner,
            sourceItemData: itemData,
            usePosition: arrivePosition,
            targetPosition: arrivePosition,
            sourceBag: currentBag,
            currentEffectData: null,
            buffManager: buffManager
        );

        if (itemData.effectDatas != null)
        {
            for (int i = 0; i < itemData.effectDatas.Length; i++)
            {
                ItemEffectData effectData = itemData.effectDatas[i];

                if (effectData == null)
                    continue;

                context.SetCurrentEffect(effectData);
                effectData.Execute(context);
            }
        }

        Debug.Log(itemData.dataName + " 도착 위치에서 이펙트 실행");
    }

    public static bool CanExecuteItemEffect(InventoryItem inventoryItem)
    {
        if (inventoryItem == null || inventoryItem.itemData == null)
            return false;

        ItemData itemData = inventoryItem.itemData;

        if (itemData.effectDatas == null || itemData.effectDatas.Length == 0)
            return false;

        for (int i = 0; i < itemData.effectDatas.Length; i++)
        {
            if (itemData.effectDatas[i] != null)
                return true;
        }

        return false;
    }


    private ItemThrowMover CreateThrowMover(Vector3 startPosition)
    {
        if (throwMoverPrefab != null)
        {
            return Instantiate(
                throwMoverPrefab,
                startPosition,
                Quaternion.identity
            );
        }

        GameObject fallbackObj = new GameObject("ItemThrowMover");
        fallbackObj.transform.position = startPosition;

        return fallbackObj.AddComponent<ItemThrowMover>();
    }

    private Sprite GetItemSprite(InventoryItem inventoryItem)
    {
        if (!useItemIconWhenPrefabMissing)
            return null;

        if (inventoryItem == null || inventoryItem.itemData == null)
            return null;

        return inventoryItem.itemData.icon;
    }

    private TargetRangeIndicator CreateTargetRangeIndicator(Vector3 targetPosition)
    {
        if (!showTargetRange)
            return null;

        if (targetRangeIndicatorPrefab == null)
            return null;

        targetPosition.z = 0f;

        TargetRangeIndicator indicator = Instantiate(
            targetRangeIndicatorPrefab,
            targetPosition,
            Quaternion.identity
        );

        return indicator;
    }
}