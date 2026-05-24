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

    [Header("Executors")]
    public ItemEffectExecutor itemEffectExecutor;

    public void Throw(
        ItemData inventoryItem,
        Vector3 startPosition,
        Vector3 targetPosition,
        Vector3 direction,
        GameObject owner,
        EquipmentBag currentBag,
        int currentCycleId
    )
    {
        if (inventoryItem == null)
            return;

        if (itemEffectExecutor == null)
            return;

        startPosition.z = 0f;
        targetPosition.z = 0f;
        direction.z = 0f;

        ItemThrowMover mover = CreateThrowMover(startPosition);

        if (mover == null)
            return;

        Sprite itemSprite = inventoryItem.icon;

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

                itemEffectExecutor.ExecuteItemEffect(
                    inventoryItem,
                    targetPosition,
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

        if (defaultTargetRangeRadius > 0f)
            indicator.SetRadius(defaultTargetRangeRadius);

        return indicator;
    }
}