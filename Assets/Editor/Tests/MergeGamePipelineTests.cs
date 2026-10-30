using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class MergeGamePipelineTests
{
    private readonly List<Object> owned = new List<Object>();
    private MergegameManager board;
    private MergeItem first, second, result;
    private sealed class DiscoveryStore : IMergeDiscoveryStore
    {
        public string json;
        public int writes;
        public string Read() => json;
        public void Write(string value) { json = value; writes++; }
        public void Delete() => json = null;
    }
    private GameObject Host(string name, bool active = true)
    {
        var host = new GameObject(name); host.SetActive(active); owned.Add(host); return host;
    }
    private T Asset<T>() where T : ScriptableObject
    {
        var value = ScriptableObject.CreateInstance<T>(); owned.Add(value); return value;
    }
    private MergeItem Definition(string name)
    {
        var item = Asset<ItemData>(); item.name = name;
        var definition = Asset<MergeItem>(); definition.item = item; definition.radius = .1f;
        var prefab = Host(name + " prefab", false);
        var piece = prefab.AddComponent<MergeItemObject>();
        piece.GetComponent<PooledObject>().SetPoolGroup(PoolObjectGroup.MergeItem);
        definition.prefab = prefab;
        return definition;
    }
    private MergeUI Toggle(string name)
    {
        var host = Host(name); host.AddComponent<RectTransform>();
        var ui = host.AddComponent<MergeUI>(); ui.toggle = host.AddComponent<Toggle>(); ui.toggle.isOn = true;
        return ui;
    }
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        Time.timeScale = 1;
        first = Definition("First"); second = Definition("Second"); result = Definition("Result");
        var host = Host("Board", false); host.transform.position = new Vector3(40000, 40000, 0);
        var pool = host.AddComponent<ObjectPoolManager>();
        pool.registerAsDefaultPool = false;
        pool.poolParent = pool.mergeItemActiveParent = pool.mergeItemPoolParent = host.transform;
        board = host.AddComponent<MergegameManager>();
        board.objectPool = pool; board.spawnPoint = host.transform;
        board.dropInterval = board.resultMergeDelay = board.dropOffset = 0;
        board.prewarmCount = 0; board.logActions = false;
        board.mergeItems.AddRange(new[] { first, second, result });
        board.mergeMaterials.Add(new MergeItemMaterial(first, Toggle("First toggle"), 3, 1));
        board.mergeMaterials.Add(new MergeItemMaterial(second, Toggle("Second toggle"), 3, 1));
        var recipe = Asset<ItemRecipeData>();
        recipe.materials = new[] { new RecipeMaterial { itemData = first.item, amount = 1 }, new RecipeMaterial { itemData = second.item, amount = 1 } };
        recipe.resultItem = result.item;
        board.recipeBook = Asset<MergeRecipeBook>();
        board.recipeBook.items = new List<MergeItem>(board.mergeItems);
        board.recipeBook.tiers.Add(new MergeSeries { tier = 0, itemRecipeDatas = new List<ItemRecipeData> { recipe } });
        host.SetActive(true);
    }
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1;
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
        yield return new ExitPlayMode();
    }
    private MergeItemObject Drop(int index, float x)
    {
        board.spawnPoint.position = new Vector3(x, 40000, 0);
        board.currentMaterial = board.mergeMaterials[index];
        Assert.That(board.TryDropCurrent(), Is.True);
        return board.ActiveItems[board.ActiveItems.Count - 1];
    }

    [Test]
    public void OnlyCommittedMergesRecordDiscoveriesAndRepeatedCraftsDoNotSaveAgain()
    {
        var store = new DiscoveryStore();
        var history = Host("Discovery").AddComponent<MergeDiscoveryHistory>();
        history.SetSaveStore(store);
        board.discoveryHistory = history;
        result.item.dataId = "Result";
        var a = Drop(0, 40000); var b = Drop(1, 40010);
        Assert.That(history.Count, Is.Zero, "Dropping an item is not crafting.");
        var prefab = result.prefab;
        result.prefab = null;
        board.QueueContact(a, b); board.ProcessContacts();
        Assert.That(history.Count, Is.Zero, "A failed merge must not discover the result.");
        result.prefab = prefab;
        board.QueueContact(a, b); board.ProcessContacts();
        Assert.That(history.HasCrafted(result.item), Is.True);
        Assert.That(store.writes, Is.EqualTo(1));
        board.ClearItems();
        a = Drop(0, 40000); b = Drop(1, 40010);
        board.QueueContact(a, b); board.ProcessContacts();
        Assert.That(board.ActiveItems.Count, Is.EqualTo(1));
        Assert.That(store.writes, Is.EqualTo(1));
    }

    [Test]
    public void DroppingAndCombiningUseTheirInjectedDataAndTheSharedPool()
    {
        var a = Drop(0, 40000); var b = Drop(1, 40010);
        a.Velocity = new Vector2(2, 0); b.Velocity = new Vector2(0, 2);
        board.QueueContact(a, b); board.QueueContact(b, a); board.ProcessContacts();
        Assert.That(board.ActiveItems.Count, Is.EqualTo(1));
        Assert.That(board.ActiveItems[0].Definition, Is.SameAs(result));
        Assert.That(board.ActiveItems[0].Velocity, Is.EqualTo(Vector2.one));
        Assert.That(board.ActiveItems[0].circleCollider.radius, Is.EqualTo(result.radius));
        Assert.That(board.mergeMaterials[0].defaultCount, Is.Zero);
        Assert.That(board.mergeMaterials[0].count, Is.EqualTo(3));
        board.ClearItems();
        Assert.That(board.ActiveItems, Is.Empty);
        Assert.That(board.TryDropCurrent(), Is.True);
    }

    [Test]
    public void MissingResultPrefabOrMissingRecipeLeavesOriginalItemsUsable()
    {
        var a = Drop(0, 40000); var b = Drop(1, 40010);
        result.prefab = null;
        board.QueueContact(a, b); board.ProcessContacts();
        Assert.That(board.ActiveItems.Count, Is.EqualTo(2));
        Assert.That(a.CanMerge && b.CanMerge, Is.True);
        board.recipeBook.tiers.Clear();
        board.QueueContact(a, b); board.ProcessContacts();
        Assert.That(board.ActiveItems.Count, Is.EqualTo(2));
    }

    [Test]
    public void FailedResultSpawnReleasesReservationsAndNeverConsumesTheIngredients()
    {
        var a = Drop(0, 40000); var b = Drop(1, 40010);
        var inactive = Host("Inactive result parent", false);
        board.objectPool.mergeItemActiveParent = inactive.transform;
        board.QueueContact(a, b); board.ProcessContacts();
        Assert.That(board.ActiveItems.Count, Is.EqualTo(2));
        Assert.That(a.IsLive && b.IsLive && a.CanMerge && b.CanMerge, Is.True);
        board.objectPool.mergeItemActiveParent = board.transform;
        board.QueueContact(a, b); board.ProcessContacts();
        Assert.That(board.ActiveItems.Count, Is.EqualTo(1));
        Assert.That(board.ActiveItems[0].Definition, Is.SameAs(result));
    }

    private void RequireThreeIngredients()
    {
        board.recipeBook.tiers[0].itemRecipeDatas[0].materials = new[] {
            new RecipeMaterial { itemData = first.item, amount = 2 },
            new RecipeMaterial { itemData = second.item, amount = 1 }
        };
    }
    [Test]
    public void ThreeConnectedIngredientsConsumeExactQuantitiesAndPreserveMomentum()
    {
        RequireThreeIngredients();
        var a = Drop(0, 40000); var b = Drop(1, 40010); var c = Drop(0, 40020); var extra = Drop(0, 40030);
        a.Velocity = Vector2.right * 3; b.Velocity = Vector2.up * 3; c.Velocity = Vector2.zero;
        float expectedX = (a.transform.position.x + b.transform.position.x + c.transform.position.x) / 3;
        board.QueueContact(a, b); board.QueueContact(b, c); board.QueueContact(c, extra); board.ProcessContacts();
        Assert.That(board.ActiveItems.Count, Is.EqualTo(2));
        Assert.That(extra.IsLive && extra.CanMerge, Is.True);
        MergeItemObject merged = null;
        foreach (var item in board.ActiveItems) if (item.Definition == result) merged = item;
        Assert.That(merged, Is.Not.Null); Assert.That(merged.Velocity, Is.EqualTo(Vector2.one));
        Assert.That(merged.transform.position.x, Is.EqualTo(expectedX).Within(.01f));
    }
    [Test]
    public void NIngredientRecipeCannotConsumeMissingOrDisconnectedPieces()
    {
        RequireThreeIngredients();
        var a = Drop(0, 40000); var b = Drop(1, 40010); var c = Drop(0, 40020);
        board.QueueContact(a, b); board.ProcessContacts();
        Assert.That(board.ActiveItems.Count, Is.EqualTo(3)); Assert.That(c.IsLive, Is.True);
        board.QueueContact(a, b); board.QueueContact(b, c); board.ProcessContacts();
        Assert.That(board.ActiveItems.Count, Is.EqualTo(1)); Assert.That(board.ActiveItems[0].Definition, Is.SameAs(result));
    }
    [Test]
    public void FailedNIngredientResultSpawnReturnsEveryIngredientReservation()
    {
        RequireThreeIngredients();
        var a = Drop(0, 40000); var b = Drop(1, 40010); var c = Drop(0, 40020);
        var inactive = Host("Inactive N result parent", false); board.objectPool.mergeItemActiveParent = inactive.transform;
        board.QueueContact(a, b); board.QueueContact(b, c); board.ProcessContacts();
        Assert.That(board.ActiveItems.Count, Is.EqualTo(3)); Assert.That(a.CanMerge && b.CanMerge && c.CanMerge, Is.True);
        board.objectPool.mergeItemActiveParent = board.transform;
        board.QueueContact(a, b); board.QueueContact(b, c); board.ProcessContacts();
        Assert.That(board.ActiveItems.Count, Is.EqualTo(1));
    }
    [Test]
    public void SingleIngredientRecipeUsesOnlyItsOneRequiredPiece()
    {
        board.recipeBook.tiers[0].itemRecipeDatas[0].materials = new[] { new RecipeMaterial { itemData = first.item, amount = 1 } };
        var a = Drop(0, 40000); var b = Drop(1, 40010);
        board.QueueContact(a, b); board.ProcessContacts();
        Assert.That(board.ActiveItems.Count, Is.EqualTo(2)); Assert.That(b.IsLive, Is.True);
        Assert.That(board.ActiveItems[1].Definition, Is.SameAs(result));
    }

    [UnityTest]
    public IEnumerator AutomaticDropSkipsUncheckedMaterialsAndStopsWithoutConsumingStock()
    {
        board.mergeMaterials[0].mergeUI.toggle.isOn = false;
        board.dropInterval = .03f; board.automaticDrop = true;
        board.SetFeeding(true);
        float deadline = Time.realtimeSinceStartup + 5;
        while (board.ActiveItems.Count == 0 && Time.realtimeSinceStartup < deadline) yield return null;
        board.SetFeeding(false);
        Assert.That(board.ActiveItems.Count, Is.EqualTo(1));
        Assert.That(board.ActiveItems[0].Definition, Is.SameAs(second));
        Assert.That(board.mergeMaterials[0].defaultCount, Is.EqualTo(1));
        int remaining = board.mergeMaterials[1].count;
        float until = Time.time + .1f;
        while (Time.time < until) yield return null;
        Assert.That(board.mergeMaterials[1].count, Is.EqualTo(remaining));
        Assert.That(board.ActiveItems.Count, Is.EqualTo(1));
    }
}
