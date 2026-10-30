using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public abstract class MergeBoxUpgradeFixture : MergeBoxLevelTestFixture
{
    protected string path;
    protected SkillNodeData rootNode, boxNode;
    protected CurrencyManager wallet;
    protected readonly List<ItemSeries> series = new List<ItemSeries> {
        ItemSeries.Weapon, ItemSeries.Food, ItemSeries.Mineral, ItemSeries.Monster,
        ItemSeries.Fishing, ItemSeries.Plant, ItemSeries.Machine, ItemSeries.Magic };

    [SetUp] public void UpgradeSetUp()
    {
        path = Path.Combine(Path.GetTempPath(), "CatnipBoxUpgradeTests", Guid.NewGuid().ToString("N"), "progress.json");
        rootNode = Node("Root"); boxNode = Node("Box upgrade"); boxNode.skillMap = rootNode.skillMap;
        boxNode.requiredSkills.Add(rootNode);
        var reward = Asset<MergeBoxLevelSkillReward>(); reward.series = ItemSeries.Weapon; reward.level = 2;
        boxNode.rewards.Add(reward);
        wallet = Host("Wallet", false).AddComponent<CurrencyManager>();
        CurrencyManager.instance = wallet; wallet.AddCurrency(CurrencyType.CurrencyMain, 100);
    }
    [TearDown] public void UpgradeTearDown()
    {
        if (Directory.Exists(Path.GetDirectoryName(path))) Directory.Delete(Path.GetDirectoryName(path), true);
        CurrencyManager.instance = null;
    }
    protected SkillNodeData Node(string id)
    {
        var node = Asset<SkillNodeData>(); node.dataId = id;
        node.skillMap = Asset<SkillMapData>(); node.skillMap.dataId = id + " map";
        node.costs = new[] { new Cost { currencyType = CurrencyType.CurrencyMain, amount = 10 } };
        return node;
    }
    protected MergeBoxUpgradeManager Progress(bool activate = false)
    {
        var host = Host("Upgrade progress", false);
        var tree = host.AddComponent<SkillTreeManager>();
        var serialized = new SerializedObject(tree); serialized.FindProperty("debugLog").boolValue = false; serialized.ApplyModifiedPropertiesWithoutUndo();
        var progress = host.AddComponent<MergeBoxUpgradeManager>(); progress.skillTree = tree; tree.mergeBoxUpgrades = progress;
        progress.skillNodes.Add(rootNode); progress.skillNodes.Add(boxNode);
        progress.SetSaveStore(new MergeBoxUpgradeFileStore(path));
        if (activate) host.SetActive(true);
        progress.Initialize(); return progress;
    }
    protected void Buy(MergeBoxUpgradeManager progress)
    {
        Assert.That(progress.skillTree.UnlockSkill(rootNode), Is.True);
        Assert.That(progress.skillTree.UnlockSkill(boxNode), Is.True);
        progress.SaveProgress();
    }
}

