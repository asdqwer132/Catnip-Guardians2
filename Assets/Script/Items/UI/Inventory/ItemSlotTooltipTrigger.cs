using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlotTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private BaseItemSlotUI slotUI;

    private void Awake()
    {
        slotUI = GetComponent<BaseItemSlotUI>();

        if (slotUI == null)
            slotUI = GetComponentInParent<BaseItemSlotUI>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slotUI == null)
            return;

        InventoryItem item = slotUI.GetCurrentItem();

        if (item == null || item.itemData == null)
            return;

        if (ItemTooltipUI.instance == null)
            return;

        ItemTooltipUI.instance.Show(item);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemTooltipUI.instance == null)
            return;

        ItemTooltipUI.instance.Hide();
    }
}