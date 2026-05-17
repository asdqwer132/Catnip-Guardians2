using System.Collections.Generic;
using UnityEngine;

public class ItemEffectContext
{
    public GameObject owner;
    public GameObject itemObject;
    public ItemData itemData;

    public Vector3 usePosition;
    public Vector3 targetPosition;
    public Vector3 direction;

    public EquipmentBag currentBag;
    public List<InventoryItem> bagItems;

    public InventoryItem inventoryItem;

    public ItemEffectData currentEffectData;
    public EffectStat effectStat;

    public float effectRadius;
    public int effectCount;

    public int currentCycleId;

    public ItemEffectContext(
        GameObject owner,
        GameObject itemObject,
        ItemData itemData,
        Vector3 usePosition,
        Vector3 targetPosition,
        Vector3 direction,
        EquipmentBag currentBag,
        InventoryItem inventoryItem = null,
        ItemEffectData currentEffectData = null,
        EffectStat effectStat = null,
        int currentCycleId = 0
    )
    {
        this.owner = owner;
        this.itemObject = itemObject;
        this.itemData = itemData;

        this.usePosition = usePosition;
        this.targetPosition = targetPosition;
        this.direction = direction;

        this.currentBag = currentBag;
        this.inventoryItem = inventoryItem;

        this.currentEffectData = currentEffectData;
        this.effectStat = effectStat;

        this.currentCycleId = currentCycleId;

        if (currentBag != null)
            this.bagItems = currentBag.equippedItems;

        RefreshCachedValues();
    }

    public void SetCurrentEffect(
        ItemEffectData effectData,
        EffectStat finalStat
    )
    {
        currentEffectData = effectData;
        effectStat = finalStat;

        RefreshCachedValues();
    }

    private void RefreshCachedValues()
    {
        if (effectStat != null)
        {
            effectRadius = effectStat.effectRadius;
            effectCount = effectStat.effectCount;
        }
        else
        {
            effectRadius = 0f;
            effectCount = 0;
        }
    }
}