public class MergeBoxUpgradeTests : MergeBoxUpgradeFixture
{
    [Test]
    public void PurchasedLevelsAndPrerequisitesSurviveReloadWithoutAnotherCharge()
    {
        var first = Progress();
        Assert.That(first.skillTree.UnlockSkill(boxNode), Is.False);
        Buy(first); int money = wallet.GetCurrency(CurrencyType.CurrencyMain);
        Assert.That(money, Is.EqualTo(80)); Assert.That(first.GetLevel(ItemSeries.Weapon), Is.EqualTo(2));
        Assert.That(first.GetLevel(ItemSeries.Food), Is.EqualTo(1));
        var restored = Progress();
        Assert.That(restored.skillTree.IsUnlocked(rootNode.dataId), Is.True);
        Assert.That(restored.skillTree.IsUnlocked(boxNode.dataId), Is.True);
        Assert.That(restored.skillTree.GetUnlockedSkillCount(rootNode.skillMap), Is.EqualTo(2));
        Assert.That(restored.skillTree.UnlockSkill(boxNode), Is.False);
        restored.Initialize(); Assert.That(restored.GetLevel(ItemSeries.Weapon), Is.EqualTo(2));
        Assert.That(wallet.GetCurrency(CurrencyType.CurrencyMain), Is.EqualTo(money));
    }
    [Test]
    public void HighestRewardWinsAndOtherSeriesStayIndependentIncludingAfterClearingSkills()
    {
        var higher = Node("Higher box"); higher.requiredSkills.Add(boxNode);
        var reward = Asset<MergeBoxLevelSkillReward>(); reward.series = ItemSeries.Weapon; reward.level = 5; higher.rewards.Add(reward);
        var food = Node("Food box"); var foodReward = Asset<MergeBoxLevelSkillReward>(); foodReward.series = ItemSeries.Food; foodReward.level = 2; food.rewards.Add(foodReward);
        var progress = Progress(); progress.RegisterNode(higher); progress.RegisterNode(food);
        Buy(progress); Assert.That(progress.skillTree.UnlockSkill(higher), Is.True); Assert.That(progress.skillTree.UnlockSkill(food), Is.True);
        progress.SaveProgress(); Assert.That(progress.GetLevel(ItemSeries.Weapon), Is.EqualTo(5)); Assert.That(progress.GetLevel(ItemSeries.Food), Is.EqualTo(2));
        progress.ApplyReward(ItemSeries.Weapon, 2); Assert.That(progress.GetLevel(ItemSeries.Weapon), Is.EqualTo(5));
        Assert.That(progress.CaptureLevels().Count, Is.EqualTo(8));
        progress.skillTree.ClearAllSkills(); progress.SaveProgress();
        Assert.That(progress.GetLevel(ItemSeries.Weapon), Is.EqualTo(1)); Assert.That(progress.GetLevel(ItemSeries.Food), Is.EqualTo(1));
    }
    [Test]
    public void UpgradeGoGameCopiesOnlySnapshotsAndKeepsTheTransferRolesSeparate()
    {
        var progress = Progress(); Buy(progress);
        var host = Host("Session", false); var session = host.AddComponent<GameSession>(); GameSession.Instance = session;
        var item = Asset<ItemData>(); var inventory = host.AddComponent<InventoryManager>(); inventory.items.Add(new InventoryItem(item, 9));
        var upgrade = host.AddComponent<UpgradeManager>(); upgrade.materialInventory = inventory; upgrade.mergeBoxUpgrades = progress;
        var previousLoader = SceneMoveManager.instance; SceneMoveManager.instance = null;
        try { upgrade.GoGame(); } finally { SceneMoveManager.instance = previousLoader; }
        Assert.That(session.TryReadGeneralData(out var data), Is.True);
        Assert.That(data.boxLevels.Single(entry => entry.series == ItemSeries.Weapon).level, Is.EqualTo(2));
        data.boxLevels[0].level = 88; inventory.items[0].amount = 0;
        Assert.That(session.TryReadGeneralData(out var next), Is.True); Assert.That(next.boxLevels[0].level, Is.EqualTo(2));
        Assert.That(next.materialItems[0].amount, Is.EqualTo(9)); Assert.That(next.skillItems, Is.Empty);
        session.SendItemsToBattle(new GameItemToBattleData { items = new List<InventoryItem> { new InventoryItem(item, 2) } });
        Assert.That(session.TryTakeBattleItems(out var batch), Is.True); Assert.That(batch.items[0].amount, Is.EqualTo(2));
        Assert.That(session.TryReadGeneralData(out next), Is.True); Assert.That(next.boxLevels[0].level, Is.EqualTo(2));
        GameSession.Instance = null;
    }
    [Test]
    public void NewlyAuthoredNodeCanBeRegisteredAndRestoredFromItsPreviouslySavedId()
    {
        var extra = Node("Extra"); var reward = Asset<MergeBoxLevelSkillReward>(); reward.series = ItemSeries.Magic; reward.level = 2; extra.rewards.Add(reward);
        var first = Progress(); first.RegisterNode(extra); Assert.That(first.skillTree.UnlockSkill(extra), Is.True); first.SaveProgress();
        var restored = Progress(); Assert.That(restored.skillTree.IsUnlocked(extra.dataId), Is.False);
        restored.RegisterNode(extra); Assert.That(restored.skillTree.IsUnlocked(extra.dataId), Is.True);
        Assert.That(restored.GetLevel(ItemSeries.Magic), Is.EqualTo(2));
    }
    [Test]
    public void AuthoredScenesConnectRewardsAndUseOnlyTheUserAssignedBoxLevels()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/UpgradeScene.unity", OpenSceneMode.Single);
        var roots = scene.GetRootGameObjects();
        var progress = roots.SelectMany(r => r.GetComponentsInChildren<MergeBoxUpgradeManager>(true)).Single();
        var tree = roots.SelectMany(r => r.GetComponentsInChildren<SkillTreeManager>(true)).Single();
        var upgrade = roots.SelectMany(r => r.GetComponentsInChildren<UpgradeManager>(true)).Single();
        Assert.That(progress.skillTree, Is.SameAs(tree)); Assert.That(tree.mergeBoxUpgrades, Is.SameAs(progress)); Assert.That(upgrade.mergeBoxUpgrades, Is.SameAs(progress));
        foreach (var kind in new[] { ItemSeries.Weapon, ItemSeries.Food })
            Assert.That(progress.skillNodes.Any(node => node.rewards.OfType<MergeBoxLevelSkillReward>().Any(r => r.series == kind && r.level == 2)), Is.True);
        foreach (var kind in series)
        {
            var reward = AssetDatabase.LoadAssetAtPath<MergeBoxLevelSkillReward>("Assets/Data/Scriptable/SkillTree/MergeBoxUpgrades/" + kind + " Box Level 2.asset");
            Assert.That(reward != null, Is.True); Assert.That(reward.series, Is.EqualTo(kind));
        }
        scene = EditorSceneManager.OpenScene("Assets/Scenes/GameItemScene.unity", OpenSceneMode.Single); roots = scene.GetRootGameObjects();
        var boxes = roots.SelectMany(r => r.GetComponentsInChildren<MergeBoxSceneController>(true)).Single();
        var bridge = roots.SelectMany(r => r.GetComponentsInChildren<GameItemSessionBridge>(true)).Single();
        Assert.That(bridge.boxes, Is.SameAs(boxes)); CollectionAssert.AreEqual(boxes.boxes.Select(e => e.board).ToArray(), bridge.boards);
        foreach (var entry in boxes.boxes)
        {
            var levels = entry.levelManager.levels; Assert.That(levels.Length, Is.GreaterThan(0));
            foreach (var level in levels)
            {
                Assert.That(level.box != null, Is.True); Assert.That(level.box.transform.IsChildOf(entry.boxRoot), Is.True);
                Assert.That(level.box.spawnPoint.IsChildOf(level.box.transform), Is.True);
                Assert.That(level.box.dropZone.transform.IsChildOf(level.box.transform), Is.True);
            }
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }
}

