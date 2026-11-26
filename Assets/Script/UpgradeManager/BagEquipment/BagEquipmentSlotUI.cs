using System;
using TMPro;
using UnityEngine;

public class BagEquipmentSlotUI : ClickableItemSlotUI
{
    public TextMeshProUGUI slotNumberText;
    [UnityEngine.SerializeField] private UnityEngine.UI.Image selectionBackground;
    private Action clicked;
    private Action deselected;
    private Color normalIconColor = Color.white;
    private Color normalBackgroundColor;
    private bool capturedColors;
    protected override bool AllowEmptySelection => true;
    protected override bool KeepSelectionOnRefresh => true;

    public void Bind(Action action, int number = 0, TooltipUI tooltip = null, Action onDeselected = null)
    {
        clicked = action;
        deselected = onDeselected;
        if (tooltip != null && TryGetComponent<ItemTooltipTrigger>(out var trigger)) trigger.Init(tooltip);
        if (slotNumberText != null && number > 0) slotNumberText.text = number.ToString();
        if (capturedColors) return;
        capturedColors = true;
        if (icon != null) normalIconColor = icon.color;
        if (selectionBackground != null) normalBackgroundColor = selectionBackground.color;
    }

    public virtual void Show(DefaultData data, bool selected, bool isLocked = false)
        => Show(data, selected, isLocked, null);

    public void Show(DefaultData data, bool selected, Sprite displayIcon)
        => Show(data, selected, false, displayIcon);

    private void Show(DefaultData data, bool selected, bool isLocked, Sprite displayIcon)
    {
        ClearSlot();
        if (data != null)
        {
            SetData(data);
            if (displayIcon != null && icon != null)
            {
                icon.sprite = displayIcon;
                icon.enabled = true;
            }
            if (ready != null) ready.SetActive(false);
        }
        SetAmountVisible(false);
        IsLocked = isLocked;
        if (selectionBackground != null)
            selectionBackground.color = selected && !isLocked
                ? Color.Lerp(normalBackgroundColor, new Color(0.93f, 0.71f, 0.40f, normalBackgroundColor.a), 0.45f)
                : normalBackgroundColor;
        if (locked != null) locked.SetActive(isLocked);
        if (icon != null) icon.color = isLocked
            ? new Color(normalIconColor.r, normalIconColor.g, normalIconColor.b, normalIconColor.a * .4f)
            : normalIconColor;
        if (slotToggle != null)
        {
            slotToggle.interactable = !isLocked;
            if (selected && !isLocked) slotToggle.SetIsOnWithoutNotify(true);
            else ResetSelection();
        }
        if (slotButton != null) slotButton.interactable = !isLocked;
    }

    protected override void OnSelectionChanged(bool selected)
    {
        if (selected) OnClickSlot();
        else if (slotToggle == null || slotToggle.group == null || !slotToggle.group.AnyTogglesOn())
            deselected?.Invoke();
    }

    public override void OnClickSlot()
    {
        if (!IsLocked) clicked?.Invoke();
    }
}
