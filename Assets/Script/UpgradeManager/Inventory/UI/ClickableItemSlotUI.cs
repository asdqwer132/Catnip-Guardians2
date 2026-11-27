using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ClickableItemSlotUI : BaseItemSlotUI
{
    [Header("Interaction")]
    public Button slotButton;
    public Toggle slotToggle;
    private static readonly List<RaycastResult> ClickHits = new();
    private ItemTooltipTrigger selectionTooltip;
    protected virtual bool ClearSelectionOnOutsideClick => true;
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
        OnSelectionChanged(selected);
    }

    protected virtual void OnSelectionChanged(bool selected)
    {
        if (selected) OnClickSlot();
    }

    protected void LateUpdate()
    {
        if (!ClearSelectionOnOutsideClick || slotToggle == null || !slotToggle.isOn) return;

        var mouse = Mouse.current;
        var eventSystem = EventSystem.current;
        if (mouse == null || !mouse.leftButton.wasReleasedThisFrame || eventSystem == null ||
            HitsToggle(eventSystem, mouse.position.ReadValue())) return;

        SetToggleOff(notify: true);
        selectionTooltip?.ReleaseSelection(true);
    }

    private static bool HitsToggle(EventSystem eventSystem, Vector2 position)
    {
        eventSystem.RaycastAll(new PointerEventData(eventSystem) { position = position }, ClickHits);
        bool hitsToggle = false;
        foreach (var hit in ClickHits)
        {
            if (hit.module is not GraphicRaycaster) continue;

            hitsToggle = hit.gameObject.GetComponentInParent<Toggle>() != null;
            break;
        }
        ClickHits.Clear();
        return hitsToggle;
    }

    public void ResetSelection(bool hideTooltip = false)
    {
        SetToggleOff(notify: false);
        if ((slotToggle != null || hideTooltip) && selectionTooltip != null) selectionTooltip.ReleaseSelection(true);
    }

    private void SetToggleOff(bool notify)
    {
        if (slotToggle == null) return;

        var group = slotToggle.group;
        bool allowSwitchOff = group != null && group.allowSwitchOff;
        try
        {
            // Explicit resets must clear selection even in a required group.
            if (group != null) group.allowSwitchOff = true;
            if (notify) slotToggle.isOn = false;
            else slotToggle.SetIsOnWithoutNotify(false);
        }
        finally
        {
            if (group != null) group.allowSwitchOff = allowSwitchOff;
        }
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

    protected override void OnDisable() { ResetSelection(); base.OnDisable(); }

    protected virtual void OnDestroy()
    {
        if (slotToggle != null) slotToggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        if (slotButton != null) slotButton.onClick.RemoveListener(OnClickSlot);
    }

    public event System.Action<InventoryItem> Clicked;
    public virtual void OnClickSlot() => Clicked?.Invoke(currentItem);
}
