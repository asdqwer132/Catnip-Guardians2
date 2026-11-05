public sealed class PotionEquipmentInventoryUI : InventoryUI
{
    public PotionEquipmentPanelUI equipmentPanel;
    protected override bool ListenForMaterialReturns => false;

    protected override bool CanDisplayItem(InventoryItem item)
        => item != null && item.amount > 0 && PotionEquipmentManager.IsPotion(item.itemData) && base.CanDisplayItem(item);

    protected override void ConfigureSlot(BaseItemSlotUI slot)
    {
        if (slot is PotionEquipInventorySlotUI potionSlot) potionSlot.Bind(equipmentPanel);
    }
}
