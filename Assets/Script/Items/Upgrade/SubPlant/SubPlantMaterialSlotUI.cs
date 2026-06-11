using UnityEngine;

public class SubPlantMaterialSlotUI : ClickableItemSlotUI
{
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

        SubPlantManager.instance.ReturnMaterial(currentItem.itemData, 1);
    }
}