public class MergeBoxUpgradePlayTests : MergeBoxUpgradeFixture
{
    [UnitySetUp] public IEnumerator Enter() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); yield return new EnterPlayMode(); }
    [UnityTearDown] public IEnumerator Exit() { yield return new ExitPlayMode(); }
    [UnityTest]
    public IEnumerator PurchaseAutomaticallySavesThenLateSessionAppliesLevelsToEightIndependentSeries()
    {
        var unlockHost = Host("Unlocks", false); var unlocks = unlockHost.AddComponent<UnlockManager>();
        var unlockSettings = new SerializedObject(unlocks); unlockSettings.FindProperty("debugLog").boolValue = false; unlockSettings.ApplyModifiedPropertiesWithoutUndo();
        unlockHost.SetActive(true);
        var unlockReward = Asset<UnlockSkillReward>(); var rewardSettings = new SerializedObject(unlockReward);
        rewardSettings.FindProperty("unlockType").enumValueIndex = 0; rewardSettings.FindProperty("unlockId").stringValue = "restored-box-prerequisite";
        rewardSettings.ApplyModifiedPropertiesWithoutUndo(); rootNode.rewards.Add(unlockReward);
        var progress = Progress(true);
        // Root was disabled to configure the wallet; no starting money is created by Awake.
        Assert.That(progress.skillTree.UnlockSkill(rootNode), Is.True); Assert.That(progress.skillTree.UnlockSkill(boxNode), Is.True);
        Assert.That(File.Exists(path), Is.True);
        int balance = wallet.GetCurrency(CurrencyType.CurrencyMain);
        Assert.That(unlocks.IsUnlocked(unlockReward.UnlockType, unlockReward.UnlockId), Is.True);
        unlocks.Lock(unlockReward.UnlockType, unlockReward.UnlockId);
        Object.DestroyImmediate(progress.gameObject); progress = Progress(true);
        Assert.That(progress.skillTree.IsUnlocked(boxNode.dataId), Is.True); Assert.That(progress.GetLevel(ItemSeries.Weapon), Is.EqualTo(2));
        Assert.That(unlocks.IsUnlocked(unlockReward.UnlockType, unlockReward.UnlockId), Is.True, "Existing unlock rewards must be restored together with their purchased prerequisite node.");
        Assert.That(wallet.GetCurrency(CurrencyType.CurrencyMain), Is.EqualTo(balance));
        var controllerHost = Host("Item scene controller", false); var controller = controllerHost.AddComponent<MergeBoxSceneController>();
        controller.boxes = series.Select(kind =>
        {
            var manager = Manager(kind, 1, 2); manager.transform.position = new Vector3(30000 + (int)kind * 20, 30000, 0);
            return new MergeBoxEntry { boxRoot = manager.transform, board = manager.Board, levelManager = manager };
        }).ToArray();
        var bridge = controllerHost.AddComponent<GameItemSessionBridge>(); bridge.boxes = controller; bridge.boards = controller.boxes.Select(e => e.board).ToArray();
        controllerHost.SetActive(true);
        var session = Host("Late session").AddComponent<GameSession>();
        var data = new UpgradeToGeneralData { boxLevels = progress.CaptureLevels() }; session.SaveUpgradeToGeneral(data);
        foreach (var entry in controller.boxes)
            Assert.That(entry.levelManager.ActiveBoxLevel, Is.EqualTo(entry.board.itemSeries == ItemSeries.Weapon ? 2 : 1));
        bridge.enabled = false; bridge.enabled = true;
        Assert.That(controller.GetBoxManager(ItemSeries.Weapon).ActiveBoxLevel, Is.EqualTo(2));
        Assert.That(controller.GetBoxManager(ItemSeries.Food).ActiveBoxLevel, Is.EqualTo(1));
        var itemScene = SceneManager.CreateScene("Box upgrade item transfer test");
        SceneManager.MoveGameObjectToScene(controllerHost, itemScene);
        foreach (var entry in controller.boxes)
        {
            SceneManager.MoveGameObjectToScene(entry.boxRoot.gameObject, itemScene);
            SceneManager.MoveGameObjectToScene(entry.board.gameObject, itemScene);
        }
        yield return SceneManager.UnloadSceneAsync(itemScene);
        Assert.That(session.TryReadGeneralData(out var retained), Is.True);
        Assert.That(retained.boxLevels.Single(entry => entry.series == ItemSeries.Weapon).level, Is.EqualTo(2));
        var again = Manager(ItemSeries.Weapon, 1, 2);
        var nextHost = Host("Reloaded item controller", false); var next = nextHost.AddComponent<MergeBoxSceneController>();
        next.boxes = new[] { new MergeBoxEntry { boxRoot = again.transform, board = again.Board, levelManager = again } };
        var nextBridge = nextHost.AddComponent<GameItemSessionBridge>(); nextBridge.boxes = next; nextBridge.boards = new[] { again.Board };
        nextHost.SetActive(true); Assert.That(again.ActiveBoxLevel, Is.EqualTo(2));
        yield return null;
    }
}
