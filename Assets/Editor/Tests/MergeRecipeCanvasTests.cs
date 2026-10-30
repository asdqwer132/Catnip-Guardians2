using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public abstract class MergeRecipeCanvasFixture
{
    private readonly List<Object> owned = new List<Object>();
    protected string saveDirectory;
    protected GameObject Host(string name, bool active = true)
    {
        var host = new GameObject(name, typeof(RectTransform));
        host.SetActive(active);
        owned.Add(host);
        return host;
    }
    protected T Asset<T>() where T : ScriptableObject
    {
        var asset = ScriptableObject.CreateInstance<T>(); owned.Add(asset); return asset;
    }
    protected ItemData Item(string id)
    {
        var item = Asset<ItemData>(); item.dataId = id; item.name = id; return item;
    }
    protected MergeDiscoveryHistory History()
    {
        var history = Host("History").AddComponent<MergeDiscoveryHistory>();
        history.SetSaveStore(new MergeDiscoveryFileStore(Path.Combine(saveDirectory, "history.json")));
        return history;
    }

    [SetUp]
    public void SetUp()
    {
        if (!Application.isPlaying) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        saveDirectory = Path.Combine(Path.GetTempPath(), "CatnipRecipeTests", Guid.NewGuid().ToString("N"));
    }
    [TearDown]
    public void TearDown()
    {
        Undo.ClearAll();
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
        if (Directory.Exists(saveDirectory)) Directory.Delete(saveDirectory, true);
        if (!Application.isPlaying) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    [UnityTearDown]
    public IEnumerator LeavePlayMode()
    {
        if (Application.isPlaying) yield return new ExitPlayMode();
    }

    protected MergeRecipeSeriesUI Panel()
    {
        var host = Host("Weapon");
        var panel = host.AddComponent<MergeRecipeSeriesUI>();
        panel.scrollView = host.AddComponent<ScrollRect>();
        panel.scrollView.viewport = host.GetComponent<RectTransform>();
        var content = Host("Content").GetComponent<RectTransform>();
        content.SetParent(host.transform, false);
        panel.scrollView.content = content;
        var node = Host("Authored Node").AddComponent<MergeRecipeNodeUI>();
        ((RectTransform)node.transform).sizeDelta = new Vector2(100, 100);
        node.icon = Host("Icon").AddComponent<Image>(); node.icon.transform.SetParent(node.transform, false);
        panel.nodeTemplate = node;
        var line = Host("Authored Line").AddComponent<RecipeTreeLineUI>();
        line.lineImage = line.gameObject.AddComponent<Image>();
        panel.lineTemplate = line;
        panel.recipeBook = Asset<MergeRecipeBook>();
        return panel;
    }
    protected ItemRecipeData Recipe(ItemData result, params ItemData[] inputs)
    {
        var recipe = Asset<ItemRecipeData>(); recipe.resultItem = result;
        recipe.materials = inputs.Select(item => new RecipeMaterial { itemData = item, amount = 1 }).ToArray();
        return recipe;
    }

}

public sealed class MergeRecipeCanvasTests : MergeRecipeCanvasFixture
{
    [Test]
    public void FirstCraftIsSavedByStableItemIdAndDeleteSurvivesReload()
    {
        var item = Item("Weapon20");
        var history = History();
        int changes = 0; history.Changed += () => changes++;
        Assert.That(history.RecordCrafted(item), Is.True);
        Assert.That(history.RecordCrafted(item), Is.False);
        var equivalent = Item(item.dataId);
        Assert.That(history.RecordCrafted(equivalent), Is.False);
        Assert.That(history.Count, Is.EqualTo(1));
        Assert.That(changes, Is.EqualTo(1));
        var reloaded = History();
        Assert.That(reloaded.HasCrafted(equivalent), Is.True);
        reloaded.ClearHistory();
        Assert.That(reloaded.Count, Is.Zero);
        Assert.That(File.Exists(Path.Combine(saveDirectory, "history.json")), Is.False);
        Assert.That(History().HasCrafted(item), Is.False);
        Assert.That(reloaded.RecordCrafted(item), Is.True);
        Assert.That(History().HasCrafted(item), Is.True);
    }

    [Test]
    public void EditorBuildPreservesDuplicateResultsVariableInputsAmountsAndAuthoredObjects()
    {
        var panel = Panel();
        var decoration = Host("Authored Decoration"); decoration.transform.SetParent(panel.scrollView.content, false);
        var a = Item("A"); var b = Item("B"); var c = Item("C"); var d = Item("D");
        var first = Recipe(c, a, b); var duplicate = Recipe(c, a, a); var next = Recipe(d, c, a, b, b);
        panel.recipeBook.tiers.Add(new MergeSeries { tier = 0, itemRecipeDatas = new List<ItemRecipeData> { first, duplicate } });
        panel.recipeBook.tiers.Add(new MergeSeries { tier = 5, itemRecipeDatas = new List<ItemRecipeData> { next } });
        Assert.That(MergeRecipeCanvasBuilder.Build(panel), Is.True);
        Assert.That(panel.Nodes.Count, Is.EqualTo(5));
        Assert.That(panel.Nodes.Count(node => node.item == c), Is.EqualTo(2));
        Assert.That(panel.Connections.Count, Is.EqualTo(6));
        Assert.That(panel.Connections.Single(line => line.recipe == duplicate).line.requiredAmount, Is.EqualTo(2));
        Assert.That(panel.Connections.Single(line => line.recipe == next && line.from.item == b).line.requiredAmount, Is.EqualTo(2));
        var baseNode = panel.Nodes.Single(node => node.item == a);
        var resultNode = panel.Nodes.First(node => node.item == c);
        var nextNode = panel.Nodes.Single(node => node.item == d);
        Assert.That(baseNode.transform.localPosition.x, Is.LessThan(resultNode.transform.localPosition.x));
        Assert.That(resultNode.transform.localPosition.x, Is.LessThan(nextNode.transform.localPosition.x));
        foreach (var connection in panel.Connections)
        {
            connection.line.RefreshPosition();
            var from = connection.line.from;
            var to = connection.line.to;
            var rect = (RectTransform)connection.line.transform;
            Assert.That(Vector3.Distance(rect.TransformPoint(Vector3.zero), from.TransformPoint(from.rect.center)), Is.LessThan(.001f));
            Assert.That(Vector3.Distance(rect.TransformPoint(new Vector3(rect.rect.width, 0, 0)), to.TransformPoint(to.rect.center)), Is.LessThan(.001f));
        }
        var originalRoot = panel.GeneratedRoot;
        Assert.That(MergeRecipeCanvasBuilder.Build(panel), Is.True);
        Assert.That(originalRoot == null, Is.True);
        Assert.That(decoration != null, Is.True);
        Assert.That(panel.scrollView.content.childCount, Is.EqualTo(2));
        Undo.PerformUndo();
        Assert.That(panel.GeneratedRoot != null, Is.True);
        Assert.That(panel.Nodes.Count, Is.EqualTo(5));
        Assert.That(panel.Connections.All(line => line.line != null && line.from != null && line.to != null), Is.True);
        Undo.PerformRedo();
        Assert.That(panel.Nodes.Count, Is.EqualTo(5));
        Assert.That(panel.Connections.All(line => line.line != null && line.from != null && line.to != null), Is.True,
            string.Join("; ", panel.Connections.Select(line => $"Line: {line.line != null}, From: {line.from != null}, To: {line.to != null}")));
    }

    [Test]
    public void AuthoredItemSceneConnectsEightSeriesTheExistingDropdownAndAllMergeBoards()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameItemScene.unity", OpenSceneMode.Single);
        var canvas = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MergeRecipeCanvas>(true)).Single();
        var boxes = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MergeBoxSceneController>(true)).Single();
        Assert.That(canvas.history != null, Is.True);
        Assert.That(canvas.seriesDropdown, Is.SameAs(boxes.seriesDropdown));
        Assert.That(canvas.series.Length, Is.EqualTo(8));
        for (int i = 0; i < 8; i++)
        {
            var panel = canvas.series[i];
            Assert.That(panel != null, Is.True);
            Assert.That(panel.series, Is.EqualTo(boxes.boxes[i].board.itemSeries));
            Assert.That(boxes.boxes[i].board.discoveryHistory, Is.SameAs(canvas.history));
            Assert.That(panel.nodeTemplate != null && panel.lineTemplate != null && panel.scrollView.content != null, Is.True);
            if (panel.recipeBook != null)
            {
                Assert.That(panel.Nodes.Count, Is.GreaterThan(0));
                Assert.That(panel.Nodes.All(node => node != null && node.item != null && node.icon != null && node.icon.sprite == node.item.icon), Is.True);
                Assert.That(panel.Connections.All(connection => connection.recipe != null && connection.line != null && connection.from != null && connection.to != null), Is.True);
            }
        }
        Assert.That(canvas.series[0].recipeBook, Is.SameAs(AssetDatabase.LoadAssetAtPath<MergeRecipeBook>(
            "Assets/Data/Scriptable/Item/MergeRecipe/01.Weapon/01.MergeRecipeGraph Book.asset")));
        Assert.That(canvas.series[0].Nodes.Count, Is.GreaterThan(30));
    }
}

