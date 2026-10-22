using UnityEngine;

public class SubPlantInventorySlotUI : ClickableItemSlotUI
{
    //UI ¿¬°á
    public override void OnClickSlot()
    {
        if (currentItem == null || currentItem.itemData == null)
            return;

        // A potion panel supplies its own recipe manager; legacy plant screens keep theirs.
        ItemCombinationPanelUI panel = GetComponentInParent<ItemCombinationPanelUI>(true);
        ItemRecipeManager manager = panel != null ? panel.Manager : SubPlantManager.instance;
        if (manager == null)
            return;

        manager.AddMaterial(currentItem.itemData);
    }
}
