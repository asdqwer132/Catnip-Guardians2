using System;
using System.Collections;
using System.Collections.Generic;
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
        var prefab = Host("Merge template"); prefab.AddComponent<MergeItemObject>();
        graph.defaultPrefab = PrefabUtility.SaveAsPrefabAsset(prefab, folder + "/Template.prefab");
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
    private MergeItem Item() => MergeRecipeGraphEditing.CreateItem(graph, new Vector2(graph.items.Count * 380, 50));
    private MergeGraphInputPair Connect(MergeItem a, MergeItem b, MergeItem result)
    {
        var node = graph.items.Find(value => value.item == result);
        var pair = node.inputs.Find(value => value.IsEmpty) ?? MergeRecipeGraphEditing.AddCombination(graph, node);
        MergeRecipeGraphEditing.SetInput(graph, pair, 0, a); MergeRecipeGraphEditing.SetInput(graph, pair, 1, b); return pair;
    }
    [Test]
    public void PlusCreatesIndependentMergeItemsDataAndPrefabVariants()
    {
        var a = Item(); var b = Item();
        Assert.That(a.item, Is.Not.SameAs(b.item)); Assert.That(a.prefab, Is.Not.SameAs(b.prefab));
        Assert.That(PrefabUtility.GetPrefabAssetType(a.prefab), Is.EqualTo(PrefabAssetType.Variant));
        Assert.That(AssetDatabase.Contains(a) && AssetDatabase.Contains(a.item), Is.True);
        MergeRecipeGraphEditing.AddItem(graph, a, Vector2.zero); Assert.That(graph.items.Count, Is.EqualTo(2));
    }
    [Test]
    public void DirectItemConnectionsExportRecipesAndBookAndReuseTheirGuids()
    {
        var a = Item(); var b = Item(); var c = Item(); var pair = Connect(a, b, c); pair.tier = 42;
        var book = MergeRecipeGraphEditing.Export(graph);
        Assert.That(book.TryGetRecipe(b.item, a.item, out var recipe), Is.True);
        Assert.That(recipe.resultItem, Is.SameAs(c.item)); Assert.That(book.GetDefinition(recipe.resultItem), Is.SameAs(c));
        Assert.That(book.tiers[0].tier, Is.EqualTo(42));
        string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(recipe));
        string bookGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(book));
        Assert.That(MergeRecipeGraphEditing.Export(graph), Is.SameAs(book));
        Assert.That(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(pair.generatedRecipe)), Is.EqualTo(guid));
        Assert.That(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(book)), Is.EqualTo(bookGuid));
        Assert.That(AssetDatabase.FindAssets("t:ItemRecipeData", new[] { folder }).Length, Is.EqualTo(1));
    }
    [Test]
    public void SameIngredientExportsQuantityTwoAndIncompletePairDoesNotChangePreviousBook()
    {
        var a = Item(); var result = Item(); var pair = Connect(a, a, result);
        var book = MergeRecipeGraphEditing.Export(graph); var recipe = pair.generatedRecipe;
        Assert.That(recipe.materials.Length, Is.EqualTo(1)); Assert.That(recipe.materials[0].amount, Is.EqualTo(2));
        MergeRecipeGraphEditing.SetInput(graph, pair, 1, null);
        Assert.Throws<InvalidOperationException>(() => MergeRecipeGraphEditing.Export(graph));
        Assert.That(book.TryGetRecipe(a.item, a.item, out _), Is.True);
        MergeRecipeGraphEditing.SetInput(graph, pair, 0, null);
        MergeRecipeGraphEditing.Export(graph);
        Assert.That(book.tiers, Is.Empty); Assert.That(AssetDatabase.Contains(recipe), Is.True);
    }
    [Test]
    public void MultipleCombinationsOnOneResultExportAllAndDuplicatePairsAreRejected()
    {
        var a = Item(); var b = Item(); var c = Item(); var result = Item();
        Connect(a, b, result); Connect(a, c, result);
        var book = MergeRecipeGraphEditing.Export(graph);
        Assert.That(book.tiers[0].itemRecipeDatas.Count, Is.EqualTo(2));
        Assert.That(book.TryGetRecipe(a.item, c.item, out _), Is.True);
        Connect(b, a, c);
        Assert.Throws<InvalidOperationException>(() => MergeRecipeGraphEditing.Export(graph));
        Assert.That(book.tiers[0].itemRecipeDatas.Count, Is.EqualTo(2));
    }
    [Test]
    public void ApplyConnectsTheScriptableBookAndPreservesManagerStockAndOverrides()
    {
        var a = Item(); var b = Item(); var c = Item(); Connect(a, b, c);
        var manager = Host("Manager").AddComponent<MergegameManager>();
        manager.mergeItems.Add(a); var stock = new MergeItemMaterial(a, null, 9, 4); manager.mergeMaterials.Add(stock);
        MergeRecipeGraphEditing.Apply(graph, manager);
        Assert.That(manager.recipeBook, Is.SameAs(graph.recipeBook));
        Assert.That(manager.mergeItems, Is.EqualTo(new[] { a })); Assert.That(manager.mergeMaterials[0], Is.SameAs(stock));
        Assert.That(stock.count, Is.EqualTo(9)); Assert.That(stock.defaultCount, Is.EqualTo(4));
        Assert.That(manager.GetDefinitionAtMergeItem(c.item), Is.SameAs(c));
    }
    [Test]
    public void ReverseBookImportRestoresDirectItemLinksWithoutChangingRecipes()
    {
        var a = Item(); var b = Item(); var c = Item(); var pair = Connect(a, b, c);
        var book = MergeRecipeGraphEditing.Export(graph); string before = EditorJsonUtility.ToJson(pair.generatedRecipe);
        var restored = Asset<MergeRecipeGraphAsset>("Restored"); MergeRecipeGraphEditing.ImportBook(restored, book);
        Assert.That(restored.items.Count, Is.EqualTo(3));
        var inputs = restored.items.Find(node => node.item == c).inputs[0];
        Assert.That(inputs.first, Is.SameAs(a)); Assert.That(inputs.second, Is.SameAs(b));
        Assert.That(inputs.generatedRecipe, Is.SameAs(pair.generatedRecipe));
        Assert.That(EditorJsonUtility.ToJson(pair.generatedRecipe), Is.EqualTo(before));
        MergeRecipeGraphEditing.ImportBook(restored, book); Assert.That(restored.items.Count, Is.EqualTo(3));
    }
    [Test]
    public void ConnectionUndoRedoAndSavedPositionsRemainOnTheGraph()
    {
        var a = Item(); var b = Item(); var c = Item(); var pair = Connect(a, a, c);
        Undo.FlushUndoRecordObjects(); Undo.IncrementCurrentGroup();
        MergeRecipeGraphEditing.SetInput(graph, pair, 1, b); Undo.FlushUndoRecordObjects();
        Undo.PerformUndo(); Assert.That(graph.items.Find(node => node.item == c).inputs[0].second, Is.SameAs(a));
        Undo.PerformRedo(); Assert.That(graph.items.Find(node => node.item == c).inputs[0].second, Is.SameAs(b));
        graph.items[0].position = new Vector2(123, 456); EditorUtility.SetDirty(graph); MergeRecipeGraphEditing.Save(graph);
        string path = AssetDatabase.GetAssetPath(graph); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        Assert.That(AssetDatabase.LoadAssetAtPath<MergeRecipeGraphAsset>(path).items[0].position, Is.EqualTo(new Vector2(123, 456)));
    }
    [Test]
    public void CanvasContainsOnlyMergeItemCardsAndPortsConnectItemsDirectly()
    {
        var a = Item(); var b = Item(); var c = Item();
        string status = null;
        var canvas = new MergeRecipeGraphCanvas(_ => { }, _ => { }, _ => { }, text => status = text);
        canvas.SetGraph(graph, null);
        var cards = canvas.Query<VisualElement>("merge-item-node").ToList(); Assert.That(cards.Count, Is.EqualTo(3));
        Assert.That(canvas.Query<VisualElement>("merge-recipe-node").ToList(), Is.Empty);
        canvas.ConnectPorts(cards[0].Q("ItemOutput"), cards[2].Q("IngredientA_0"));
        canvas.ConnectPorts(cards[2].Q("IngredientB_0"), cards[1].Q("ItemOutput"));
        var pair = graph.items[2].inputs[0]; Assert.That(pair.first, Is.SameAs(a)); Assert.That(pair.second, Is.SameAs(b));
        canvas.ConnectPorts(cards[0].Q("ItemOutput"), cards[1].Q("ItemOutput")); Assert.That(status, Does.Contain("A / B"));
        Assert.That(MergeRecipeGraphEditing.Export(graph).TryGetRecipe(a.item, b.item, out _), Is.True);
    }
    [Test]
    public void ChainedConnectionsExportEveryStepAndArrangeByDependencies()
    {
        var a = Item(); var b = Item(); var c = Item(); var d = Item();
        Connect(a, b, c); Connect(c, c, d);
        var book = MergeRecipeGraphEditing.Export(graph);
        Assert.That(book.TryGetRecipe(a.item, b.item, out var first), Is.True);
        Assert.That(book.TryGetRecipe(first.resultItem, first.resultItem, out var second), Is.True);
        Assert.That(second.resultItem, Is.SameAs(d.item));
        MergeRecipeGraphEditing.Arrange(graph);
        Assert.That(graph.items[0].position.x, Is.LessThan(graph.items[2].position.x));
        Assert.That(graph.items[2].position.x, Is.LessThan(graph.items[3].position.x));
    }
    [Test]
    public void PreviousRecipeCardsUpgradeToDirectConnectionsWithoutChangingRecipeAssets()
    {
        var a = Item(); var b = Item(); var c = Item();
        var recipe = Asset<ItemRecipeData>("Previous recipe");
        recipe.resultItem = c.item;
        recipe.materials = new[] { new RecipeMaterial { itemData = a.item, amount = 1 }, new RecipeMaterial { itemData = b.item, amount = 1 } };
        string before = EditorJsonUtility.ToJson(recipe);
        var serialized = new SerializedObject(graph);
        var previous = serialized.FindProperty("legacyRecipes"); previous.arraySize = 1;
        previous.GetArrayElementAtIndex(0).FindPropertyRelative("recipe").objectReferenceValue = recipe;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        MergeRecipeGraphEditing.Upgrade(graph);
        var pair = graph.items.Find(node => node.item == c).inputs[0];
        Assert.That(pair.first, Is.SameAs(a)); Assert.That(pair.second, Is.SameAs(b));
        Assert.That(pair.generatedRecipe, Is.SameAs(recipe));
        Assert.That(EditorJsonUtility.ToJson(recipe), Is.EqualTo(before));
    }
    [Test]
    public void AuthoredItemSceneManagersLoadTheirMigratedRecipeBooks()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameItemScene.unity", OpenSceneMode.Single);
        var books = new HashSet<MergeRecipeBook>(); int count = 0;
        foreach (var root in scene.GetRootGameObjects()) foreach (var manager in root.GetComponentsInChildren<MergegameManager>(true))
        {
            Assert.That(manager.recipeBook, Is.Not.Null);
            Assert.That(manager.recipeBook.tiers, Is.Not.Empty);
            Assert.That(manager.recipeBook.items, Is.Not.Empty);
            Assert.That(manager.LegacyRecipes, Is.Empty);
            Assert.That(books.Add(manager.recipeBook), Is.True, "Preserve each board's independent configuration.");
            count++;
        }
        Assert.That(count, Is.EqualTo(8));
    }
    [UnityTest]
    public IEnumerator ActualEditorDraggingConnectsMergeItemPorts()
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) Assert.Ignore("실제 Editor 드래그 검사는 그래픽 장치가 필요합니다.");
        var a = Item(); Item(); Item();
        var window = ScriptableObject.CreateInstance<MergeRecipeGraphWindow>();
        try
        {
            window.position = new Rect(40, 40, 1100, 700); window.Show(); yield return null;
            var canvas = window.rootVisualElement.Q<MergeRecipeGraphCanvas>(); canvas.SetGraph(graph, null); yield return null;
            var cards = canvas.Query<VisualElement>("merge-item-node").ToList();
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = cards[0].Q("ItemOutput").worldBound.center })) cards[0].Q("ItemOutput").SendEvent(down);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = cards[2].Q("IngredientA_0").worldBound.center })) canvas.SendEvent(up);
            Assert.That(graph.items[2].inputs[0].first, Is.SameAs(a));
        }
        finally { window.Close(); Object.DestroyImmediate(window); }
    }
}
