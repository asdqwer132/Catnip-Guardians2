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
        int currentCycleId,
        ItemEffectContext parentContext = null,
        bool triggerSpecialItems = true,
        ItemThrowOptions options = null
    )
    {
        if (inventoryItem == null || (parentContext != null && !parentContext.CanContinue))
            return false;

        if (itemEffectExecutor == null)
            return false;

        startPosition.z = 0f;
        targetPosition.z = 0f;

        Vector3 direction = targetPosition - startPosition;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            if (options == null || !options.allowStationaryThrow) return false;
            if (parentContext == null || !parentContext.TryGetDirection(out direction)) direction = Vector3.up;
        }

        direction.z = 0f;
        direction.Normalize();

        ItemThrowMover selectedPrefab = options != null && options.projectilePrefab != null ? options.projectilePrefab : throwMoverPrefab;
        ItemThrowMover mover = CreateThrowMover(startPosition, selectedPrefab);

        if (mover == null)
            return false;

        RegisterRuntimeObject(mover);
        options?.ApplyTo(mover);

        Sprite itemSprite = options != null && options.projectileSprite != null ? options.projectileSprite : inventoryItem.icon;

        TargetRangeIndicator rangeIndicator = CreateTargetRangeIndicator(targetPosition);

        if (rangeIndicator != null)
            RegisterRuntimeObject(rangeIndicator);

        BuffManager manager = parentContext != null && parentContext.buffManager != null ? parentContext.buffManager :
            itemEffectExecutor.buffManager != null ? itemEffectExecutor.buffManager : BuffManager.instance;
        ItemEffectContext flight = new ItemEffectContext(owner, inventoryItem, startPosition, targetPosition,
            currentBag, buffManager: manager, direction: direction);
        flight.InheritExecution(parentContext);
        ItemEffectLifetime flightScope = new ItemEffectLifetime(parentContext != null ? parentContext.lifetime : null,
            trackCompletion: parentContext != null && parentContext.lifetime != null && parentContext.lifetime.TracksCompletion);
        flight.lifetime = flightScope;
        ItemThrowCompletion completion = mover.gameObject.AddComponent<ItemThrowCompletion>();
        completion.Init(flight);

        mover.Init(
            startPosition,
            targetPosition,
            itemSprite,
            () =>
            {
                if (rangeIndicator != null)
                    Destroy(rangeIndicator.gameObject);

                try
                {
                    if (flight.CanContinue)
                        ItemEffectExecutor.ExecuteItem(inventoryItem, startPosition, targetPosition, direction,
                            owner, currentBag, manager, flight, triggerSpecialItems, isThrownItem: true);
                }
                finally { completion.Complete(); }

            },
            itemWeight: inventoryItem.weight
        );

        flightScope.Close();
        return true;
    }

    private ItemThrowMover CreateThrowMover(Vector3 startPosition, ItemThrowMover selectedPrefab)
    {
        if (selectedPrefab != null)
        {
            return Instantiate(
                selectedPrefab,
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
