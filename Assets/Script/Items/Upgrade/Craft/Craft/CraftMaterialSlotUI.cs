using TMPro;
using UnityEngine;

public class CraftMaterialSlotUI : ClickableItemSlotUI
{
    // Kept with the original spelling so existing scene assignments remain intact.
    public GameObject Pannel;

    private ItemCraftManager craftManager;

    public void Bind(ItemCraftManager manager)
    {
        craftManager = manager;
    }

    public void SetAmount(int amount)
    {
        if (amountText != null)
            amountText.text = amount > 0 ? amount.ToString() : string.Empty;
    }

    public override void OnClickSlot()
    {
        if (currentItem == null || currentItem.itemData == null)
            return;

        ItemCraftManager manager = craftManager != null ? craftManager : GeneralCraftManager.instance.GetCurrentManager();
        if (manager == null)
        {
            Debug.LogWarning("[CraftMaterialSlotUI] ItemCraftManager is missing.");
            return;
        }

        // Each click returns one item. The slot disappears only when its amount reaches zero.
        manager.ReturnMaterial(currentItem.itemData, 1);
    }
}
