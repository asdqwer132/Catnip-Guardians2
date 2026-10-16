using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class Runner
{
    private static ItemRecipeData MergeRecipe(ItemData result, params RecipeMaterial[] materials)
        => new ItemRecipeData { resultItem = result, materials = materials };
    private static RecipeMaterial MergeIngredient(ItemData item, int amount = 1)
        => new RecipeMaterial { itemData = item, amount = amount };
    private static void RunMergeGameChecks()
    {
        Check("selection clamps world distance in every drag direction and builds ordered rectangles", () => {
            foreach (var direction in new[] { new Vector2(3f, 4f), new Vector2(-3f, 4f), new Vector2(3f, -4f), new Vector2(-3f, -4f) }) {
                var selection = new MergeItemSelectionGesture(); var start = new Vector2(10f, 20f);
                True(selection.TryBegin(start, 0f)); selection.Move(start + direction * 10f, 5f, .1f);
                Eq((selection.End - start).magnitude, 5f); Eq(selection.Bounds.width, 3f); Eq(selection.Bounds.height, 4f);
                True(selection.IsDrag && selection.Bounds.xMin <= selection.Bounds.xMax && selection.Bounds.yMin <= selection.Bounds.yMax);
            }
        });
        Check("a drag never also becomes a click even after returning to its starting point", () => {
            var selection = new MergeItemSelectionGesture(); True(selection.TryBegin(Vector2.zero, 0f));
            selection.Move(new Vector2(.01f, 0f), 3f, .1f); True(!selection.IsDrag);
            selection.Move(Vector2.one, 3f, .1f); selection.Move(Vector2.zero, 3f, .1f); True(selection.IsDrag);
            selection.Complete(1f, 2f); True(!selection.IsActive && !selection.IsDrag);
            selection.Complete(1f, 100f); Eq(selection.CooldownRemaining(1f), 2f);
        });
        Check("X cancellation ignores subsequent release and permits immediate fresh selection", () => {
            var selection = new MergeItemSelectionGesture(); selection.TryBegin(Vector2.zero, 0f);
            selection.Move(Vector2.one, 3f, .1f); selection.Cancel();
            selection.Complete(1f, 100f); True(!selection.IsActive); Eq(selection.CooldownRemaining(1f), 0f);
            True(selection.TryBegin(Vector2.zero, 1f) && !selection.IsDrag);
        });
        Check("click and drag share the next selection time and cancellation cannot reset cooldown", () => {
            var selection = new MergeItemSelectionGesture(); selection.TryBegin(Vector2.zero, 0f); selection.Complete(0f, 5f);
            True(!selection.TryBegin(Vector2.zero, 4f)); selection.Cancel(); Eq(selection.CooldownRemaining(4f), 1f);
            True(selection.TryBegin(Vector2.zero, 5f)); selection.Move(Vector2.one, 3f, .1f); selection.Complete(5f, 2f);
            True(!selection.TryBegin(Vector2.zero, 6f)); True(selection.TryBegin(Vector2.zero, 7f));
        });
        Check("selection includes circle edges but excludes outside corners", () => {
            var rect = Rect.MinMaxRect(-1f, -1f, 1f, 1f);
            True(MergeItemSelectionGesture.IntersectsCircle(rect, new Vector2(1.2f, 0f), .25f));
            True(!MergeItemSelectionGesture.IntersectsCircle(rect, new Vector2(1.2f, 1.2f), .25f));
            True(MergeItemSelectionGesture.IntersectsCircle(rect, Vector2.zero, .1f));
        });
        Check("selection rejects nonfinite points and sanitizes limits without blocking an empty release", () => {
            var selection = new MergeItemSelectionGesture();
            True(!selection.TryBegin(new Vector2(float.NaN, 0f), 0f)); True(selection.TryBegin(Vector2.zero, 0f));
            selection.Move(Vector2.one * 10f, float.NaN, float.NaN); Eq(selection.End.magnitude, 3f);
            selection.Move(new Vector2(float.PositiveInfinity, 0f), 3f, .1f); Eq(selection.End.magnitude, 3f);
            selection.Complete(0f, 0f); True(selection.TryBegin(Vector2.zero, 0f));
        });
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
