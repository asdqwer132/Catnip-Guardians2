using TMPro;

public sealed class PotionEquipSlotUI : ClickableItemSlotUI
{
    public TextMeshProUGUI slotNumberText;
    private PotionEquipmentPanelUI panel;
    public int SlotIndex { get; private set; } = -1;
    protected override bool AllowEmptySelection => true;
    protected override bool KeepSelectionOnRefresh => true;

    public void Bind(PotionEquipmentPanelUI owner, int index)
    {
        panel = owner;
        SlotIndex = index;
        RefreshSlot();
    }

    public void RefreshSlot()
    {
        if (panel == null || panel.manager == null) return;
        if (panel.manager.IsLocked(SlotIndex)) LockSlot();
        else
        {
            var potion = panel.manager.GetPotion(SlotIndex);
            SetSlot(potion != null ? new InventoryItem(potion, 1) : null);
        }
        if (slotNumberText != null) slotNumberText.text = (SlotIndex + 1).ToString();
        if (slotToggle != null) slotToggle.SetIsOnWithoutNotify(!IsLocked && panel.SelectedSlot == SlotIndex);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        RefreshSlot();
    }

    protected override void OnSelectionChanged(bool selected)
    {
        if (panel == null) return;
        if (selected) panel.SelectSlot(SlotIndex);
        else if (panel.SelectedSlot == SlotIndex)
        {
            if (slotToggle != null && slotToggle.group != null && slotToggle.group.AnyTogglesOn()) return;
            panel.ClearSelection();
        }
    }
}
