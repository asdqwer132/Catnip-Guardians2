using System.Collections.Generic;
using UnityEngine;

public enum RuntimeBuffUIScope
{
    CurrentBag,
    AllBags
}

public class RuntimeBuffUIInfo
{
    public Object source;
    public Sprite icon;
    public RuntimeBuffDurationType durationType;

    public RuntimeBuffUIScope scope;

    public EquipmentBag currentBag;
    public List<InventoryItem> bagItems;

    public int currentCycleId;

    public int startNumber;

    public RuntimeBuffUIInfo(
        Object source,
        Sprite icon,
        RuntimeBuffDurationType durationType,
        RuntimeBuffUIScope scope,
        EquipmentBag currentBag,
        List<InventoryItem> bagItems,
        int currentCycleId,
        int startNumber
    )
    {
        this.source = source;
        this.icon = icon;
        this.durationType = durationType;
        this.scope = scope;
        this.currentBag = currentBag;
        this.bagItems = bagItems;
        this.currentCycleId = currentCycleId;
        this.startNumber = startNumber;
    }
}