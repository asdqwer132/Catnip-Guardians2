using UnityEngine;

public class ItemSlotTooltipProvider : MonoBehaviour, ITooltipContentProvider
{
    [Header("Target")]
    [SerializeField] private BaseItemSlotUI slot;

    [Header("Anchor")]
    [SerializeField] private RectTransform anchorRect;

    private void Awake()
    {
        if (slot == null)
            slot = GetComponent<BaseItemSlotUI>();

        if (anchorRect == null)
            anchorRect = transform as RectTransform;
    }

    public bool TryGetTooltipData(out TooltipData data)
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
            title = itemData.GetDataName(),
            subTitle = itemData.grade.ToString(),
            amountText = item.amount > 1 ? $"x{item.amount}" : "",
            description = itemData.GetDescription()
        };

        return true;
    }

    public RectTransform GetTooltipAnchor()
    {
        if (anchorRect == null)
            anchorRect = transform as RectTransform;

        return anchorRect;
    }
}