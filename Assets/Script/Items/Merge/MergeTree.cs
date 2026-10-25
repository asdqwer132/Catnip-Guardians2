using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable]
public class MergeSeries
{
    public int tier;
    public List<ItemRecipeData> itemRecipeDatas = new List<ItemRecipeData>();
}



// 사용자가 등록한 조합과 재고만 판정합니다. 같은 종류라는 이유로 자동 합성하지 않습니다.
public static class MergeTree
{
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
