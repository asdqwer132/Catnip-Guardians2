using UnityEngine;
using UnityEngine.UI;

public class ClickableItemSlotUI : BaseItemSlotUI
{
    [Header("Interaction")]
    public Button slotButton;
    public Toggle slotToggle;
    private ItemTooltipTrigger selectionTooltip;
    protected virtual bool AllowEmptySelection => false;
    protected virtual bool KeepSelectionOnRefresh => false;

    protected virtual void Awake()
    {
        if (slotToggle == null) slotToggle = GetComponent<Toggle>();
        if (slotButton == null) slotButton = GetComponent<Button>();
        selectionTooltip = GetComponent<ItemTooltipTrigger>();
        if (slotToggle != null) slotToggle.onValueChanged.AddListener(OnToggleValueChanged);
        else if (slotButton != null) slotButton.onClick.AddListener(OnClickSlot);
    }

    public void OnToggleValueChanged(bool selected)
    {
        if (selected && (IsLocked || (!AllowEmptySelection && (currentItem == null || currentItem.itemData == null))))
        {
            if (slotToggle != null) slotToggle.SetIsOnWithoutNotify(false);
            return;
        }
        // Capture the clicked item before its action can consume stock and recycle this row.
        if (selectionTooltip != null) selectionTooltip.OnSlotSelectionChanged(selected);
        OnSelectionChanged(selected);
    }

    protected virtual void OnSelectionChanged(bool selected)
    {
        if (selected) OnClickSlot();
    }

    public void ResetSelection(bool hideTooltip = false)
    {
        if (slotToggle != null)
        {
            var group = slotToggle.group;
            bool allowSwitchOff = group != null && group.allowSwitchOff;
            try
            {
                // Explicit resets must clear selection even in a required group.
                if (group != null) group.allowSwitchOff = true;
                slotToggle.SetIsOnWithoutNotify(false);
            }
            finally
            {
                if (group != null) group.allowSwitchOff = allowSwitchOff;
            }
        }
        if (hideTooltip && selectionTooltip != null) selectionTooltip.ReleaseSelection(true);
    }

    public override void ClearSlot()
    {
        if (!KeepSelectionOnRefresh) ResetSelection();
        base.ClearSlot();
        RefreshInteraction();
    }

    public override void LockSlot()
    {
        base.LockSlot();
        ResetSelection();
        RefreshInteraction();
    }

    public override void SetSlot(InventoryItem item)
    {
        if (!KeepSelectionOnRefresh && currentItem?.itemData != item?.itemData) ResetSelection();
        base.SetSlot(item);
        RefreshInteraction();
    }

    public override void SetItemData(ItemData itemData, int amount = 1)
    {
        if (!KeepSelectionOnRefresh && currentItem?.itemData != itemData) ResetSelection();
        base.SetItemData(itemData, amount);
        RefreshInteraction();
    }

    protected void RefreshInteraction()
    {
        bool hasItem = currentItem != null && currentItem.itemData != null;
        if (slotButton != null) slotButton.interactable = !IsLocked && hasItem;
        if (slotToggle != null) slotToggle.interactable = !IsLocked && (hasItem || AllowEmptySelection);
    }

    protected virtual void OnDisable() => ResetSelection();

    protected virtual void OnDestroy()
    {
        if (slotToggle != null) slotToggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        if (slotButton != null) slotButton.onClick.RemoveListener(OnClickSlot);
    }

    public virtual void OnClickSlot() { }
}
