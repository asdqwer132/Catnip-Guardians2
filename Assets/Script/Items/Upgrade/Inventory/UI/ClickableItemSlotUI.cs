using UnityEngine;
using UnityEngine.UI;

public class ClickableItemSlotUI : BaseItemSlotUI
{
    [Header("Button")]
    public Button slotButton;

    protected virtual void Awake()
    {
        if (slotButton == null)
            slotButton = GetComponent<Button>();

        if (slotButton != null)
            slotButton.onClick.AddListener(OnClickSlot);
        else
            Debug.LogWarning(name + " 슬롯에 Button이 없습니다.");
    }

    public override void ClearSlot()
    {
        base.ClearSlot();
        if (slotButton != null)
            slotButton.interactable = false;
    }
    public override void LockSlot()
    {
        base.LockSlot();
        if (slotButton != null)
            slotButton.interactable = false;
    }
    public override void SetSlot(InventoryItem item)
    {
        base.SetSlot(item);
        if (slotButton != null)
            slotButton.interactable = item != null && item.itemData != null;

    }


    protected virtual void OnDestroy()
    {
        if (slotButton != null)
            slotButton.onClick.RemoveListener(OnClickSlot);
    }

    public virtual void OnClickSlot() { }
}