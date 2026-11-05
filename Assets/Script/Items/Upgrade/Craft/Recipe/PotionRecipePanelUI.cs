using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Owns the recipe view only. Crafting and inventory transfers remain in the manager.
public sealed class PotionRecipePanelUI : ItemSearchFilterTargetUI
{
    public ItemCombinationManager manager;
    public InventoryManager inventory;
    public PotionRecipeHistory history;
    public Transform recipeContent;
    public PotionRecipeSlotUI recipeSlotPrefab;
    public ItemDefinitionUI details;
    public Transform materialsContent;
    public PotionRecipeMaterialUI materialSlotPrefab;
    public Button goToCraftingButton;
    public Toggle craftingTab;
    public ItemCombinationPanelUI craftingPanel;
    private readonly List<PotionRecipeSlotUI> slots = new List<PotionRecipeSlotUI>();
    private readonly List<PotionRecipeMaterialUI> materialRows = new List<PotionRecipeMaterialUI>();
    private readonly List<InventoryItem> requirements = new List<InventoryItem>();
    private readonly List<ItemRecipeData> visibleRecipes = new List<ItemRecipeData>();
    public ItemRecipeData SelectedRecipe { get; private set; }
    public IReadOnlyList<ItemRecipeData> VisibleRecipes => visibleRecipes;

    private void OnEnable()
    {
        if (history != null) history.Changed += RefreshRecipes;
        if (inventory != null) inventory.onInventoryChanged += RefreshAvailability;
        if (manager != null) manager.onMaterialChanged += RefreshAvailability;
        if (goToCraftingButton != null) goToCraftingButton.onClick.AddListener(GoToCrafting);
        RefreshRecipes();
    }
    private void Start() => RefreshRecipes();
    private void OnDisable()
    {
        if (history != null) history.Changed -= RefreshRecipes;
        if (inventory != null) inventory.onInventoryChanged -= RefreshAvailability;
        if (manager != null) manager.onMaterialChanged -= RefreshAvailability;
        if (goToCraftingButton != null) goToCraftingButton.onClick.RemoveListener(GoToCrafting);
    }
    protected override void OnSearchFilterChanged() => RefreshRecipes();
    public void RefreshRecipes()
    {
        visibleRecipes.Clear();
        var seen = new HashSet<ItemData>();
        if (manager != null && history != null)
            foreach (var recipe in manager.GetAllRecipe())
                if (recipe?.resultItem != null && history.HasCrafted(recipe.resultItem) &&
                    IsItemDataVisible(recipe.resultItem) && seen.Add(recipe.resultItem)) visibleRecipes.Add(recipe);
        if (recipeContent != null && recipeSlotPrefab != null)
        {
            while (slots.Count < visibleRecipes.Count) slots.Add(Instantiate(recipeSlotPrefab, recipeContent));
            for (int i = 0; i < slots.Count; i++)
            {
                bool active = i < visibleRecipes.Count;
                slots[i].gameObject.SetActive(active);
                if (active) slots[i].Bind(visibleRecipes[i], SelectRecipe);
            }
        }
        if (SelectedRecipe != null && !visibleRecipes.Contains(SelectedRecipe)) SelectRecipe(null);
        else if (SelectedRecipe == null) SelectRecipe(null);
        else RefreshAvailability();
    }
    public void SelectRecipe(ItemRecipeData recipe)
    {
        SelectedRecipe = recipe != null && visibleRecipes.Contains(recipe) ? recipe : null;
        requirements.Clear();
        if (SelectedRecipe != null && !SelectedRecipe.TryGetRequiredMaterials(requirements)) SelectedRecipe = null;
        if (details != null) details.SetData(SelectedRecipe != null ? SelectedRecipe.resultItem : null);
        if (materialsContent != null && materialSlotPrefab != null)
        {
            while (materialRows.Count < requirements.Count) materialRows.Add(Instantiate(materialSlotPrefab, materialsContent));
            for (int i = 0; i < materialRows.Count; i++)
            {
                bool active = i < requirements.Count;
                materialRows[i].gameObject.SetActive(active);
                if (active)
                {
                    var entry = requirements[i];
                    materialRows[i].Bind(entry.itemData, entry.amount, manager != null ? manager.GetAvailableMaterialCount(entry.itemData) : 0);
                }
            }
        }
        RefreshAvailability();
    }
    public void RefreshAvailability()
    {
        foreach (var row in materialRows)
            if (row.gameObject.activeSelf) row.SetCounts(manager != null ? manager.GetAvailableMaterialCount(row.Item) : 0);
        if (goToCraftingButton != null) goToCraftingButton.interactable =
            SelectedRecipe != null && manager != null && manager.CanPrepareRecipe(SelectedRecipe);
    }
    public void GoToCrafting()
    {
        // Revalidate on click, even if inventory changed since the last UI refresh.
        if (SelectedRecipe == null || manager == null || !manager.TryPrepareRecipe(SelectedRecipe))
        { RefreshAvailability(); return; }
        if (craftingTab != null)
        {
            if (craftingTab.isOn) craftingTab.onValueChanged.Invoke(true);
            else craftingTab.isOn = true;
        }
        if (craftingPanel != null) craftingPanel.RefreshUI();
    }
}
