using UnityEngine;

public class CraftInventorySlotUI : ClickableItemSlotUI
{
    public override void OnClickSlot()
    {
        if (currentItem == null || currentItem.itemData == null)
        {
            Debug.LogWarning("조합에 넣을 아이템이 없습니다.");
            return;
        }

        if (ItemCombinationManager.instance == null)
        {
            Debug.LogWarning("ItemCombinationManager가 없습니다.");
            return;
        }

        ItemCombinationManager.instance.AddMaterial(currentItem.itemData);
    }
}