using System;
using System.Collections.Generic;

public static partial class Runner
{
    private static ItemRecipeData MergeRecipe(ItemData result, params RecipeMaterial[] materials)
        => new ItemRecipeData { resultItem = result, materials = materials };
    private static RecipeMaterial MergeIngredient(ItemData item, int amount = 1)
        => new RecipeMaterial { itemData = item, amount = amount };
    private static void RunMergeGameChecks()
    {
        Check("merge recipes match registered mixed pairs in both contact orders", () => {
            var a = new ItemData(); var b = new ItemData(); var c = new ItemData();
            var recipe = MergeRecipe(c, MergeIngredient(a), MergeIngredient(b));
            var tiers = new List<MergeSeries> { new MergeSeries { tier = 4, itemRecipeDatas = new List<ItemRecipeData> { recipe } } };
            True(MergeTree.TryGetRecipe(tiers, a, b, out var result) && result == recipe);
            True(MergeTree.TryGetRecipe(tiers, b, a, out result) && result == recipe);
        });
        Check("equal items never merge implicitly and only an explicit two-copy recipe permits it", () => {
            var a = new ItemData(); var b = new ItemData();
            var recipes = new List<MergeSeries> { new MergeSeries() };
            True(!MergeTree.TryGetRecipe(recipes, a, a, out _));
            var recipe = MergeRecipe(b, MergeIngredient(a, 2)); recipes[0].itemRecipeDatas.Add(recipe);
            True(MergeTree.TryGetRecipe(recipes, a, a, out _));
            recipe.materials = new[] { MergeIngredient(a), MergeIngredient(a) };
            True(MergeTree.MatchesPair(recipe, a, a));
        });
        Check("merge recipes reject invalid counts null data and more than two ingredients", () => {
            var a = new ItemData(); var b = new ItemData();
            foreach (int amount in new[] { -1, 0, 3, 4, int.MaxValue })
                True(!MergeTree.MatchesPair(MergeRecipe(b, MergeIngredient(a, amount)), a, a));
            True(!MergeTree.MatchesPair(MergeRecipe(null, MergeIngredient(a, 2)), a, a));
            True(!MergeTree.MatchesPair(MergeRecipe(b, MergeIngredient(null, 2)), a, a));
            True(!MergeTree.MatchesPair(MergeRecipe(b, MergeIngredient(a), null), a, a));
            True(!MergeTree.MatchesPair(MergeRecipe(b, MergeIngredient(a), MergeIngredient(a), MergeIngredient(a)), a, a));
            True(!MergeTree.MatchesPair(MergeRecipe(b, MergeIngredient(a)), a, a));
            True(!MergeTree.MatchesPair(null, a, a));
        });
        Check("merge tier and per-tier recipe lists can exceed the old ten-by-three limit", () => {
            var a = new ItemData(); var b = new ItemData(); var tiers = new List<MergeSeries>();
            for (int i = 0; i < 15; i++) tiers.Add(new MergeSeries());
            for (int i = 0; i < 6; i++) tiers[14].itemRecipeDatas.Add(null);
            var recipe = MergeRecipe(b, MergeIngredient(a, 2)); tiers[14].itemRecipeDatas.Add(recipe);
            True(MergeTree.TryGetRecipe(tiers, a, a, out var found) && found == recipe);
            tiers[14].itemRecipeDatas.Clear(); True(!MergeTree.TryGetRecipe(tiers, a, a, out _));
        });
        Check("merge recipe lookup tolerates incomplete tiers and preserves first registered match", () => {
            var a = new ItemData(); var b = new ItemData(); var c = new ItemData();
            var first = MergeRecipe(b, MergeIngredient(a, 2)); var second = MergeRecipe(c, MergeIngredient(a, 2));
            var tiers = new List<MergeSeries> { null, new MergeSeries { itemRecipeDatas = null },
                new MergeSeries { itemRecipeDatas = new List<ItemRecipeData> { first, second } } };
            True(MergeTree.TryGetRecipe(tiers, a, a, out var found) && found == first);
            True(!MergeTree.TryGetRecipe(null, a, a, out _)); True(!MergeTree.TryGetRecipe(tiers, null, a, out _));
        });
        Check("material selection cycles and returns the actual stock entry without granting inventory", () => {
            var a = new MergeItemMaterial { MergeItem = new MergeItem { item = new ItemData() }, count = 2 };
            var b = new MergeItemMaterial { MergeItem = new MergeItem { item = new ItemData() }, count = 3 };
            var stock = new List<MergeItemMaterial> { a, b };
            True(MergeTree.NextMaterial(stock, null) == a && MergeTree.NextMaterial(stock, a) == b && MergeTree.NextMaterial(stock, b) == a);
            MergeTree.NextMaterial(stock, b).count--;
            True(a.count == 1 && b.count == 3);
        });
        Check("material selection skips empty invalid and depleted entries and handles full exhaustion", () => {
            var available = new MergeItemMaterial { MergeItem = new MergeItem { item = new ItemData() }, count = 1 };
            var stock = new List<MergeItemMaterial> { null, new MergeItemMaterial(),
                new MergeItemMaterial { MergeItem = new MergeItem(), count = 9 },
                new MergeItemMaterial { MergeItem = new MergeItem { item = new ItemData() }, count = -1 }, available };
            True(MergeTree.NextMaterial(stock, null) == available && MergeTree.NextMaterial(stock, available) == available);
            available.count = 0; True(MergeTree.NextMaterial(stock, available) == null);
            True(MergeTree.NextMaterial(null, null) == null && MergeTree.NextMaterial(new List<MergeItemMaterial>(), null) == null);
        });
        Check("material selection recovers when the previous entry has been removed", () => {
            var removed = new MergeItemMaterial();
            var next = new MergeItemMaterial { MergeItem = new MergeItem { item = new ItemData() }, count = 2 };
            True(MergeTree.NextMaterial(new[] { next }, removed) == next);
        });
        Check("merge radii preserve authored values and sanitize non-finite or nonpositive values", () => {
            var definition = new MergeItem { radius = 1f }; Eq(definition.Radius, 1f);
            definition.radius = float.NaN; Eq(definition.Radius, .5f);
            definition.radius = -2f; Eq(definition.Radius, .01f);
        });
    }
}
