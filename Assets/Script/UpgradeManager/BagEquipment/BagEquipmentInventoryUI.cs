public sealed class BagEquipmentInventoryUI : InventoryUI
{
    public BagEquipmentPanelUI equipment;
    protected override bool ListenForMaterialReturns => false;

    protected override bool CanDisplayItem(InventoryItem item)
        => item != null && item.amount > 0 && item.itemData != null &&
            equipment != null && equipment.IsMaterial(item.itemData) && base.CanDisplayItem(item);

    protected override void ConfigureSlot(BaseItemSlotUI slot)
    {
        if (slot is ClickableItemSlotUI clickable)
            clickable.Clicked += item => equipment?.SelectMaterial(item?.itemData);
    }
}
