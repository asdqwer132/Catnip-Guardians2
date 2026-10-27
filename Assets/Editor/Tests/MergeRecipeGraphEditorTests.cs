using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class MergeRecipeGraphEditorTests
{
    private string folder;
    private MergeRecipeGraphAsset graph;
    private readonly List<GameObject> hosts = new List<GameObject>();
    private T Asset<T>(string name) where T : ScriptableObject
    {
        var asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, folder + "/" + name + ".asset"); return asset;
    }
    private GameObject Host(string name) { var host = new GameObject(name); hosts.Add(host); return host; }
    [SetUp]
    public void SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        string name = "__MergeBookTests_" + Guid.NewGuid().ToString("N");
        AssetDatabase.CreateFolder("Assets", name); folder = "Assets/" + name;
        graph = Asset<MergeRecipeGraphAsset>("Graph");
    }
    [TearDown]
    public void TearDown()
    {
        foreach (var host in hosts) if (host != null) Object.DestroyImmediate(host);
        hosts.Clear();
        foreach (string guid in AssetDatabase.FindAssets("", new[] { folder }))
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)); if (asset != null) Undo.ClearUndo(asset);
        }
        AssetDatabase.DeleteAsset(folder);
    }
    private MergeItem Item()
    {
        var item = MergeRecipeGraphEditing.CreateItem(graph, new Vector2(graph.items.Count * 380, 50));
        item.item = Asset<ItemData>(item.name + " Existing Data");
        return item;
    }
    private MergeGraphItemNode Connect(MergeItem result, params MergeItem[] sources)
    {
        var node = graph.items.Find(value => value.item == result);
        foreach (var source in sources)
        {
            MergeRecipeGraphEditing.AddInput(graph, node);
            MergeRecipeGraphEditing.SetInput(graph, node, node.ingredients.Count - 1, source);
        }
        return node;
    }
    [Test]
    public void PlusCreatesOnlyEmptyMergeItemAssetsAndGraphCanSaveBeforeDataAssignment()
    {
        var a = MergeRecipeGraphEditing.CreateItem(graph, Vector2.zero);
        var b = MergeRecipeGraphEditing.CreateItem(graph, Vector2.right * 400);
        Assert.That(a.item, Is.Null); Assert.That(a.prefab, Is.Null); Assert.That(a, Is.Not.SameAs(b));
        Assert.That(AssetDatabase.FindAssets("t:ItemData", new[] { folder }), Is.Empty);
        Assert.That(AssetDatabase.FindAssets("t:Prefab", new[] { folder }), Is.Empty);
        Assert.That(AssetDatabase.FindAssets("t:MergeItem", new[] { folder }).Length, Is.EqualTo(2));
        MergeRecipeGraphEditing.Save(graph);
        Assert.That(AssetDatabase.Contains(a) && AssetDatabase.Contains(b), Is.True);
        Assert.Throws<InvalidOperationException>(() => MergeRecipeGraphEditing.ExportRecipes(graph));
        Assert.That(graph.recipeBook, Is.Null);
    }
    [Test]
    public void ArbitraryInputsExtractRecipesFirstThenGroupByResultNodeTierAndKeepGuids()
    {
        var a = Item(); var b = Item(); var c = Item(); var result = Item(); var next = Item();
        var node = Connect(result, a, b, a, c); node.tier = 42;
        var nextNode = Connect(next, result, c, c); nextNode.tier = 55;
        var recipes = MergeRecipeGraphEditing.ExportRecipes(graph);
        Assert.That(recipes.Count, Is.EqualTo(2)); Assert.That(graph.recipeBook, Is.Null);
        Assert.That(node.generatedRecipe.tier, Is.EqualTo(42));
        Assert.That(MergeTree.MatchesIngredients(node.generatedRecipe, new[] { c.item, a.item, b.item, a.item }), Is.True);
        Assert.That(node.generatedRecipe.materials.Length, Is.EqualTo(3));
        string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(node.generatedRecipe));
        var book = MergeRecipeGraphEditing.Export(graph);
        Assert.That(book.tiers.Count, Is.EqualTo(2)); Assert.That(book.tiers[0].tier, Is.EqualTo(42));
        Assert.That(book.tiers[1].tier, Is.EqualTo(55));
        Assert.That(book.TryGetRecipe(new[] { a.item, a.item, b.item, c.item }, out var recipe), Is.True);
        Assert.That(recipe.resultItem, Is.SameAs(result.item)); Assert.That(book.GetDefinition(recipe.resultItem), Is.SameAs(result));
        string bookGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(book));
        MergeRecipeGraphEditing.Export(graph);
        Assert.That(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(node.generatedRecipe)), Is.EqualTo(guid));
        Assert.That(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(book)), Is.EqualTo(bookGuid));
        Assert.That(AssetDatabase.FindAssets("t:ItemRecipeData", new[] { folder }).Length, Is.EqualTo(2));
    }
    [Test]
    public void UnboundedInputControlsAllowMoreThanTwoPortsAndRepeatedIngredients()
    {
        var a = Item(); var result = Item(); var node = Connect(result);
        for (int i = 0; i < 32; i++) { MergeRecipeGraphEditing.AddInput(graph, node); MergeRecipeGraphEditing.SetInput(graph, node, i, a); }
        var canvas = new MergeRecipeGraphCanvas(_ => { }, _ => { }, _ => { }, _ => { }); canvas.SetGraph(graph, null);
        var card = canvas.Query<VisualElement>("merge-item-node").ToList()[1];
        Assert.That(card.Q("Ingredient_31"), Is.Not.Null); Assert.That(card.Query<VisualElement>("ItemOutput").ToList().Count, Is.EqualTo(1));
        MergeRecipeGraphEditing.ExportRecipes(graph);
        Assert.That(node.generatedRecipe.materials.Length, Is.EqualTo(1)); Assert.That(node.generatedRecipe.materials[0].amount, Is.EqualTo(32));
        MergeRecipeGraphEditing.RemoveInput(graph, node, 10); Assert.That(node.ingredients.Count, Is.EqualTo(31));
    }
    [Test]
    public void EmptyOrDuplicateIngredientsAreRejectedBeforeExportMutatesAnyAssets()
    {
        var a = Item(); var b = Item(); var result = Item(); var node = Connect(result, a, a, b);
        var book = MergeRecipeGraphEditing.Export(graph); string before = EditorJsonUtility.ToJson(node.generatedRecipe);
        MergeRecipeGraphEditing.SetInput(graph, node, 1, null);
        Assert.Throws<InvalidOperationException>(() => MergeRecipeGraphEditing.Export(graph));
        Assert.That(EditorJsonUtility.ToJson(node.generatedRecipe), Is.EqualTo(before));
        MergeRecipeGraphEditing.SetInput(graph, node, 1, a);
        Connect(Item(), b, a, a);
        Assert.Throws<InvalidOperationException>(() => MergeRecipeGraphEditing.Export(graph));
        Assert.That(book.tiers[0].itemRecipeDatas.Count, Is.EqualTo(1));
        Assert.That(EditorJsonUtility.ToJson(node.generatedRecipe), Is.EqualTo(before));
    }
    [Test]
    public void RemovingAllInputsExcludesRecipeWithoutDeletingItsAsset()
    {
        var a = Item(); var result = Item(); var node = Connect(result, a, a);
        var book = MergeRecipeGraphEditing.Export(graph); var recipe = node.generatedRecipe;
        MergeRecipeGraphEditing.RemoveInput(graph, node, 1); MergeRecipeGraphEditing.RemoveInput(graph, node, 0);
        MergeRecipeGraphEditing.Export(graph);
        Assert.That(book.tiers, Is.Empty); Assert.That(AssetDatabase.Contains(recipe), Is.True);
    }
    [Test]
    public void ApplyConnectsScriptableBookAndPreservesManagerStockAndUi()
    {
        var a = Item(); var b = Item(); var c = Item(); Connect(c, a, b, a);
        var manager = Host("Manager").AddComponent<MergegameManager>(); manager.mergeItems.Add(a);
        var stock = new MergeItemMaterial(a, null, 9, 4); manager.mergeMaterials.Add(stock);
        MergeRecipeGraphEditing.Apply(graph, manager);
        Assert.That(manager.recipeBook, Is.SameAs(graph.recipeBook)); Assert.That(manager.mergeItems, Is.EqualTo(new[] { a }));
        Assert.That(manager.mergeMaterials[0], Is.SameAs(stock)); Assert.That(stock.count, Is.EqualTo(9));
        Assert.That(stock.defaultCount, Is.EqualTo(4)); Assert.That(manager.GetDefinitionAtMergeItem(c.item), Is.SameAs(c));
    }
    [Test]
    public void ReverseBookAndFolderImportPreserveNIngredientsAndNodeTiers()
    {
        var a = Item(); var b = Item(); var c = Item(); var node = Connect(c, a, a, b); node.tier = 7;
        var book = MergeRecipeGraphEditing.Export(graph); string before = EditorJsonUtility.ToJson(node.generatedRecipe);
        var restored = Asset<MergeRecipeGraphAsset>("Restored"); MergeRecipeGraphEditing.ImportBook(restored, book);
        var result = restored.items.Find(value => value.item == c);
        Assert.That(result.ingredients, Is.EquivalentTo(new[] { a, a, b })); Assert.That(result.tier, Is.EqualTo(7));
        Assert.That(EditorJsonUtility.ToJson(node.generatedRecipe), Is.EqualTo(before));
        MergeRecipeGraphEditing.ImportBook(restored, book); Assert.That(restored.items.Count, Is.EqualTo(3));
        var manager = Host("Manager").AddComponent<MergegameManager>(); manager.mergeItems.AddRange(book.items);
        MergeManagerFolderImport.ApplyRecipes(manager, folder + "/Graph Recipes", true, false);
        Assert.That(manager.recipeBook.TryGetRecipe(new[] { b.item, a.item, a.item }, out _), Is.True);
        Assert.That(manager.recipeBook.tiers[0].tier, Is.EqualTo(7));
    }
    [Test]
    public void NodeTierChangesRegroupBookAndUseEditableGradientAndRange()
    {
        var a = Item(); var b = Item(); var c = Item(); var d = Item();
        var cNode = Connect(c, a, a); var dNode = Connect(d, a, b); cNode.tier = 2; dNode.tier = 5;
        var book = MergeRecipeGraphEditing.Export(graph); Assert.That(book.tiers.Count, Is.EqualTo(2));
        graph.colorMinTier = 2; graph.colorMaxTier = 6;
        graph.tierColors.SetKeys(new[] { new GradientColorKey(Color.blue, 0), new GradientColorKey(Color.red, 1) },
            new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
        var canvas = new MergeRecipeGraphCanvas(_ => { }, _ => { }, _ => { }, _ => { }); canvas.SetGraph(graph, cNode);
        var cards = canvas.Query<VisualElement>("merge-item-node").ToList();
        Assert.That(cards[2].style.backgroundColor.value, Is.EqualTo(Color.blue));
        MergeRecipeGraphEditing.SetTier(graph, cNode, 4); canvas.RefreshPreviews();
        Assert.That(cards[2].Q<IntegerField>("NodeTier").value, Is.EqualTo(4));
        Assert.That(cNode.tier, Is.EqualTo(4)); Assert.That(graph.GetTierColor(4).r, Is.EqualTo(.5f).Within(.01f));
        Assert.That(cards[2].style.backgroundColor.value, Is.EqualTo(graph.GetTierColor(4)));
        Assert.That(graph.GetTierColor(-10), Is.EqualTo(Color.blue)); Assert.That(graph.GetTierColor(999), Is.EqualTo(Color.red));
        MergeRecipeGraphEditing.SetTier(graph, dNode, 4); MergeRecipeGraphEditing.Export(graph);
        Assert.That(book.tiers.Count, Is.EqualTo(1)); Assert.That(book.tiers[0].tier, Is.EqualTo(4));
        Assert.That(book.tiers[0].itemRecipeDatas.Count, Is.EqualTo(2));
    }
    [Test]
    public void ConnectionUndoRedoAndSavedInputsTiersColorsAndPositionsPersist()
    {
        var a = Item(); var b = Item(); var c = Item(); var node = Connect(c, a, a, a);
        Undo.FlushUndoRecordObjects(); Undo.IncrementCurrentGroup();
        MergeRecipeGraphEditing.SetInput(graph, node, 2, b); Undo.FlushUndoRecordObjects();
        Undo.PerformUndo(); Assert.That(graph.items[2].ingredients[2], Is.SameAs(a));
        Undo.PerformRedo(); Assert.That(graph.items[2].ingredients[2], Is.SameAs(b));
        graph.items[2].position = new Vector2(123, 456); graph.items[2].tier = 17; graph.colorMaxTier = 30;
        EditorUtility.SetDirty(graph); MergeRecipeGraphEditing.Save(graph);
        string path = AssetDatabase.GetAssetPath(graph); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var restored = AssetDatabase.LoadAssetAtPath<MergeRecipeGraphAsset>(path);
        Assert.That(restored.items[2].position, Is.EqualTo(new Vector2(123, 456)));
        Assert.That(restored.items[2].tier, Is.EqualTo(17)); Assert.That(restored.items[2].ingredients[2], Is.SameAs(b));
        Assert.That(restored.colorMaxTier, Is.EqualTo(30));
    }
    [Test]
    public void CanvasConnectsAnyInputInEitherDirectionAndKeepsOneOutputPerNode()
    {
        var a = Item(); var b = Item(); var c = Item(); var node = Connect(c);
        for (int i = 0; i < 3; i++) MergeRecipeGraphEditing.AddInput(graph, node);
        string status = null; var canvas = new MergeRecipeGraphCanvas(_ => { }, _ => { }, _ => { }, text => status = text);
        canvas.SetGraph(graph, null); var cards = canvas.Query<VisualElement>("merge-item-node").ToList();
        canvas.ConnectPorts(cards[0].Q("ItemOutput"), cards[2].Q("Ingredient_0"));
        canvas.ConnectPorts(cards[2].Q("Ingredient_1"), cards[1].Q("ItemOutput"));
        canvas.ConnectPorts(cards[0].Q("ItemOutput"), cards[2].Q("Ingredient_2"));
        Assert.That(node.ingredients, Is.EqualTo(new[] { a, b, a }));
        canvas.ConnectPorts(cards[0].Q("ItemOutput"), cards[1].Q("ItemOutput")); Assert.That(status, Does.Contain("재료 입력"));
        Assert.That(MergeRecipeGraphEditing.Export(graph).TryGetRecipe(new[] { a.item, b.item, a.item }, out _), Is.True);
    }
    [Test]
    public void PreviousFixedInputGraphsMigrateAllCombinationsAndTiersWithoutCreatingData()
    {
        var a = Item(); var b = Item(); var c = Item(); var node = graph.items[2];
        node.LegacyInputs.Add(new MergeGraphInputPair { first = a, second = b, tier = 4 });
        node.LegacyInputs.Add(new MergeGraphInputPair { first = a, second = a, tier = 7 });
        EditorUtility.SetDirty(graph); AssetDatabase.SaveAssetIfDirty(graph);
        string path = AssetDatabase.GetAssetPath(graph);
        File.WriteAllText(path, File.ReadAllText(path).Replace("legacyInputs:", "inputs:"));
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        MergeRecipeGraphEditing.Upgrade(graph);
        Assert.That(graph.items.Count, Is.EqualTo(4)); Assert.That(graph.items[2].tier, Is.EqualTo(4));
        Assert.That(graph.items[2].ingredients, Is.EqualTo(new[] { a, b }));
        Assert.That(graph.items[3].tier, Is.EqualTo(7)); Assert.That(graph.items[3].item, Is.SameAs(c));
        MergeRecipeGraphEditing.Upgrade(graph); Assert.That(graph.items.Count, Is.EqualTo(4));
        var book = MergeRecipeGraphEditing.Export(graph); Assert.That(book.items.Count, Is.EqualTo(3));
        Assert.That(book.tiers.Count, Is.EqualTo(2));
    }
    [Test]
    public void PreviousRecipeCardsUpgradeWithoutChangingRecipeAssets()
    {
        var a = Item(); var b = Item(); var c = Item(); var recipe = Asset<ItemRecipeData>("Previous recipe");
        recipe.resultItem = c.item; recipe.tier = 6;
        recipe.materials = new[] { new RecipeMaterial { itemData = a.item, amount = 3 }, new RecipeMaterial { itemData = b.item, amount = 1 } };
        string before = EditorJsonUtility.ToJson(recipe);
        var serialized = new SerializedObject(graph); var previous = serialized.FindProperty("legacyRecipes"); previous.arraySize = 1;
        previous.GetArrayElementAtIndex(0).FindPropertyRelative("recipe").objectReferenceValue = recipe;
        serialized.ApplyModifiedPropertiesWithoutUndo(); MergeRecipeGraphEditing.Upgrade(graph);
        Assert.That(graph.items[2].ingredients, Is.EquivalentTo(new[] { a, a, a, b })); Assert.That(graph.items[2].tier, Is.EqualTo(6));
        Assert.That(EditorJsonUtility.ToJson(recipe), Is.EqualTo(before));
    }
    [Test]
    public void ArrangeUsesExplicitNodeTierAndVariableCardHeights()
    {
        var a = Item(); var b = Item(); var c = Item();
        Connect(c, a, b); graph.items[0].tier = 0; graph.items[1].tier = 42; graph.items[2].tier = 7;
        MergeRecipeGraphEditing.Arrange(graph);
        Assert.That(graph.items[0].position.x, Is.LessThan(graph.items[2].position.x));
        Assert.That(graph.items[2].position.x, Is.LessThan(graph.items[1].position.x));
    }
    [UnityTest]
    public IEnumerator ActualEditorDraggingConnectsDynamicInput()
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) Assert.Ignore("실제 Editor 드래그 검사는 그래픽 장치가 필요합니다.");
        var a = Item(); Item(); var c = Item(); Connect(c, a, a); MergeRecipeGraphEditing.AddInput(graph, graph.items[2]);
        var window = ScriptableObject.CreateInstance<MergeRecipeGraphWindow>();
        try
        {
            window.position = new Rect(40, 40, 1100, 700); window.Show(); yield return null;
            var canvas = window.rootVisualElement.Q<MergeRecipeGraphCanvas>(); canvas.SetGraph(graph, null); yield return null;
            var cards = canvas.Query<VisualElement>("merge-item-node").ToList();
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = cards[0].Q("ItemOutput").worldBound.center })) cards[0].Q("ItemOutput").SendEvent(down);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = cards[2].Q("Ingredient_2").worldBound.center })) canvas.SendEvent(up);
            Assert.That(graph.items[2].ingredients[2], Is.SameAs(a));
        }
        finally { window.Close(); Object.DestroyImmediate(window); }
    }
}
