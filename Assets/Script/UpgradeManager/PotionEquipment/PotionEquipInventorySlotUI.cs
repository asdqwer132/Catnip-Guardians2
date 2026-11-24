public sealed class PotionEquipInventorySlotUI : ClickableItemSlotUI
{
    private PotionEquipmentPanelUI panel;
    public void Bind(PotionEquipmentPanelUI owner) => panel = owner;

    protected override void Awake()
    {
        base.Awake();
        if (slotToggle is PotionEquipInventoryToggle toggle) toggle.Clicked += OnClickSlot;
    }

    // Value changes only select and show tooltip information; clicks perform equipment actions.
    protected override void OnSelectionChanged(bool selected) { }

    public override void OnClickSlot()
    {
        // Capture the data before equipping can consume the last unit and recycle this row.
        var potion = currentItem?.itemData;
        if (IsLocked || panel == null || !PotionEquipmentManager.IsPotion(potion)) return;
        OnToggleValueChanged(true);
        panel.SelectInventoryPotion(potion);
    }

    protected override void OnDestroy()
    {
        if (slotToggle is PotionEquipInventoryToggle toggle) toggle.Clicked -= OnClickSlot;
        base.OnDestroy();
    }
}
