using System;
using TMPro;
using UnityEngine;

public class BagEquipmentSlotUI : ClickableItemSlotUI
{
    public TextMeshProUGUI slotNumberText;
    private Action clicked;
    private Color normalIconColor = Color.white;
    protected override bool AllowEmptySelection => true;
    protected override bool KeepSelectionOnRefresh => true;

    public void Bind(Action action, int number = 0, TooltipUI tooltip = null)
    {
        clicked = action;
        if (tooltip != null && TryGetComponent<ItemTooltipTrigger>(out var trigger)) trigger.Init(tooltip);
        if (slotNumberText != null && number > 0) slotNumberText.text = number.ToString();
        if (icon != null) normalIconColor = icon.color;
    }

    public virtual void Show(DefaultData data, bool selected, bool isLocked = false)
    {
        ClearSlot();
        if (data != null)
        {
            SetData(data);
            if (ready != null) ready.SetActive(false);
        }
        SetAmountVisible(false);
        IsLocked = isLocked;
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

    public override void OnClickSlot()
    {
        if (!IsLocked) clicked?.Invoke();
    }
}
