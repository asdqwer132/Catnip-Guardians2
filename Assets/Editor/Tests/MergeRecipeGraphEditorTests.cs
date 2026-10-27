using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class MergeRecipeGraphEditorTests
{
    private string folder;
    private MergeRecipeGraphAsset graph;
    private readonly List<GameObject> objects = new List<GameObject>();

    private T Asset<T>(string name) where T : ScriptableObject
    {
        var value = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(value, folder + "/" + name + ".asset");
        return value;
    }

    private GameObject Host(string name)
    {
        var host = new GameObject(name);
        objects.Add(host);
        return host;
    }

    [SetUp]
    public void SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        string name = "__MergeGraphTests_" + Guid.NewGuid().ToString("N");
        AssetDatabase.CreateFolder("Assets", name);
        folder = "Assets/" + name;
        graph = Asset<MergeRecipeGraphAsset>("Graph");
        var host = Host("Merge prefab");
        host.AddComponent<MergeItemObject>();
        graph.defaultPrefab = PrefabUtility.SaveAsPrefabAsset(host, folder + "/Merge.prefab");
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var host in objects) if (host != null) Object.DestroyImmediate(host);
        objects.Clear();
        foreach (string guid in AssetDatabase.FindAssets("", new[] { folder }))
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) Undo.ClearUndo(asset);
        }
        AssetDatabase.DeleteAsset(folder);
    }

    private MergeItem Item() => MergeRecipeGraphEditing.CreateItem(graph, new Vector2(graph.items.Count * 300, 50));

    private ItemRecipeData Recipe(MergeItem first, MergeItem second, MergeItem result)
    {
        var recipe = MergeRecipeGraphEditing.CreateRecipe(graph, new Vector2(300, 200));
        MergeRecipeGraphEditing.SetInput(recipe, 0, first.item);
        MergeRecipeGraphEditing.SetInput(recipe, 1, second.item);
        MergeRecipeGraphEditing.SetResult(recipe, result.item);
        return recipe;
    }

    [Test]
    public void CreatingNodesProducesIndependentMergeAndItemDataAssets()
    {
        var first = Item(); var second = Item();
        Assert.That(first.item, Is.Not.Null);
        Assert.That(first.item, Is.Not.SameAs(second.item));
        Assert.That(first.prefab, Is.Not.SameAs(second.prefab));
        Assert.That(PrefabUtility.GetPrefabAssetType(first.prefab), Is.EqualTo(PrefabAssetType.Variant));
        Assert.That(first.item.series, Is.EqualTo(graph.defaultSeries));
        Assert.That(AssetDatabase.Contains(first), Is.True);
        Assert.That(AssetDatabase.Contains(first.item), Is.True);
        MergeRecipeGraphEditing.AddItem(graph, first, Vector2.zero);
        Assert.That(graph.items.Count, Is.EqualTo(2));
    }

    [Test]
    public void ConnectedAssetsUseTheExistingRuntimeRecipeMatcherAndPreserveManagerStock()
    {
        var a = Item(); var b = Item(); var c = Item();
        var recipe = Recipe(a, b, c);
        recipe.tier = 42; // 티어 수를 고정하지 않습니다.
        var manager = Host("Manager").AddComponent<MergegameManager>();
        var stock = new MergeItemMaterial(a, null, 9, 4);
        manager.mergeMaterials.Add(stock);
        MergeRecipeGraphEditing.Apply(graph, manager);
        Assert.That(MergeTree.TryGetRecipe(manager.mergeRecipe, b.item, a.item, out var found), Is.True);
        Assert.That(found, Is.SameAs(recipe));
        Assert.That(found.resultItem, Is.SameAs(c.item));
        Assert.That(manager.GetDefinitionAtMergeItem(found.resultItem), Is.SameAs(c));
        Assert.That(manager.mergeRecipe[0].tier, Is.EqualTo(42));
        Assert.That(manager.mergeMaterials[0], Is.SameAs(stock));
        Assert.That(stock.count, Is.EqualTo(9)); Assert.That(stock.defaultCount, Is.EqualTo(4));
        var other = Host("Other manager").AddComponent<MergegameManager>();
        MergeRecipeGraphEditing.Apply(graph, other);
        manager.mergeItems.Clear(); manager.mergeRecipe.Clear();
        Assert.That(other.mergeItems.Count, Is.EqualTo(3));
        Assert.That(other.mergeRecipe.Count, Is.EqualTo(1));
    }

    [Test]
    public void SameIngredientUsesQuantityTwoAndDisconnectPreservesTheOtherPort()
    {
        var a = Item(); var result = Item();
        var recipe = Recipe(a, a, result);
        Assert.That(recipe.materials.Length, Is.EqualTo(1));
        Assert.That(recipe.materials[0].amount, Is.EqualTo(2));
        Assert.That(MergeTree.MatchesPair(recipe, a.item, a.item), Is.True);
        MergeRecipeGraphEditing.SetInput(recipe, 0, null);
        Assert.That(MergeRecipeGraphEditing.TryInputs(recipe, out var first, out var second), Is.True);
        Assert.That(first, Is.Null); Assert.That(second, Is.SameAs(a.item));
        Assert.That(MergeTree.MatchesPair(recipe, a.item, a.item), Is.False);
    }

    [Test]
    public void ExistingRecipesRestoreNodesAndConnectionsWithoutChangingTheSource()
    {
        var a = Item(); var b = Item(); var c = Item();
        var recipe = Recipe(a, b, c);
        string original = EditorJsonUtility.ToJson(recipe);
        var restored = Asset<MergeRecipeGraphAsset>("Restored");
        MergeRecipeGraphEditing.ImportRecipes(restored, new[] { recipe });
        Assert.That(restored.items.Count, Is.EqualTo(3));
        Assert.That(restored.recipes[0].recipe, Is.SameAs(recipe));
        Assert.That(MergeRecipeGraphEditing.Definition(restored, a.item), Is.SameAs(a));
        Assert.That(EditorJsonUtility.ToJson(recipe), Is.EqualTo(original));
        MergeRecipeGraphEditing.ImportRecipes(restored, new[] { recipe });
        Assert.That(restored.items.Count, Is.EqualTo(3)); Assert.That(restored.recipes.Count, Is.EqualTo(1));
    }

    [Test]
    public void ReverseImportCreatesMissingMergeDefinitionsAndKeepsTheOriginalItemData()
    {
        var a = Asset<ItemData>("Material"); var result = Asset<ItemData>("Result");
        var recipe = Asset<ItemRecipeData>("Existing recipe");
        recipe.materials = new[] { new RecipeMaterial { itemData = a, amount = 2 } };
        recipe.resultItem = result;
        string original = EditorJsonUtility.ToJson(recipe);
        MergeRecipeGraphEditing.ImportRecipes(graph, new[] { recipe });
        Assert.That(graph.items.Count, Is.EqualTo(2));
        Assert.That(MergeRecipeGraphEditing.Definition(graph, a).item, Is.SameAs(a));
        Assert.That(PrefabUtility.GetPrefabAssetType(MergeRecipeGraphEditing.Definition(graph, result).prefab), Is.EqualTo(PrefabAssetType.Variant));
        Assert.That(EditorJsonUtility.ToJson(recipe), Is.EqualTo(original));
        Assert.That(MergeRecipeGraphEditing.Validate(graph), Is.Empty);
    }

    [Test]
    public void ValidationRejectsDuplicatePairsAndIncompleteRecipesBeforeChangingManager()
    {
        var a = Item(); var b = Item(); var c = Item();
        Recipe(a, b, c); Recipe(b, a, c);
        var manager = Host("Manager").AddComponent<MergegameManager>();
        manager.mergeItems.Add(a);
        Assert.That(MergeRecipeGraphEditing.Validate(graph), Is.Not.Empty);
        Assert.Throws<InvalidOperationException>(() => MergeRecipeGraphEditing.Apply(graph, manager));
        Assert.That(manager.mergeItems.Count, Is.EqualTo(1));
        graph.recipes.RemoveAt(1);
        MergeRecipeGraphEditing.SetResult(graph.recipes[0].recipe, null);
        Assert.Throws<InvalidOperationException>(() => MergeRecipeGraphEditing.Apply(graph, manager));
        Assert.That(manager.mergeItems.Count, Is.EqualTo(1));
    }

    [Test]
    public void ConnectionEditsSupportUndoAndRedoOnTheRealRecipeAsset()
    {
        var a = Item(); var b = Item(); var c = Item();
        var recipe = Recipe(a, a, c);
        Undo.FlushUndoRecordObjects(); Undo.IncrementCurrentGroup();
        MergeRecipeGraphEditing.SetInput(recipe, 1, b.item);
        Undo.FlushUndoRecordObjects();
        Assert.That(MergeTree.MatchesPair(recipe, a.item, b.item), Is.True);
        Undo.PerformUndo();
        Assert.That(MergeTree.MatchesPair(recipe, a.item, a.item), Is.True);
        Undo.PerformRedo();
        Assert.That(MergeTree.MatchesPair(recipe, a.item, b.item), Is.True);
    }

    [Test]
    public void GraphWindowBuildsItemAndRecipeCardsUsingExistingAssets()
    {
        var a = Item(); var b = Item(); var c = Item(); Recipe(a, b, c);
        var window = ScriptableObject.CreateInstance<MergeRecipeGraphWindow>();
        try
        {
            window.CreateGUI();
            var canvas = window.rootVisualElement.Q<MergeRecipeGraphCanvas>();
            canvas.SetGraph(graph, a);
            Assert.That(canvas.Query<VisualElement>("merge-item-node").ToList().Count, Is.EqualTo(3));
            Assert.That(canvas.Query<VisualElement>("merge-recipe-node").ToList().Count, Is.EqualTo(1));
            Assert.That(canvas.Query<VisualElement>("IngredientA").ToList().Count, Is.EqualTo(1));
            canvas.RefreshPreviews(); canvas.FrameAll();
        }
        finally { Object.DestroyImmediate(window); }
    }

    [Test]
    public void SavingPersistsNodeLayoutAndRecipeConnections()
    {
        var a = Item(); var b = Item(); var c = Item();
        var recipe = Recipe(a, b, c);
        graph.items[0].position = new Vector2(123, 456);
        graph.recipes[0].position = new Vector2(789, 234);
        EditorUtility.SetDirty(graph);
        MergeRecipeGraphEditing.Save(graph);
        string path = AssetDatabase.GetAssetPath(graph);
        Assert.That(File.ReadAllText(path), Does.Contain("123").And.Contain("456"));
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var restored = AssetDatabase.LoadAssetAtPath<MergeRecipeGraphAsset>(path);
        Assert.That(restored.items[0].position, Is.EqualTo(new Vector2(123, 456)));
        Assert.That(restored.recipes[0].position, Is.EqualTo(new Vector2(789, 234)));
        Assert.That(restored.recipes[0].recipe, Is.SameAs(recipe));
        Assert.That(MergeTree.MatchesPair(recipe, a.item, b.item), Is.True);
    }

    [Test]
    public void ImportingManagerRestoresItsTreeAndKeepsSparseTiersCompact()
    {
        var a = Item(); var b = Item(); var c = Item();
        var recipe = Recipe(a, b, c);
        recipe.tier = 42;
        var manager = Host("Existing manager").AddComponent<MergegameManager>();
        MergeRecipeGraphEditing.Apply(graph, manager);
        var restored = Asset<MergeRecipeGraphAsset>("Manager import");
        MergeRecipeGraphEditing.Import(restored, manager);
        Assert.That(restored.items.Count, Is.EqualTo(3));
        Assert.That(restored.recipes[0].recipe, Is.SameAs(recipe));
        MergeRecipeGraphEditing.Arrange(restored);
        Assert.That(restored.items.Find(node => node.item == c).position.x, Is.EqualTo(580));
        Assert.That(restored.recipes[0].position.x, Is.EqualTo(310));
        Assert.That(recipe.tier, Is.EqualTo(42), "Arrangement must not change the actual recipe tier.");
    }

    private static void DragPort(MergeRecipeGraphCanvas canvas, VisualElement from, VisualElement to)
    {
        using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = from.worldBound.center }))
            from.SendEvent(down);
        using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = to.worldBound.center }))
            canvas.SendEvent(up);
    }

    [Test]
    public void ConnectingRealCardPortsWritesRecipesAndRejectsIncompatiblePorts()
    {
        var a = Item(); var b = Item(); var c = Item();
        var recipe = MergeRecipeGraphEditing.CreateRecipe(graph, new Vector2(340, 300));
        string message = null;
        var canvas = new MergeRecipeGraphCanvas(_ => { }, _ => { }, _ => { }, _ => { }, value => message = value);
        canvas.SetGraph(graph, null);
        var cards = canvas.Query<VisualElement>("merge-item-node").ToList();
        var recipeCard = canvas.Q<VisualElement>("merge-recipe-node");
        canvas.ConnectPorts(cards[0].Q("ItemOutput"), recipeCard.Q("IngredientA"));
        canvas.ConnectPorts(recipeCard.Q("IngredientB"), cards[1].Q("ItemOutput")); // 반대 방향 드래그도 허용합니다.
        canvas.ConnectPorts(recipeCard.Q("Result"), cards[2].Q("ItemInput"));
        Assert.That(MergeTree.MatchesPair(recipe, a.item, b.item), Is.True);
        Assert.That(recipe.resultItem, Is.SameAs(c.item));
        string before = EditorJsonUtility.ToJson(recipe);
        canvas.ConnectPorts(cards[0].Q("ItemOutput"), cards[2].Q("ItemInput"));
        Assert.That(EditorJsonUtility.ToJson(recipe), Is.EqualTo(before));
        Assert.That(message, Does.Contain("레시피 A/B"));
    }

    [UnityTest]
    public IEnumerator DraggingActualPortsWritesTheRuntimeRecipe()
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            Assert.Ignore("실제 Editor 창의 마우스 드래그 검사는 그래픽 장치가 있는 Unity에서 실행합니다.");
        var a = Item(); var b = Item(); var c = Item();
        var recipe = MergeRecipeGraphEditing.CreateRecipe(graph, new Vector2(340, 300));
        var window = ScriptableObject.CreateInstance<MergeRecipeGraphWindow>();
        try
        {
            window.position = new Rect(40, 40, 1100, 700);
            window.Show();
            yield return null;
            var canvas = window.rootVisualElement.Q<MergeRecipeGraphCanvas>();
            Assert.That(canvas, Is.Not.Null);
            canvas.SetGraph(graph, null);
            yield return null;
            var items = canvas.Query<VisualElement>("merge-item-node").ToList();
            var recipeCard = canvas.Q<VisualElement>("merge-recipe-node");
            Assert.That(recipeCard.worldBound.width, Is.GreaterThan(0));
            DragPort(canvas, items[0].Q("ItemOutput"), recipeCard.Q("IngredientA"));
            DragPort(canvas, items[1].Q("ItemOutput"), recipeCard.Q("IngredientB"));
            DragPort(canvas, recipeCard.Q("Result"), items[2].Q("ItemInput"));
            Assert.That(MergeTree.MatchesPair(recipe, a.item, b.item), Is.True);
            Assert.That(recipe.resultItem, Is.SameAs(c.item));
        }
        finally { window.Close(); Object.DestroyImmediate(window); }
    }
}
