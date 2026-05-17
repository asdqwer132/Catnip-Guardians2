using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentSlotUI : ClickableItemSlotUI,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler
{
    [Header("Drag Icon Option")]
    public Vector2 dragIconOffset = Vector2.zero;

    private int slotIndex;
    private EquipmentBagUI bagUI;

    private bool isDragging;
    private bool ignoreClick;

    private Image dragIconImage;
    private RectTransform dragIconRect;

    public int SlotIndex => slotIndex;
    public EquipmentBagUI BagUI => bagUI;

    protected override void Awake()
    {
        base.Awake();
    }

    public void Init(EquipmentBagUI ownerUI, Image sharedDragIconImage)
    {
        bagUI = ownerUI;
        dragIconImage = sharedDragIconImage;

        if (dragIconImage != null)
        {
            dragIconRect = dragIconImage.GetComponent<RectTransform>();
            dragIconImage.raycastTarget = false;
            dragIconImage.gameObject.SetActive(false);
        }
    }

    public void SetSlot(int index, InventoryItem item, EquipmentBagUI ownerUI)
    {
        slotIndex = index;
        bagUI = ownerUI;

        SetSlot(item);
    }

    public override void OnClickSlot()
    {
        if (ignoreClick)
            return;

        if (currentItem == null || currentItem.itemData == null)
            return;

        if (bagUI == null)
            return;

        bagUI.ClickSlot(slotIndex);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem == null || currentItem.itemData == null)
            return;

        if (currentItem.itemData.icon == null)
            return;

        if (dragIconImage == null || dragIconRect == null)
        {
            Debug.LogWarning("Drag Icon Image가 연결되지 않았습니다.");
            return;
        }

        isDragging = true;
        ignoreClick = true;

        ShowDragIcon(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        MoveDragIcon(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        isDragging = false;

        HideDragIcon();

        Invoke(nameof(ResetIgnoreClick), 0.05f);
    }

    public void OnDrop(PointerEventData eventData)
    {
        EquipmentSlotUI draggedSlot = eventData.pointerDrag?.GetComponent<EquipmentSlotUI>();

        if (draggedSlot == null)
            return;

        if (draggedSlot == this)
            return;

        if (bagUI == null)
            return;

        if (draggedSlot.BagUI != bagUI)
        {
            Debug.LogWarning("다른 가방 슬롯과는 교체할 수 없습니다.");
            return;
        }

        bagUI.SwapSlots(draggedSlot.SlotIndex, slotIndex);
    }

    private void ShowDragIcon(PointerEventData eventData)
    {
        dragIconImage.sprite = currentItem.itemData.icon;

        RectTransform myRect = GetComponent<RectTransform>();
        dragIconRect.sizeDelta = myRect.sizeDelta;

        dragIconImage.gameObject.SetActive(true);
        dragIconImage.transform.SetAsLastSibling();

        MoveDragIcon(eventData);
    }

    private void MoveDragIcon(PointerEventData eventData)
    {
        if (dragIconRect == null)
            return;

        dragIconRect.position = eventData.position + dragIconOffset;
    }

    private void HideDragIcon()
    {
        if (dragIconImage == null)
            return;

        dragIconImage.gameObject.SetActive(false);
        dragIconImage.sprite = null;
    }

    private void ResetIgnoreClick()
    {
        ignoreClick = false;
    }
}