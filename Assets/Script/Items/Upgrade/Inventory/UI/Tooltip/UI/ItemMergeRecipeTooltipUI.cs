using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The selected inventory item's details and the merge recipes using it.</summary>
public sealed class ItemMergeRecipeTooltipUI : ItemTooltipUI
{
    [Header("Merge Recipes")]
    public ItemRecipeData[] recipes;
    public Transform recipeSlotParent;
    public MergeRecipeSlotUI recipeSlotPrefab;
    public ScrollRect recipeScroll;
    [Tooltip("Existing scene rows reused before instantiating more copies of the slot prefab.")]
    [SerializeField] private List<MergeRecipeSlotUI> recipeSlots = new List<MergeRecipeSlotUI>();

    private readonly List<InventoryItem> requiredMaterials = new List<InventoryItem>();
    private readonly HashSet<ItemRecipeData> visitedRecipes = new HashSet<ItemRecipeData>();
    private ItemData selectedItem;

    public IReadOnlyList<MergeRecipeSlotUI> RecipeSlots => recipeSlots;

    protected override void OnDataChanged(DefinedData data)
    {
        base.OnDataChanged(data);
        var item = data as ItemData ?? (data as TooltipData)?.sourceData as ItemData;
        bool changed = selectedItem != item;
        selectedItem = item;
        RefreshRecipes();
        if (changed && recipeScroll != null)
        {
            recipeScroll.StopMovement();
            recipeScroll.verticalNormalizedPosition = 1f;
        }
    }

    public void RefreshRecipes()
    {
        visitedRecipes.Clear();
        int count = 0;
        if (selectedItem != null && recipes != null)
        {
            foreach (var recipe in recipes)
            {
                // Remove repeated references, but retain distinct recipes even
                // when they have the same ingredients or the same result item.
                if (recipe == null || !visitedRecipes.Add(recipe)
                    || !recipe.TryGetRequiredMaterials(requiredMaterials)
                    || !requiredMaterials.Exists(material => material.itemData == selectedItem)) continue;
                var slot = GetRecipeSlot(count);
                if (slot == null) continue;
                slot.Bind(recipe, selectedItem);
                slot.gameObject.SetActive(true);
                count++;
            }
        }
        for (int i = count; i < recipeSlots.Count; i++)
        {
            if (recipeSlots[i] == null) continue;
            recipeSlots[i].Clear();
            recipeSlots[i].gameObject.SetActive(false);
        }
    }

    private MergeRecipeSlotUI GetRecipeSlot(int index)
    {
        if (index < recipeSlots.Count) return recipeSlots[index];
        if (recipeSlotPrefab == null || recipeSlotParent == null) return null;
        var slot = Instantiate(recipeSlotPrefab, recipeSlotParent);
        recipeSlots.Add(slot);
        return slot;
    }
}
