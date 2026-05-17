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

        ApplyItemThrowSetting(mover, inventoryItem);

        EffectStat finalStatForPreview = GetFinalEffectStat(
            inventoryItem,
            owner,
            currentCycleId
        );

        TargetRangeIndicator rangeIndicator = CreateTargetRangeIndicator(
            inventoryItem,
            targetPosition,
            finalStatForPreview
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
        ItemThrowMover mover = null;

        if (throwMoverPrefab != null)
        {
            mover = Instantiate(
                throwMoverPrefab,
                startPosition,
                Quaternion.identity
            );

            return mover;
        }

        GameObject fallbackObj = new GameObject("ItemThrowMover");
        fallbackObj.transform.position = startPosition;

        mover = fallbackObj.AddComponent<ItemThrowMover>();

        return mover;
    }

    private Sprite GetItemSprite(InventoryItem inventoryItem)
    {
        if (!useItemIconWhenPrefabMissing)
            return null;

        if (inventoryItem == null || inventoryItem.itemData == null)
            return null;

        return inventoryItem.itemData.icon;
    }

    private void ApplyItemThrowSetting(
        ItemThrowMover mover,
        InventoryItem inventoryItem
    )
    {
        if (mover == null)
            return;

        if (inventoryItem == null || inventoryItem.itemData == null)
            return;

        ItemData itemData = inventoryItem.itemData;

        if (!itemData.overrideThrowSetting)
            return;

        mover.arriveTime = itemData.throwArriveTime;
        mover.arcHeight = itemData.throwArcHeight;
        mover.autoArcHeightByDistance = itemData.autoArcHeightByDistance;
        mover.minArcHeight = itemData.minArcHeight;
        mover.maxArcHeight = itemData.maxArcHeight;
        mover.arcHeightDistanceMultiplier = itemData.arcHeightDistanceMultiplier;
    }

    private TargetRangeIndicator CreateTargetRangeIndicator(
        InventoryItem inventoryItem,
        Vector3 targetPosition,
        EffectStat finalStat
    )
    {
        if (!showTargetRange)
            return null;

        if (targetRangeIndicatorPrefab == null)
            return null;

        float radius = GetTargetRangeRadius(
            inventoryItem,
            finalStat
        );

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

    private float GetTargetRangeRadius(
        InventoryItem inventoryItem,
        EffectStat finalStat
    )
    {
        if (finalStat != null)
            return finalStat.effectRadius;

        if (inventoryItem != null &&
            inventoryItem.itemData != null &&
            inventoryItem.itemData.effectStat != null)
        {
            return inventoryItem.itemData.effectStat.effectRadius;
        }

        return defaultTargetRangeRadius;
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

        EffectStat finalStat = GetFinalEffectStat(
            inventoryItem,
            owner,
            currentCycleId
        );

        ItemEffectContext context = new ItemEffectContext(
            owner: owner,
            itemObject: null,
            itemData: itemData,
            usePosition: arrivePosition,
            targetPosition: arrivePosition,
            direction: direction,
            currentBag: currentBag,
            inventoryItem: inventoryItem,
            effectStat: finalStat,
            currentCycleId: currentCycleId
        );

        if (itemData.effectDatas != null)
        {
            for (int i = 0; i < itemData.effectDatas.Length; i++)
            {
                ItemEffectData effectData = itemData.effectDatas[i];

                if (effectData == null)
                    continue;

                effectData.Execute(context);
            }
        }

        SpawnImpactVfx(
            itemData,
            arrivePosition,
            context.effectRadius
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
            float size = Mathf.Max(0f, effectRadius);
            vfx.transform.localScale = new Vector3(size, size, size);
        }

        Destroy(
            vfx,
            Mathf.Max(0.01f, itemData.impactVfxLifeTime)
        );
    }

    private EffectStat GetFinalEffectStat(
        InventoryItem inventoryItem,
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
                ownerStat,
                currentCycleId
            );
        }

        EffectStat result = new EffectStat();

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