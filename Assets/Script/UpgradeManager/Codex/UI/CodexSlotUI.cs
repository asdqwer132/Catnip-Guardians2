public sealed class CodexSlotUI : InventorySlotUI
{
    protected override bool AllowEmptySelection => true;

    protected override bool KeepSelectionOnRefresh => true;

    public void Bind(DefaultData entry)
    {
        if (dataSource != entry) ResetSelection(true);
        IsLocked = false;
        if (entry is ItemData item)
            SetSlot(new InventoryItem(item, InventoryManager.instance != null ? InventoryManager.instance.GetItemAmount(item) : 0));
        else
        {
            ClearSlot();
            SetData(entry);
        }
        if (locked != null) locked.SetActive(false);
        if (ready != null) ready.SetActive(false);
        if (slotToggle != null) slotToggle.interactable = entry != null;
        if (slotButton != null) slotButton.interactable = entry != null;
    }
}
