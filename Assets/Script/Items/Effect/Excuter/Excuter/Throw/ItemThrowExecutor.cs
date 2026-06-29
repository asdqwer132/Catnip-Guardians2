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

    public bool Throw(
        ItemData inventoryItem,
        Vector3 startPosition,
        Vector3 targetPosition,
        GameObject owner,
        EquipmentBag currentBag,
        int currentCycleId
    )
    {
        if (inventoryItem == null)
            return false;

        if (itemEffectExecutor == null)
            return false;

        startPosition.z = 0f;
        targetPosition.z = 0f;

        Vector3 direction = targetPosition - startPosition;
        if (direction.sqrMagnitude <= 0.0001f)
            return false;

        direction.z = 0f;
        direction.Normalize();

        ItemThrowMover mover = CreateThrowMover(startPosition);

        if (mover == null)
            return false;

        RegisterRuntimeObject(mover);

        Sprite itemSprite = inventoryItem.icon;

        TargetRangeIndicator rangeIndicator = CreateTargetRangeIndicator(targetPosition);

        if (rangeIndicator != null)
            RegisterRuntimeObject(rangeIndicator);

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

        return true;
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

    private void RegisterRuntimeObject(Component component)
    {
        if (component == null)
            return;

        if (ItemRuntimeObjectManager.Instance == null)
            return;

        ItemRuntimeObjectManager.Instance.Register(component);
    }
}