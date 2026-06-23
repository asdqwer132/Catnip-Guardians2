using UnityEngine;
using UnityEngine.EventSystems;

public class ItemTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Tooltip")]
    public ItemTooltipUI tooltipUI;

    [Header("Slot")]
    public BaseItemSlotUI slot;

    private void Awake()
    {
        if (slot == null)
            slot = GetComponent<BaseItemSlotUI>();

    }

    public void Init(ItemTooltipUI tooltipTrigger)
    {
        tooltipUI = tooltipTrigger;
    }

    private void OnDisable()
    {
        if (tooltipUI != null && slot != null)
            tooltipUI.Hide(slot);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipUI == null || slot == null)
            return;

        tooltipUI.Show(slot);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipUI == null || slot == null)
            return;

        tooltipUI.Hide(slot);
    }
}