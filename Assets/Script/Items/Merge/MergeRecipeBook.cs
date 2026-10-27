using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MergeRecipeBook", menuName = "04.Items/Merge/Recipe Book")]
public sealed class MergeRecipeBook : ScriptableObject
{
    public List<MergeItem> items = new List<MergeItem>();
    public List<MergeSeries> tiers = new List<MergeSeries>();

    public bool TryGetRecipe(ItemData first, ItemData second, out ItemRecipeData recipe)
        => MergeTree.TryGetRecipe(tiers, first, second, out recipe);

    public bool TryGetRecipe(IReadOnlyList<ItemData> ingredients, out ItemRecipeData recipe)
        => MergeTree.TryGetRecipe(tiers, ingredients, out recipe);

    public MergeItem GetDefinition(ItemData item)
    {
        if (item == null || items == null) return null;
        foreach (var definition in items)
            if (definition != null && definition.item == item) return definition;
        return null;
    }
}
