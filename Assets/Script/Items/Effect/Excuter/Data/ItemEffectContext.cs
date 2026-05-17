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
        this.effectStat = effectStat;
        this.currentCycleId = currentCycleId;

        if (currentBag != null)
            this.bagItems = currentBag.equippedItems;

        if (this.effectStat == null && itemData != null)
            this.effectStat = itemData.effectStat;

        if (this.effectStat != null)
        {
            this.effectRadius = this.effectStat.effectRadius;
            this.effectCount = this.effectStat.effectCount;
        }
        else
        {
            this.effectRadius = 0f;
            this.effectCount = 0;
        }
    }
}