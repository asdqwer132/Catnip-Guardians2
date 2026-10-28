using UnityEngine;

public class ItemSlotTooltipProvider : TooltipProvider
{
    [Header("Target")]
    [SerializeField] private BaseItemSlotUI slot;

    protected override void Awake()
    {
        base.Awake();
        if (slot == null) slot = GetComponent<BaseItemSlotUI>();
    }

    public override bool TryGetTooltipData(out TooltipData data)
    {
        InventoryItem item = slot != null ? slot.currentItem : null;
        data = item != null ? ItemTooltipData.FromItem(item.itemData, item.amount) : null;
        return data != null;
    }
}
