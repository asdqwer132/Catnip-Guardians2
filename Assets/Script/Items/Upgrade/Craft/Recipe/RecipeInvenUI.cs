using System.Collections.Generic;
using UnityEngine;

public class RecipeInvenUI : ItemSearchFilterTargetUI
{
    [Header("Recipe Manager")]
    public ItemRecipeManager recipeManager;

    [Header("Detail Inventory")]
    public Transform detailSlotParent;
    public GameObject detailSlotPrefab;

    [Header("Option")]
    public bool initOnStart = true;

    private readonly List<ItemRecipeData> allRecipes = new List<ItemRecipeData>();

    private void Start()
    {
        if (initOnStart)
            Init();
    }

    public void Init()
    {
        if (recipeManager == null)
            recipeManager = GetComponentInParent<ItemRecipeManager>();

        allRecipes.Clear();

        if (recipeManager == null)
        {
            Debug.LogWarning("[RecipeInvenUI] ItemRecipeManager가 없습니다.");
            RefreshUI();
            return;
        }

        List<ItemRecipeData> recipes = recipeManager.GetAllRecipe();

        if (recipes != null)
            allRecipes.AddRange(recipes);

        RefreshUI();
    }

    protected override void OnSearchFilterChanged()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        List<ItemRecipeData> validRecipes = GetValidRecipes();
        RefreshRecipeInventory(validRecipes);
    }

    private List<ItemRecipeData> GetValidRecipes()
    {
        List<ItemRecipeData> result = new List<ItemRecipeData>();

        for (int i = 0; i < allRecipes.Count; i++)
        {
            ItemRecipeData recipe = allRecipes[i];

            if (!IsRecipeVisible(recipe))
                continue;

            result.Add(recipe);
        }

        return result;
    }

    private bool IsRecipeVisible(ItemRecipeData recipe)
    {
        if (recipe == null)
            return false;

        if (recipe.resultItem == null)
            return false;

        return IsItemDataVisible(recipe.resultItem);
    }

    private void RefreshRecipeInventory(List<ItemRecipeData> recipes)
    {
        if (detailSlotParent == null || detailSlotPrefab == null)
            return;

        ClearChildren(detailSlotParent);

        if (recipes == null)
            return;

        for (int i = 0; i < recipes.Count; i++)
        {
            GameObject slotObj = Instantiate(
                detailSlotPrefab,
                detailSlotParent
            );

            RecipeSlotUI slotUI = slotObj.GetComponent<RecipeSlotUI>();

            if (slotUI != null)
                slotUI.SetSlot(recipes[i], recipeManager);
        }
    }

    private void ClearChildren(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}