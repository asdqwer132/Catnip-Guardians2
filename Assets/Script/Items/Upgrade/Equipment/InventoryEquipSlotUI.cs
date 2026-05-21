using UnityEngine;

public class InventoryEquipSlotUI : ClickableItemSlotUI
{
    public override void OnClickSlot()
    {
        if (currentItem == null || currentItem.itemData == null)
        {
            Debug.LogWarning("장착할 아이템이 없습니다.");
            return;
        }

        if (EquipmentBagManager.instance == null)
        {
            Debug.LogWarning("EquipmentBagManager가 없습니다.");
            return;
        }

        EquipmentBagManager.instance.EquipItemToCurrentBag(currentItem);
    }
}