public sealed class MergeRecipeCanvasPlayTests : MergeRecipeCanvasFixture
{
    [UnitySetUp]
    public IEnumerator EnterGameplay()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
    }

    [Test]
    public void CraftedFilterRefreshesOnFirstCraftAndAfterCanvasReopensWithoutChangingLayout()
    {
        var panel = Panel();
        var a = Item("A"); var b = Item("B"); var c = Item("C");
        // A small authored layout, without calling the Editor builder in Play Mode.
        var nodes = new List<MergeRecipeNodeUI>();
        foreach (var item in new[] { a, b, c })
        {
            var node = Host(item.name).AddComponent<MergeRecipeNodeUI>();
            node.item = item; node.transform.SetParent(panel.scrollView.content, false);
            nodes.Add(node);
        }
        var connections = new List<MergeRecipeConnection>();
        foreach (var input in nodes.Take(2))
        {
            var line = Host("Connection").AddComponent<RecipeTreeLineUI>();
            line.transform.SetParent(panel.scrollView.content, false);
            line.Setup((RectTransform)input.transform, (RectTransform)nodes[2].transform, 1);
            connections.Add(new MergeRecipeConnection { from = input, to = nodes[2], line = line });
        }
        panel.SetEditorLayout(panel.scrollView.content, nodes, connections);
        var secondPanel = Panel();
        var history = History();
        var host = Host("Recipe Canvas", false);
        var canvas = host.AddComponent<MergeRecipeCanvas>();
        canvas.history = history; canvas.series = new[] { panel, secondPanel }; canvas.onlyShowCraftedItems = true;
        host.SetActive(true);
        Assert.That(panel.gameObject.activeSelf, Is.True);
        Assert.That(secondPanel.gameObject.activeSelf, Is.False);
        Assert.That(panel.Nodes.All(node => !node.gameObject.activeSelf), Is.True);
        history.RecordCrafted(c);
        Assert.That(panel.Nodes.Single(node => node.item == c).gameObject.activeSelf, Is.True);
        Assert.That(panel.Connections.All(line => !line.line.gameObject.activeSelf), Is.True);
        var position = panel.Nodes[0].transform.localPosition;
        host.SetActive(false);
        history.RecordCrafted(a); history.RecordCrafted(b);
        host.SetActive(true);
        Assert.That(panel.Nodes.All(node => node.gameObject.activeSelf), Is.True);
        Assert.That(panel.Connections.All(line => line.line.gameObject.activeSelf), Is.True);
        Assert.That(panel.Nodes[0].transform.localPosition, Is.EqualTo(position));
        canvas.SelectSeriesIndex(1);
        Assert.That(panel.gameObject.activeSelf, Is.False);
        Assert.That(secondPanel.gameObject.activeSelf, Is.True);
        canvas.DeleteSavedCraftingHistory();
        Assert.That(panel.Nodes.All(node => !node.gameObject.activeSelf), Is.True);
        canvas.SetOnlyShowCraftedItems(false);
        Assert.That(panel.Nodes.All(node => node.gameObject.activeSelf), Is.True);
    }

}
