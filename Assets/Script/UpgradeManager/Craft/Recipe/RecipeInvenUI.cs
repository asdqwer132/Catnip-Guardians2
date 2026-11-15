using System.Collections.Generic;
using UnityEngine;

public class RecipeInvenUI : ItemSearchFilterTargetUI
{
    [Header("Recipe Manager")]
    public ItemRecipeManager recipeManager;

    [Header("Recipe Inventory")]
    public Transform recipeSlotParent;
    public GameObject recipeSlotPrefab;

    [Header("Option")]
    public bool initOnStart = true;

    private bool isInitialized;

    private readonly List<ItemRecipeData> allRecipes = new List<ItemRecipeData>();
    private readonly List<ItemRecipeData> validRecipesCache = new List<ItemRecipeData>();

    private readonly List<GameObject> slotObjects = new List<GameObject>();
    private readonly List<RecipeSlotUI> slotUIs = new List<RecipeSlotUI>();

    private void Start()
    {
        if (initOnStart)
            Init();
    }

    public void Init()
    {
        if (isInitialized)
        {
            RefreshUI();
            return;
        }

        if (recipeManager == null)
            recipeManager = GetComponentInParent<ItemRecipeManager>();

        allRecipes.Clear();

        if (recipeManager == null)
        {
            Debug.LogWarning("[RecipeInvenUI] ItemRecipeManager가 없습니다.");
            RefreshUI();
            isInitialized = true;
            return;
        }

        List<ItemRecipeData> recipes = recipeManager.GetAllRecipe();

        if (recipes != null)
            allRecipes.AddRange(recipes);

        isInitialized = true;
        RefreshUI();
    }

    protected override void OnSearchFilterChanged()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        GetValidRecipes(validRecipesCache);
        RefreshRecipeInventory(validRecipesCache);
    }

    private void GetValidRecipes(List<ItemRecipeData> result)
    {
        result.Clear();

        for (int i = 0; i < allRecipes.Count; i++)
        {
            ItemRecipeData recipe = allRecipes[i];

            if (!IsRecipeVisible(recipe))
                continue;

            result.Add(recipe);
        }
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
        if (recipeSlotParent == null || recipeSlotPrefab == null)
            return;

        if (recipes == null)
            return;

        EnsureSlots(recipes.Count);

        for (int i = 0; i < slotObjects.Count; i++)
        {
            bool active = i < recipes.Count;

            slotObjects[i].SetActive(active);

            if (slotUIs[i] == null)
                continue;

            if (active)
                slotUIs[i].SetSlot(recipes[i], recipeManager);
        }
    }

    private void EnsureSlots(int count)
    {
        while (slotObjects.Count < count)
        {
            GameObject slotObj = Instantiate(recipeSlotPrefab, recipeSlotParent);

            RecipeSlotUI slotUI = slotObj.GetComponent<RecipeSlotUI>();

            slotObjects.Add(slotObj);
            slotUIs.Add(slotUI);
        }
    }
}