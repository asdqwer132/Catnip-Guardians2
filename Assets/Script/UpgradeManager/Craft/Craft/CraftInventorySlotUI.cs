using UnityEngine;

public class CraftInventorySlotUI : ClickableItemSlotUI
{
    // Optional explicit override for a slot that is not attached to the craft screen.
    [SerializeField] private ItemCraftManager itemCraftManager;

    public override void OnClickSlot()
    {
        if (currentItem == null || currentItem.itemData == null)
            return;

        ItemCraftManager newManager = itemCraftManager;
        if (newManager == null && CraftUIManager.Active != null)
            newManager = CraftUIManager.Active.Manager;

        if (newManager != null)
        {
            newManager.AddMaterial(currentItem.itemData);
            return;
        }

        if (GeneralCraftManager.instance != null && GeneralCraftManager.instance.currentManager != null)
        {
            GeneralCraftManager.instance.GetCurrentManager().AddMaterial(currentItem.itemData);
            return;
        }

        Debug.LogWarning("[CraftInventorySlotUI] No crafting manager is available.");
    }
}
