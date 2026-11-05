public sealed class CodexSlotUI : InventorySlotUI
{
    private CodexInventoryUI owner;
    public DefaultData Entry { get; private set; }
    protected override bool AllowEmptySelection => true;

    public void Bind(DefaultData entry, CodexInventoryUI inventory)
    {
        owner = inventory;
        Entry = entry;
        IsLocked = false;
        currentItem = entry is ItemData item ? new InventoryItem(item, 1) : null;
        SetData(entry);
        if (amountImage != null) amountImage.SetActive(false);
        if (amountText != null) amountText.text = "";
        if (locked != null) locked.SetActive(false);
        if (ready != null) ready.SetActive(false);
        if (slotToggle != null) slotToggle.interactable = entry != null;
        if (slotButton != null) slotButton.interactable = entry != null;
    }

    public void ApplySelection(bool selected)
    {
        if (slotToggle == null) return;
        if (selected) slotToggle.SetIsOnWithoutNotify(true);
        else ResetSelection();
    }

    protected override void OnSelectionChanged(bool selected)
    {
        if (selected) OnClickSlot();
        else if (owner != null && owner.SelectedEntry == Entry &&
                 (slotToggle == null || slotToggle.group == null || !slotToggle.group.AnyTogglesOn()))
        {
            ApplySelection(true);
            OnClickSlot(); // Reopen this entry after navigating to a child stage's details.
        }
    }

    public override void OnClickSlot()
    {
        if (owner != null && Entry != null) owner.Select(Entry);
    }
}
