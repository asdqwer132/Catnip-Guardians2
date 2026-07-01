using UnityEngine;

public class ItemSlotTooltipProvider : TooltipProvider
{
    [Header("Target")]
    [SerializeField] private BaseItemSlotUI slot;

    protected override void Awake()
    {
        base.Awake();

        if (slot == null)
            slot = GetComponent<BaseItemSlotUI>();
    }

    public override bool TryGetTooltipData(out TooltipData data)
    {
        data = null;

        if (slot == null)
            return false;

        if (slot.currentItem == null)
            return false;

        if (slot.currentItem.itemData == null)
            return false;

        InventoryItem item = slot.currentItem;
        ItemData itemData = item.itemData;

        data = new TooltipData
        {
            icon = itemData.icon,
            title = GetItemName(itemData),
            subTitle = itemData.grade.ToString(),
            amountText = item.amount > 1 ? $"x{item.amount}" : "",
            description = GetItemDescription(itemData)
        };

        return true;
    }

    private string GetItemName(ItemData itemData)
    {
        if (itemData == null)
            return "";

        string dataName = itemData.GetDataName();

        if (!string.IsNullOrEmpty(dataName))
            return dataName;

        return itemData.name;
    }

    private string GetItemDescription(ItemData itemData)
    {
        if (itemData == null)
            return "";

        return itemData.GetDescription();
    }
}