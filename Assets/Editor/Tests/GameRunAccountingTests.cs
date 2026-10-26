using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class GameRunAccountingTests
{
    private List<Object> owned;
    private GameSession session;
    private ItemData item;
    private MergeItem definition;

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

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        owned = new List<Object>();
        Time.timeScale = 1f;
        session = Host("Session").AddComponent<GameSession>();
        item = Asset<ItemData>();
        item.series = ItemSeries.Weapon;
        item.name = "Material";
        definition = Asset<MergeItem>();
        definition.item = item;
        definition.radius = .1f;
        var prefab = Host("Merge prefab", false);
        var piece = prefab.AddComponent<MergeItemObject>();
        piece.GetComponent<PooledObject>().SetPoolGroup(PoolObjectGroup.MergeItem);
        definition.prefab = prefab;
        var data = new UpgradeToGeneralData();
        data.materialItems.Add(new InventoryItem(item, 3));
        session.SaveUpgradeToGeneral(data);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1f;
        for (int i = owned.Count - 1; i >= 0; i--)
            if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
        yield return new ExitPlayMode();
    }

    private MergegameManager Board(int bonus, float x)
    {
        var host = Host("Board", false);
        host.transform.position = new Vector3(x, 40000f, 0f);
        var pool = host.AddComponent<ObjectPoolManager>();
        pool.registerAsDefaultPool = false;
        pool.poolParent = pool.mergeItemActiveParent = pool.mergeItemPoolParent = host.transform;
        var board = host.AddComponent<MergegameManager>();
        board.objectPool = pool;
        board.spawnPoint = host.transform;
        board.acceptKeyboardInput = false;
        board.dropInterval = 0f;
        board.prewarmCount = 0;
        board.dropOffset = 0f;
        board.logActions = false;
        board.itemSeries = item.series;
        board.mergeItems.Add(definition);
        board.mergeMaterials.Add(new MergeItemMaterial(definition, null, 0, bonus));
        var bridge = host.AddComponent<GameItemSessionBridge>();
        bridge.boards = new[] { board };
        host.SetActive(true);
        return board;
    }

    private static void DropAndClear(MergegameManager board)
    {
        Assert.That(board.TryDropCurrent(), Is.True);
        Assert.That(board.ActiveItems.Count, Is.EqualTo(1));
        board.ClearItems();
    }

    [Test]
    public void BonusIsSpentFirstAndOnlyMainStockChangesTheReturningInventory()
    {
        var board = Board(2, 40000f);
        var material = board.mergeMaterials[0];
        DropAndClear(board);
        DropAndClear(board);
        Assert.That(material.defaultCount, Is.Zero);
        Assert.That(material.count, Is.EqualTo(3));
        Assert.That(session.GetMaterialCount(item), Is.EqualTo(3));
        DropAndClear(board);
        Assert.That(material.count, Is.EqualTo(2));
        Assert.That(session.GetMaterialCount(item), Is.EqualTo(2));

        var inventory = Host("Upgrade inventory", false).AddComponent<InventoryManager>();
        Assert.That(session.RestoreMaterialInventory(inventory), Is.True);
        Assert.That(inventory.GetItemAmount(item), Is.EqualTo(2));
        inventory.items[0].amount = 99;
        Assert.That(session.GetMaterialCount(item), Is.EqualTo(2), "Restored inventory must not alias the session snapshot.");

        board.enabled = false;
        board.enabled = true;
        Assert.That(material.defaultCount, Is.Zero, "Toggling a board must not refill bonuses.");
        session.BeginRound();
        Assert.That(material.defaultCount, Is.EqualTo(2));
        Assert.That(material.count, Is.EqualTo(2), "Retry refills the bonus, not the inventory stock.");
    }

    [Test]
    public void SharedBoardsCannotDuplicateTheLastMainMaterial()
    {
        var first = Board(0, 40000f);
        var second = Board(0, 40010f);
        DropAndClear(first);
        Assert.That(second.mergeMaterials[0].count, Is.EqualTo(2));
        DropAndClear(second);
        DropAndClear(first);
        Assert.That(second.TryDropCurrent(), Is.False);
        Assert.That(session.GetMaterialCount(item), Is.Zero);

        var inventory = Host("Empty returning inventory", false).AddComponent<InventoryManager>();
        inventory.items.Add(new InventoryItem(item, 99));
        Assert.That(session.RestoreMaterialInventory(inventory), Is.True);
        Assert.That(inventory.GetItemAmount(item), Is.Zero, "An empty saved inventory must replace old/default stock too.");
    }

    [TestCase(0)]
    [TestCase(2)]
    public void FailedPooledSpawnRestoresTheSameStockType(int bonus)
    {
        var board = Board(bonus, 40000f);
        var inactiveParent = Host("Inactive pool destination", false);
        board.objectPool.mergeItemActiveParent = inactiveParent.transform;
        Assert.That(board.TryDropCurrent(), Is.False);
        Assert.That(board.mergeMaterials[0].defaultCount, Is.EqualTo(bonus));
        Assert.That(board.mergeMaterials[0].count, Is.EqualTo(3));
        Assert.That(session.GetMaterialCount(item), Is.EqualTo(3));
        Assert.That(board.ActiveItems, Is.Empty);
    }

    [Test]
    public void BonusOnlyMaterialStaysAvailableWithoutAnyOwnedStock()
    {
        session.SaveUpgradeToGeneral(new UpgradeToGeneralData());
        var board = Board(1, 40000f);
        Assert.That(board.currentMaterial, Is.SameAs(board.mergeMaterials[0]));
        DropAndClear(board);
        Assert.That(board.TryDropCurrent(), Is.False);
        Assert.That(session.GetMaterialCount(item), Is.Zero);
    }

    [UnityTest]
    public IEnumerator MainStockSurvivesUnloadingItemSceneAndRestoresBeforeUpgradeDefaults()
    {
        var outgoing = SceneManager.CreateScene("Item stock test");
        var board = Board(0, 40000f);
        SceneManager.MoveGameObjectToScene(board.gameObject, outgoing);
        DropAndClear(board);
        yield return SceneManager.UnloadSceneAsync(outgoing);
        Assert.That(session.GetMaterialCount(item), Is.EqualTo(2));

        var host = Host("Returning Upgrade", false);
        var inventory = host.AddComponent<InventoryManager>();
        var defaults = host.AddComponent<ItemInitManager>();
        defaults.setAmount = 99;
        defaults.items = new[] { new TestItemSet { isUse = true, itemDatas = new[] { item } } };
        var upgrade = host.AddComponent<UpgradeManager>();
        upgrade.itemInitManager = defaults;
        upgrade.materialInventory = inventory;
        host.SetActive(true);
        yield return null;
        Assert.That(inventory.GetItemAmount(item), Is.EqualTo(2), "Returning must not grant default inventory again.");
    }

    private CurrencyUIGroup CurrencyGroup(CurrencyDisplaySource source)
    {
        var host = Host("Currency display", false);
        var label = new GameObject("Amount", typeof(RectTransform));
        owned.Add(label);
        var text = label.AddComponent<TextMeshProUGUI>();
        var group = host.AddComponent<CurrencyUIGroup>();
        group.source = source;
        group.currencyUIs = new[] { new CurrencyUIGroup.CurrencyUI { type = CurrencyType.CurrencyMain, textUI = text } };
        host.SetActive(true);
        return group;
    }

    [Test]
    public void RoundNavigationBindsLateIgnoresWalletChangesAndResultConfirmationPaysOnce()
    {
        var navigation = CurrencyGroup(CurrencyDisplaySource.CurrentGame);
        Assert.That(navigation.currencyUIs[0].textUI.text, Is.EqualTo("0"));
        var wallet = Host("Wallet").AddComponent<CurrencyManager>();
        int initial = wallet.GetCurrency(CurrencyType.CurrencyMain);
        var statistics = Host("Statistics").AddComponent<GameStatisticsManager>();
        statistics.AddKill(2);
        statistics.AddGold(7);
        wallet.AddCurrency(CurrencyType.CurrencyMain, 50);
        Assert.That(navigation.currencyUIs[0].textUI.text, Is.EqualTo("7"));

        var panel = Host("Authored result", false);
        var title = new GameObject("Result title", typeof(RectTransform));
        owned.Add(title);
        var titleText = title.AddComponent<TextMeshProUGUI>();
        var end = Host("End").AddComponent<GameEndManager>();
        SetPrivate(end, "resultPanel", panel);
        SetPrivate(end, "titleText", titleText);
        SetPrivate(end, "includeKillCountInTitle", true);
        end.LoseGame();
        Assert.That(panel.activeSelf, Is.True);
        Assert.That(titleText.text, Does.Contain("2"));
        statistics.AddGold(100);
        statistics.AddKill();
        Assert.That(statistics.RoundKillCount, Is.EqualTo(2));
        Assert.That(statistics.RoundGold, Is.EqualTo(7));
        statistics.Confirm();
        statistics.Confirm();
        Assert.That(wallet.GetCurrency(CurrencyType.CurrencyMain), Is.EqualTo(initial + 50 + 7));
        Assert.That(navigation.currencyUIs[0].textUI.text, Is.EqualTo("7"));

        statistics.ResetRound();
        end.ResetGameEnd();
        Assert.That(navigation.currencyUIs[0].textUI.text, Is.EqualTo("0"));
        statistics.AddGold(3);
        statistics.Confirm();
        Assert.That(wallet.GetCurrency(CurrencyType.CurrencyMain), Is.EqualTo(initial + 50 + 10));
    }

    private static void SetPrivate(Object component, string field, object value)
        => component.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(component, value);

    [Test]
    public void EnemyDeathRecordsOneKillAndRewardPerLife()
    {
        var statistics = Host("Statistics").AddComponent<GameStatisticsManager>();
        var pool = Host("Enemy pool").AddComponent<ObjectPoolManager>();
        pool.poolParent = pool.enemyActiveParent = pool.enemyPoolParent = pool.transform;
        var prefab = Host("Enemy prefab", false);
        var health = prefab.AddComponent<Health>();
        prefab.AddComponent<Enemy>().health = health;
        prefab.AddComponent<PooledObject>().SetPoolGroup(PoolObjectGroup.Enemy);
        var data = Asset<EnemyDataSet>();
        data.statData = Asset<EnemyStatData>();
        data.statData.reward = new[] { new Cost { currencyType = CurrencyType.CurrencyMain, amount = 4 } };
        var enemy = pool.Spawn(prefab, Vector3.zero, Quaternion.identity).GetComponent<Enemy>();
        enemy.Init(null, null, data);
        enemy.TakeDamage(100f);
        enemy.TakeDamage(100f);
        Assert.That(statistics.RoundKillCount, Is.EqualTo(1));
        Assert.That(statistics.RoundGold, Is.EqualTo(4));
        var reused = pool.Spawn(prefab, Vector3.zero, Quaternion.identity).GetComponent<Enemy>();
        Assert.That(reused, Is.SameAs(enemy));
        reused.Init(null, null, data);
        reused.TakeDamage(100f);
        Assert.That(statistics.RoundKillCount, Is.EqualTo(2));
        Assert.That(statistics.RoundGold, Is.EqualTo(8));
    }
}
