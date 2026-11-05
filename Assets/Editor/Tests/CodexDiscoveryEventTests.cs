using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class CodexDiscoveryEventTests
{
    private sealed class MemoryStore : ICodexDiscoveryStore
    {
        public string json;
        public int writes;
        public string Read() => json;
        public void Write(string value) { json = value; writes++; }
    }

    private readonly List<Object> owned = new List<Object>();
    private MemoryStore store;
    private CodexDiscoveryHistory history;
    private IDisposable historyScope;

    private GameObject Host(string name, bool active = true)
    {
        var host = new GameObject(name);
        host.SetActive(active);
        owned.Add(host);
        return host;
    }

    private T Asset<T>() where T : ScriptableObject
    {
        var asset = ScriptableObject.CreateInstance<T>();
        owned.Add(asset);
        return asset;
    }

    private ItemData Item(string id)
    {
        var item = Asset<ItemData>();
        item.dataId = id;
        return item;
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        Time.timeScale = 1f;
        InventoryManager.instance = null;
        GameManager.instance = null;
        store = new MemoryStore();
        history = new CodexDiscoveryHistory(store);
        historyScope = CodexDiscoveryHistory.OverrideCurrent(history);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1f;
        for (int i = owned.Count - 1; i >= 0; i--)
            if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
        InventoryManager.instance = null;
        historyScope.Dispose();
        yield return new ExitPlayMode();
    }

    [Test]
    public void OnlyPositiveAcquisitionsDiscoverItemsAndConsumptionDoesNotForgetThem()
    {
        var inventory = Host("Inventory").AddComponent<InventoryManager>();
        var existing = Item("Existing");
        inventory.items.Add(new InventoryItem(existing, 2));
        Assert.That(history.HasItem(existing), Is.False, "Loading or inspecting old stock does not manufacture a discovery event.");

        var acquired = Item("Acquired");
        inventory.AddItem(acquired, 0);
        inventory.AddItem(acquired, -1);
        Assert.That(inventory.GetItemAmount(acquired), Is.Zero);
        Assert.That(history.ItemCount, Is.Zero);

        bool discoveredBeforeUiRefresh = false;
        inventory.onInventoryChanged += () => discoveredBeforeUiRefresh = history.HasItem(acquired);
        inventory.AddItem(acquired, 2);
        Assert.That(discoveredBeforeUiRefresh, Is.True);
        Assert.That(history.HasItem(acquired), Is.True);
        inventory.AddItem(acquired);
        Assert.That(store.writes, Is.EqualTo(1), "Repeated acquisition keeps a single persistent discovery.");
        Assert.That(inventory.RemoveItem(acquired, 3), Is.True);
        Assert.That(history.HasItem(acquired), Is.True);
    }

    [Test]
    public void PotionPreparationAndPreviewDoNotDiscoverTheResultButActualCraftDoes()
    {
        var inventory = Host("Inventory").AddComponent<InventoryManager>();
        var first = Item("Ingredient1");
        var second = Item("Ingredient2");
        var result = Item("Potion");
        inventory.AddItem(first);
        inventory.AddItem(second);
        var recipe = Asset<ItemRecipeData>();
        recipe.materials = new[] {
            new RecipeMaterial { itemData = first, amount = 1 },
            new RecipeMaterial { itemData = second, amount = 1 }
        };
        recipe.resultItem = result;
        var craft = Host("Potion craft").AddComponent<PotionCraftManager>();
        craft.recipes.Add(new ItemRecipeList { recipes = new List<ItemRecipeData> { recipe } });

        Assert.That(craft.TryPrepareRecipe(recipe), Is.True);
        Assert.That(craft.GetPreviewResult(), Is.Null);
        Assert.That(history.HasItem(result), Is.False);
        Assert.That(store.writes, Is.EqualTo(2));
        Assert.That(craft.TryCombine(out var actual), Is.True);
        Assert.That(actual, Is.SameAs(result));
        Assert.That(inventory.GetItemAmount(result), Is.EqualTo(1));
        Assert.That(history.HasItem(result), Is.True);
        Assert.That(store.writes, Is.EqualTo(3));
    }

    private MergeItem MergeDefinition(string id)
    {
        var definition = Asset<MergeItem>();
        definition.item = Item(id);
        definition.radius = .1f;
        var prefab = Host(id + " prefab", false);
        var piece = prefab.AddComponent<MergeItemObject>();
        piece.GetComponent<PooledObject>().SetPoolGroup(PoolObjectGroup.MergeItem);
        definition.prefab = prefab;
        return definition;
    }

    private MergeUI MaterialToggle(string name)
    {
        var host = Host(name);
        host.AddComponent<RectTransform>();
        var ui = host.AddComponent<MergeUI>();
        ui.toggle = host.AddComponent<Toggle>();
        ui.toggle.isOn = true;
        return ui;
    }

    private MergegameManager Board(out MergeItem result)
    {
        var first = MergeDefinition("MergeFirst");
        var second = MergeDefinition("MergeSecond");
        result = MergeDefinition("MergeResult");
        var host = Host("Merge board", false);
        host.transform.position = new Vector3(40000f, 40000f, 0f);
        var pool = host.AddComponent<ObjectPoolManager>();
        pool.registerAsDefaultPool = false;
        pool.poolParent = pool.mergeItemActiveParent = pool.mergeItemPoolParent = host.transform;
        var board = host.AddComponent<MergegameManager>();
        board.objectPool = pool;
        board.spawnPoint = Host("Drop point").transform;
        board.dropInterval = board.resultMergeDelay = board.dropOffset = 0f;
        board.prewarmCount = 0;
        board.logActions = false;
        board.mergeItems.AddRange(new[] { first, second, result });
        board.mergeMaterials.Add(new MergeItemMaterial(first, MaterialToggle("First toggle"), 3, 1));
        board.mergeMaterials.Add(new MergeItemMaterial(second, MaterialToggle("Second toggle"), 3, 1));
        var recipe = Asset<ItemRecipeData>();
        recipe.materials = new[] {
            new RecipeMaterial { itemData = first.item, amount = 1 },
            new RecipeMaterial { itemData = second.item, amount = 1 }
        };
        recipe.resultItem = result.item;
        board.recipeBook = Asset<MergeRecipeBook>();
        board.recipeBook.items = new List<MergeItem>(board.mergeItems);
        board.recipeBook.tiers.Add(new MergeSeries { itemRecipeDatas = new List<ItemRecipeData> { recipe } });
        host.SetActive(true);
        return board;
    }

    private MergeItemObject Drop(MergegameManager board, int index)
    {
        board.spawnPoint.position = new Vector3(40000f + index * 10f, 40000f, 0f);
        board.currentMaterial = board.mergeMaterials[index];
        Assert.That(board.TryDropCurrent(), Is.True);
        return board.ActiveItems[board.ActiveItems.Count - 1];
    }

    [Test]
    public void OnlyCommittedMergeDiscoversResultWithoutAnOptionalRecipeHistoryComponent()
    {
        var board = Board(out var result);
        board.discoveryHistory = null;
        var first = Drop(board, 0);
        var second = Drop(board, 1);
        Assert.That(history.ItemCount, Is.Zero, "Dropping supplied materials is not crafting.");
        var inactive = Host("Inactive result parent", false);
        board.objectPool.mergeItemActiveParent = inactive.transform;
        board.QueueContact(first, second);
        board.ProcessContacts();
        Assert.That(history.HasItem(result.item), Is.False, "A pooled result that fails to spawn must not unlock an entry.");
        Assert.That(first.IsLive && second.IsLive, Is.True);

        board.objectPool.mergeItemActiveParent = board.transform;
        board.QueueContact(first, second);
        board.ProcessContacts();
        Assert.That(history.HasItem(result.item), Is.True);
        Assert.That(store.writes, Is.EqualTo(1));
        board.ClearItems();
        first = Drop(board, 0);
        second = Drop(board, 1);
        board.QueueContact(first, second);
        board.ProcessContacts();
        Assert.That(board.ActiveItems.Count, Is.EqualTo(1));
        Assert.That(store.writes, Is.EqualTo(1));
    }

    private EnemyDataSet EnemyData(string id)
    {
        var data = Asset<EnemyDataSet>();
        data.dataId = id;
        data.statData = Asset<EnemyStatData>();
        return data;
    }

    private ObjectPoolManager EnemyPool(out GameObject prefab)
    {
        var pool = Host("Shared enemy pool").AddComponent<ObjectPoolManager>();
        pool.poolParent = pool.enemyActiveParent = pool.enemyPoolParent = pool.transform;
        prefab = Host("Enemy prefab", false);
        var health = prefab.AddComponent<Health>();
        prefab.AddComponent<ActorTarget>();
        prefab.AddComponent<ActorMover>();
        prefab.AddComponent<ActorAttack>();
        prefab.AddComponent<Enemy>().health = health;
        prefab.AddComponent<PooledObject>().SetPoolGroup(PoolObjectGroup.Enemy);
        return pool;
    }

    [Test]
    public void BattleTargetDefeatRecordsExplicitDisplayDataButPoolingAndNonBattleDefeatsDoNot()
    {
        var battle = SceneManager.CreateScene(GameSceneRoutes.Battle);
        var plantHost = Host("Battle plant", false);
        SceneManager.MoveGameObjectToScene(plantHost, battle);
        var health = plantHost.AddComponent<Health>();
        var plant = plantHost.AddComponent<Plant>();
        plant.health = health;
        plantHost.SetActive(true);
        var plantData = Asset<PlantData>();
        plantData.maxHP = 10f;
        plant.Init(plantData);
        var pool = EnemyPool(out var prefab);
        var data = EnemyData("CombatData");
        data.codexEntry = EnemyData("DisplayData");
        var enemy = pool.Spawn(prefab, Vector3.zero, Quaternion.identity).GetComponent<Enemy>();
        enemy.Init(plant, null, data);
        pool.Release(enemy.gameObject);
        Assert.That(history.EnemyCount, Is.Zero, "Returning an enemy without a real death is not a kill.");

        enemy = pool.Spawn(prefab, Vector3.zero, Quaternion.identity).GetComponent<Enemy>();
        enemy.Init(null, null, data);
        enemy.TakeDamage(100f);
        Assert.That(history.EnemyCount, Is.Zero, "The presence of a loaded battle scene alone does not turn another scene's enemy into a battle kill.");

        enemy = pool.Spawn(prefab, Vector3.zero, Quaternion.identity).GetComponent<Enemy>();
        enemy.Init(plant, null, data);
        enemy.TakeDamage(100f);
        Assert.That(history.HasEnemy(data.codexEntry), Is.True, "A battle target identifies a real battle kill even when its pooled actor belongs to another scene.");
        Assert.That(history.HasEnemy(data), Is.False, "Explicit display data replaces the combat data as the codex entry.");
        Assert.That(store.writes, Is.EqualTo(1));
    }

    [Test]
    public void BattleEnemyWithoutDisplayOverrideRecordsItsOwnDataOnce()
    {
        var battle = SceneManager.CreateScene(GameSceneRoutes.Battle);
        var pool = EnemyPool(out var prefab);
        SceneManager.MoveGameObjectToScene(pool.gameObject, battle);
        var data = EnemyData("NativeEnemyEntry");
        for (int i = 0; i < 2; i++)
        {
            var enemy = pool.Spawn(prefab, Vector3.zero, Quaternion.identity).GetComponent<Enemy>();
            enemy.Init(null, null, data);
            enemy.TakeDamage(100f);
        }
        Assert.That(history.HasEnemy(data), Is.True);
        Assert.That(history.EnemyCount, Is.EqualTo(1));
        Assert.That(store.writes, Is.EqualTo(1));
    }
}
