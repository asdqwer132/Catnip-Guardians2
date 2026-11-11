using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MergeRecipeBook", menuName = "Items/Merge/Recipe Book")]
public sealed class MergeRecipeBook : ScriptableObject
{
    public List<MergeItem> items = new List<MergeItem>();
    public List<ItemRecipeData> recipes = new List<ItemRecipeData>();
    public List<ItemRecipeData> Recipes => recipes;

    public bool TryGetRecipe(ItemData first, ItemData second, out ItemRecipeData recipe)
    {
        recipe = Recipes.Find(candidate => MergeTree.MatchesPair(candidate, first, second));
        return recipe != null;
    }

    public bool TryGetRecipe(IReadOnlyList<ItemData> ingredients, out ItemRecipeData recipe)
    {
        recipe = Recipes.Find(candidate => MergeTree.MatchesIngredients(candidate, ingredients));
        return recipe != null;
    }

    public MergeItem GetDefinition(ItemData item)
    {
        if (item == null || items == null) return null;
        foreach (var definition in items)
            if (definition != null && definition.item == item) return definition;
        return null;
    }
}
