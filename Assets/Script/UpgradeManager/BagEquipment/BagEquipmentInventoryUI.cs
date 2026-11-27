public sealed class BagEquipmentInventoryUI : InventoryUI
{
    public BagEquipmentPanelUI equipment;
    public BoardEquipmentPanelUI board;
    protected override bool ListenForMaterialReturns => false;

    protected override bool CanDisplayItem(InventoryItem item)
        => item != null && item.amount > 0 && item.itemData != null &&
            (board != null ? board.CanDisplay(item.itemData) : equipment != null && equipment.IsMaterial(item.itemData)) && base.CanDisplayItem(item);

    protected override DataType GetSearchCategory(ItemData item)
        => item is BoardPinData ? DataType.Pin : BoardLoadout.AcceptsMaterial(item)
            ? DataType.ItemMaterial : base.GetSearchCategory(item);

    protected override void ConfigureTooltip(ItemTooltipTrigger trigger)
    {
        base.ConfigureTooltip(trigger);
        if (tooltipUI != null) trigger.isPointer = true;
    }

    protected override void ConfigureSlot(BaseItemSlotUI slot)
    {
        if (slot is ClickableItemSlotUI clickable)
            clickable.Clicked += Select;
    }

    private void Select(InventoryItem item)
    {
        if (board != null) board.SelectItem(item?.itemData);
        else equipment?.SelectMaterial(item?.itemData);
    }
}
