public sealed class PotionEquipInventorySlotUI : ClickableItemSlotUI
{
    private PotionEquipmentPanelUI panel;
    public void Bind(PotionEquipmentPanelUI owner) => panel = owner;

    public override void OnClickSlot()
    {
        // Capture the data before equipping can consume the last unit and recycle this row.
        var potion = currentItem?.itemData;
        if (panel != null && PotionEquipmentManager.IsPotion(potion)) panel.SelectInventoryPotion(potion);
    }
}
