using UnityEngine;

public class CraftMaterialSlotUI : ClickableItemSlotUI
{
    public GameObject Pannel;
    //UI 연결
    public override void OnClickSlot()
    {
        if (currentItem == null || currentItem.itemData == null)
        {
            Debug.LogWarning("반환할 조합 재료가 없습니다.");
            return;
        }

        if (ItemCombinationManager.instance == null)
        {
            Debug.LogWarning("ItemCombinationManager가 없습니다.");
            return;
        }

        ItemCombinationManager.instance.ReturnMaterial(currentItem.itemData, 1);
    }
}