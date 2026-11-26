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
        if (data == null && slot is BagEquipmentSlotUI && slot.dataSource is ItemData equipped)
        {
            data = ItemTooltipData.FromItem(equipped, 0);
            data.amountText = null;
        }

        if (data == null && slot != null && slot.dataSource != null && !(slot.dataSource is ItemData))
        {
            var source = slot.dataSource;
            data = source is ItemBoxData box ? new CostTooltipData { costs = box.costs } : new TooltipData();
            data.sourceData = source;
            data.icon = slot.icon != null && slot.icon.enabled ? slot.icon.sprite : source.Icon;
            data.dataName = source.name;
        }
        return data != null;
    }
}
