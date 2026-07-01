using System.Collections.Generic;
using UnityEngine;

public class SubPlantRecipeSlotUI : RecipeSlotUI
{
    public InventorySlotUI[] materialSlots;

    public override void SetSlot(ItemRecipeData item, ItemRecipeManager manager)
    {
        base.SetSlot(item, manager);
        RecipeMaterial[] materials = item.materials;
        for (int i = 0; i < materialSlots.Length ; i++)
        {
            if (i < materials.Length)
            {
                materialSlots[i].gameObject.SetActive(true);
                materialSlots[i].SetSlot(new InventoryItem(materials[i].itemData, 1));
            }
            else
                materialSlots[i].gameObject.SetActive(false);
        }
    }
}
