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

        float previewRadius = GetPreviewRadius(
            inventoryItem,
            owner,
            currentCycleId
        );

        TargetRangeIndicator rangeIndicator = CreateTargetRangeIndicator(
            targetPosition,
            previewRadius
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

    private TargetRangeIndicator CreateTargetRangeIndicator(
        Vector3 targetPosition,
        float radius
    )
    {
        if (!showTargetRange)
            return null;

        if (targetRangeIndicatorPrefab == null)
            return null;

        if (radius <= 0f)
            return null;

        targetPosition.z = 0f;

        TargetRangeIndicator indicator = Instantiate(
            targetRangeIndicatorPrefab,
            targetPosition,
            Quaternion.identity
        );

        indicator.SetRadius(radius);

        return indicator;
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
            itemObject: null,
            itemData: itemData,
            usePosition: arrivePosition,
            targetPosition: arrivePosition,
            direction: direction,
            currentBag: currentBag,
            inventoryItem: inventoryItem,
            currentEffectData: null,
            effectStat: null,
            currentCycleId: currentCycleId
        );

        float biggestRadius = 0f;

        if (itemData.effectDatas != null)
        {
            for (int i = 0; i < itemData.effectDatas.Length; i++)
            {
                ItemEffectData effectData = itemData.effectDatas[i];

                if (effectData == null)
                    continue;

                EffectStat baseEffectStat = GetBaseEffectStat(effectData);

                EffectStat finalStat = GetFinalEffectStat(
                    inventoryItem,
                    baseEffectStat,
                    owner,
                    currentCycleId
                );

                context.SetCurrentEffect(
                    effectData,
                    finalStat
                );

                if (finalStat != null)
                    biggestRadius = Mathf.Max(biggestRadius, finalStat.effectRadius);

                effectData.Execute(context);
            }
        }

        SpawnImpactVfx(
            itemData,
            arrivePosition,
            biggestRadius
        );

        inventoryItem.ConsumeNextItemUseBuffs();

        Debug.Log(itemData.itemName + " 도착 위치에서 이펙트 실행");
    }

    private void SpawnImpactVfx(
        ItemData itemData,
        Vector3 position,
        float effectRadius
    )
    {
        if (itemData == null)
            return;

        if (itemData.impactVfxPrefab == null)
            return;

        position.z = 0f;

        GameObject vfx = Instantiate(
            itemData.impactVfxPrefab,
            position,
            Quaternion.identity
        );

        if (itemData.scaleImpactVfxByRadius)
        {
            float size = Mathf.Max(0.01f, effectRadius);
            vfx.transform.localScale = new Vector3(size, size, size);
        }

        Destroy(
            vfx,
            Mathf.Max(0.01f, itemData.impactVfxLifeTime)
        );
    }

    private float GetPreviewRadius(
        InventoryItem inventoryItem,
        GameObject owner,
        int currentCycleId
    )
    {
        if (inventoryItem == null || inventoryItem.itemData == null)
            return defaultTargetRangeRadius;

        ItemEffectData[] effectDatas = inventoryItem.itemData.effectDatas;

        if (effectDatas == null || effectDatas.Length == 0)
            return defaultTargetRangeRadius;

        float biggestRadius = 0f;

        for (int i = 0; i < effectDatas.Length; i++)
        {
            ItemEffectData effectData = effectDatas[i];

            if (effectData == null)
                continue;

            EffectStat baseEffectStat = GetBaseEffectStat(effectData);

            EffectStat finalStat = GetFinalEffectStat(
                inventoryItem,
                baseEffectStat,
                owner,
                currentCycleId
            );

            if (finalStat == null)
                continue;

            biggestRadius = Mathf.Max(
                biggestRadius,
                finalStat.effectRadius
            );
        }

        if (biggestRadius <= 0f)
            return defaultTargetRangeRadius;

        return biggestRadius;
    }

    private EffectStat GetBaseEffectStat(ItemEffectData effectData)
    {
        if (effectData == null)
            return null;

        IItemEffectStatProvider statProvider =
            effectData as IItemEffectStatProvider;

        if (statProvider == null)
            return null;

        return statProvider.GetBaseEffectStat();
    }

    private EffectStat GetFinalEffectStat(
        InventoryItem inventoryItem,
        EffectStat baseEffectStat,
        GameObject owner,
        int currentCycleId
    )
    {
        EffectStat ownerStat = null;

        if (owner != null)
        {
            PlayerStat playerStat = owner.GetComponent<PlayerStat>();

            if (playerStat != null)
                ownerStat = playerStat.TotalStat;
        }

        if (inventoryItem != null)
        {
            return inventoryItem.GetFinalEffectStat(
                baseEffectStat,
                ownerStat,
                currentCycleId
            );
        }

        EffectStat result;

        if (baseEffectStat != null)
            result = baseEffectStat.Clone();
        else
            result = new EffectStat();

        if (ownerStat != null)
            result.Add(ownerStat);

        return result;
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
}