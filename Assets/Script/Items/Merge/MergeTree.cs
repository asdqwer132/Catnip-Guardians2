using System;
using System.Collections.Generic;

[Serializable]
public class MergeSeries
{
    public int tier;
    public List<ItemRecipeData> itemRecipeDatas = new List<ItemRecipeData>();
}



// 사용자가 등록한 조합과 재고만 판정합니다. 같은 종류라는 이유로 자동 합성하지 않습니다.
public static class MergeTree
{
    // 모든 레시피 재료를 종류별 필요 수량으로 정규화합니다. 재료 개수 제한은 없습니다.
    public static bool TryGetIngredients(ItemRecipeData recipe, out Dictionary<ItemData, int> ingredients)
    {
        ingredients = new Dictionary<ItemData, int>();
        if (recipe == null || recipe.resultItem == null || recipe.materials == null || recipe.materials.Length == 0) return false;
        foreach (var material in recipe.materials)
        {
            if (material == null || material.itemData == null || material.amount < 1) return false;
            ingredients.TryGetValue(material.itemData, out int amount);
            if (material.amount > int.MaxValue - amount) return false;
            ingredients[material.itemData] = amount + material.amount;
        }
        return true;
    }
    public static bool SameIngredients(IDictionary<ItemData, int> first, IDictionary<ItemData, int> second)
    {
        if (first.Count != second.Count) return false;
        foreach (var entry in first)
            if (!second.TryGetValue(entry.Key, out int amount) || amount != entry.Value) return false;
        return true;
    }
    public static bool MatchesIngredients(ItemRecipeData recipe, IReadOnlyList<ItemData> items)
    {
        if (items == null || !TryGetIngredients(recipe, out var required)) return false;
        var supplied = new Dictionary<ItemData, int>();
        foreach (var item in items)
        {
            if (item == null) return false;
            supplied.TryGetValue(item, out int amount); supplied[item] = amount + 1;
        }
        return SameIngredients(required, supplied);
    }
    public static bool TryGetRecipe(IList<MergeSeries> tiers, IReadOnlyList<ItemData> ingredients, out ItemRecipeData recipe)
    {
        recipe = null;
        if (tiers == null) return false;
        foreach (var tier in tiers)
        {
            if (tier?.itemRecipeDatas == null) continue;
            foreach (var candidate in tier.itemRecipeDatas)
                if (MatchesIngredients(candidate, ingredients)) { recipe = candidate; return true; }
        }
        return false;
    }
    public static bool MatchesPair(ItemRecipeData recipe, ItemData first, ItemData second)
    {
        if (recipe == null || recipe.resultItem == null || recipe.materials == null || first == null || second == null) return false;
        ItemData a = null, b = null;
        int count = 0;
        foreach (var material in recipe.materials)
        {
            if (material == null || material.itemData == null || material.amount <= 0 || material.amount > 2 - count) return false;
            for (int i = 0; i < material.amount; i++)
                if (count++ == 0) a = material.itemData; else b = material.itemData;
        }
        return count == 2 && ((a == first && b == second) || (a == second && b == first));
    }
    public static bool TryGetRecipe(IList<MergeSeries> tiers, ItemData first, ItemData second, out ItemRecipeData recipe)
    {
        recipe = null;
        if (tiers == null) return false;
        foreach (var tier in tiers)
        {
            if (tier?.itemRecipeDatas == null) continue;
            foreach (var candidate in tier.itemRecipeDatas)
                if (MatchesPair(candidate, first, second)) { recipe = candidate; return true; }
        }
        return false;
    }
    public static bool IsAvailable(MergeItemMaterial material)
        => material != null && material.mergeItem != null && material.mergeItem.item != null && material.IsAvailable();
    public static MergeItemMaterial NextMaterial(IList<MergeItemMaterial> materials, MergeItemMaterial current)
    {
        if (materials == null || materials.Count == 0) return null;
        int start = materials.IndexOf(current);
        for (int offset = 1; offset <= materials.Count; offset++)
        {
            var candidate = materials[(start + offset) % materials.Count];
            if (IsAvailable(candidate)) return candidate;
        }
        return null;
    }
}
