using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class GameSessionTransferTests
{
    [Test]
    public void UpgradeGoGameCapturesInventoryAndSkillItemsBeforeSceneTransition()
    {
        var previousSession = GameSession.Instance;
        var previousLoader = SceneMoveManager.instance;
        var host = new GameObject("Upgrade transfer test");
        host.SetActive(false);
        var item = ScriptableObject.CreateInstance<ItemData>();
        try
        {
            var session = host.AddComponent<GameSession>();
            GameSession.Instance = session;
            SceneMoveManager.instance = null;
            Assert.That(session.TryReadGeneralData(out _), Is.False);
            var inventory = host.AddComponent<InventoryManager>();
            inventory.items.Add(new InventoryItem(item, 12));
            var skills = host.AddComponent<BuffSkillManager>();
            // Seed an already unlocked skill; reward registration/execution is a separate system.
            ((List<RegisteredBuffSkillItem>)skills.RegisteredBuffItems).Add(new RegisteredBuffSkillItem(item, "skill-bag"));
            var upgrade = host.AddComponent<UpgradeManager>();
            upgrade.materialInventory = inventory;
            upgrade.buffSkillManager = skills;
            upgrade.GoGame();
            inventory.items[0].amount = 0;
            Assert.That(session.TryReadGeneralData(out var data), Is.True);
            Assert.That(data.materialItems[0].amount, Is.EqualTo(12));
            Assert.That(data.skillItems[0].bagId, Is.EqualTo("skill-bag"));
            Assert.That(session.HasSkillBuffData, Is.True);
        }
        finally
        {
            GameSession.Instance = previousSession;
            SceneMoveManager.instance = previousLoader;
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(item);
        }
    }

    [UnityTearDown]
    public IEnumerator LeavePlayMode()
    {
        if (Application.isPlaying) yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator DataSurvivesSceneUnloadAndDeliversToLateBattleReceiverExactlyOnce()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        var item = ScriptableObject.CreateInstance<ItemData>();
        item.series = ItemSeries.Weapon;
        var definition = ScriptableObject.CreateInstance<MergeItem>();
        definition.item = item;
        var outgoing = SceneManager.CreateScene("Session outgoing test");
        var sessionHost = new GameObject("Session test");
        SceneManager.MoveGameObjectToScene(sessionHost, outgoing);
        var session = sessionHost.AddComponent<GameSession>();
        var currencyHost = new GameObject("Currency test");
        SceneManager.MoveGameObjectToScene(currencyHost, outgoing);
        var currency = currencyHost.AddComponent<CurrencyManager>();
        var boardHost = new GameObject("Board test");
        boardHost.SetActive(false);
        var receiverHost = new GameObject("Receiver test");
        receiverHost.SetActive(false);
        try
        {
            currency.AddCurrency(CurrencyType.Gold, 7);
            int gold = currency.GetCurrency(CurrencyType.Gold);
            var original = new UpgradeToGeneralData();
            original.materialItems.Add(new InventoryItem(item, 5));
            original.skillItems.Add(new SkillBuffSnapshot { itemData = item, bagId = "test-bag" });
            session.SaveUpgradeToGeneral(original);
            original.materialItems[0].amount = 99;
            Assert.That(session.TryReadGeneralData(out var copy), Is.True);
            Assert.That(copy.materialItems[0].amount, Is.EqualTo(5));
            Assert.That(copy.skillItems[0].bagId, Is.EqualTo("test-bag"));
            copy.materialItems[0].amount = 42;

            var board = boardHost.AddComponent<MergegameManager>();
            board.acceptKeyboardInput = false;
            board.prewarmCount = 0;
            board.itemSeries = ItemSeries.Weapon;
            board.mergeItems.Add(definition);
            var bridge = boardHost.AddComponent<GameItemSessionBridge>();
            bridge.boards = new[] { board };
            boardHost.SetActive(true);
            Assert.That(board.currentMaterial.count, Is.EqualTo(5));
            board.currentMaterial.count--;
            bridge.enabled = false;
            bridge.enabled = true;
            Assert.That(board.currentMaterial.count, Is.EqualTo(4), "Re-enabling must not refill spent materials.");

            var first = new GameItemToBattleData();
            first.items.Add(new InventoryItem(item, 2));
            session.SendItemsToBattle(first);
            first.items[0].amount = 100;
            var second = new GameItemToBattleData();
            second.items.Add(new InventoryItem(item, 3));
            session.SendItemsToBattle(second);
            Assert.That(session.PendingBattleBatchCount, Is.EqualTo(2));

            var receiver = receiverHost.AddComponent<BattleItemSessionReceiver>();
            receiverHost.SetActive(true);
            Assert.That(receiver.ReceivedItems[0].amount, Is.EqualTo(5));
            Assert.That(session.PendingBattleBatchCount, Is.Zero);
            receiverHost.SetActive(false);
            receiverHost.SetActive(true);
            Assert.That(receiver.ReceivedItems[0].amount, Is.EqualTo(5));
            Assert.That(session.TryTakeBattleItems(out _), Is.False);

            yield return SceneManager.UnloadSceneAsync(outgoing);
            Assert.That(GameSession.Instance, Is.SameAs(session));
            Assert.That(CurrencyManager.instance, Is.SameAs(currency));
            Assert.That(currency.GetCurrency(CurrencyType.Gold), Is.EqualTo(gold));

            var duplicate = new GameObject("Duplicate session");
            duplicate.AddComponent<GameSession>();
            yield return null;
            Assert.That(duplicate == null, Is.True);
            Assert.That(GameSession.Instance, Is.SameAs(session));

            receiverHost.SetActive(false);
            session.SaveUpgradeToGeneral(new UpgradeToGeneralData());
            receiverHost.SetActive(true);
            Assert.That(receiver.ReceivedItems, Is.Empty);
            Assert.That(board.mergeMaterials, Is.Empty);
            Assert.That(session.HasSkillBuffData, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(boardHost);
            Object.DestroyImmediate(receiverHost);
            Object.DestroyImmediate(sessionHost);
            Object.DestroyImmediate(currencyHost);
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(item);
        }
    }
}
