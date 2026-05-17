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

    protected virtual void OnDestroy()
    {
        if (slotButton != null)
            slotButton.onClick.RemoveListener(OnClickSlot);
    }

    public virtual void OnClickSlot()
    {
        // 클릭 가능 슬롯의 기본 동작 없음
    }
}