using System.Collections.Generic;
using UnityEngine;

/// <summary>Binds a recipe to the authored material + materials = result row.</summary>
public sealed class MergeRecipeSlotUI : MonoBehaviour
{
    public InventorySlotUI resultSlot;
    public ItemDefinitionUI selectedMaterial;
    public Transform additionalMaterialParent;
    public ItemDefinitionUI additionalMaterialTemplate;
    [SerializeField] private List<ItemDefinitionUI> additionalMaterialSlots = new List<ItemDefinitionUI>();

    private readonly List<InventoryItem> requiredMaterials = new List<InventoryItem>();

    public ItemRecipeData Recipe { get; private set; }
    public IReadOnlyList<ItemDefinitionUI> AdditionalMaterialSlots => additionalMaterialSlots;

    public bool Bind(ItemRecipeData recipe, ItemData selectedItem)
    {
        Clear();
        if (selectedItem == null || recipe == null || !recipe.TryGetRequiredMaterials(requiredMaterials)
            || !requiredMaterials.Exists(material => material.itemData == selectedItem)) return false;

        Recipe = recipe;
        if (selectedMaterial != null) selectedMaterial.SetData(selectedItem);
        if (resultSlot != null) resultSlot.SetItemData(recipe.resultItem);

        // Main displays one selected item. Repeat the existing ingredient template for
        // every remaining unit, including recipes requiring several of the same item.
        int index = 0;
        foreach (var material in requiredMaterials)
        {
            int amount = material.amount - (material.itemData == selectedItem ? 1 : 0);
            for (int unit = 0; unit < amount; unit++)
            {
                var slot = GetAdditionalMaterial(index++);
                if (slot == null) continue;
                slot.SetData(material.itemData);
                slot.gameObject.SetActive(true);
            }
        }
        return true;
    }

    public void Clear()
    {
        Recipe = null;
        if (resultSlot != null)
        {
            resultSlot.ResetSelection();
            resultSlot.SetSlot(null);
        }
        if (selectedMaterial != null) selectedMaterial.ClearData();
        foreach (var slot in additionalMaterialSlots)
        {
            if (slot == null) continue;
            slot.ClearData();
            slot.gameObject.SetActive(false);
        }
    }

    private ItemDefinitionUI GetAdditionalMaterial(int index)
    {
        if (index < additionalMaterialSlots.Count) return additionalMaterialSlots[index];
        if (additionalMaterialTemplate == null || additionalMaterialParent == null) return null;
        var slot = Instantiate(additionalMaterialTemplate, additionalMaterialParent);
        additionalMaterialSlots.Add(slot);
        return slot;
    }
}
