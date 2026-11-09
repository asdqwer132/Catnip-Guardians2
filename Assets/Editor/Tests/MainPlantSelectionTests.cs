using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class MainPlantSelectionTests
{
    private sealed class MemoryStore : IMergeBoxUpgradeSaveStore
    { public string json; public string Read() => json; public void Write(string value) => json = value; }
    private sealed class CodexStore : ICodexDiscoveryStore
    { public string Read() => null; public void Write(string value) { } }
    private readonly List<Object> owned = new List<Object>();
    private T Asset<T>() where T : ScriptableObject { var value = ScriptableObject.CreateInstance<T>(); owned.Add(value); return value; }
    private GameObject Host(string name, bool active = true)
    { var value = new GameObject(name); value.SetActive(active); owned.Add(value); return value; }
    private PlantManager Plants()
    {
        var manager = Host("Plants").AddComponent<PlantManager>();
        manager.allPlants = new PlantData[8];
        for (int i = 0; i < 8; i++) { manager.allPlants[i] = Asset<PlantData>(); manager.allPlants[i].dataId = "Plant" + i; }
        return manager;
    }
    private ItemData SelectionPotion(int index)
    {
        var effect = Asset<MainPlantIndexEffect>(); effect.plantIndex = index;
        var item = Asset<ItemData>(); item.dataId = "Potion" + index; item.series = ItemSeries.Potions; item.effectDatas = new ItemEffectData[] { effect };
        return item;
    }
    [UnitySetUp] public IEnumerator Setup()
    {
        SessionState.SetBool("PlantSelection.HadWallet", PlayerPrefs.HasKey(CurrencyManager.SaveKey));
        SessionState.SetString("PlantSelection.Wallet", PlayerPrefs.GetString(CurrencyManager.SaveKey, ""));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
        if (Application.isPlaying) yield return new ExitPlayMode();
        if (SessionState.GetBool("PlantSelection.HadWallet", false))
            PlayerPrefs.SetString(CurrencyManager.SaveKey, SessionState.GetString("PlantSelection.Wallet", ""));
        else PlayerPrefs.DeleteKey(CurrencyManager.SaveKey);
        PlayerPrefs.Save();
        SessionState.EraseBool("PlantSelection.HadWallet"); SessionState.EraseString("PlantSelection.Wallet");
    }

    [UnityTest]
    public IEnumerator RoundInitializationUsesPotionThenSkillThenAuthoredDefaultAndDoesNotCarryRemovedPotionsForward()
    {
        var plants = Plants(); plants.SetPlantIndex(7);
        var session = Host("Session").AddComponent<GameSession>();
        var executor = Host("Effect executor").AddComponent<ItemEffectExecutor>();
        var init = Host("Round init").AddComponent<InitManager>(); init.plantManager = plants; init.potionEffectExecutor = executor;
        var data = new UpgradeToGeneralData { mainPlantIndex = 2 };
        data.potionSlots.Add(new PotionEquipmentSnapshot { slotIndex = 0, potion = SelectionPotion(5) });
        session.SaveUpgradeToGeneral(data);
        init.InitAll(); Assert.That(plants.CurrentPlant, Is.SameAs(plants.allPlants[5]));

        // Re-applying a skill after the potion cannot defeat the potion's priority.
        plants.SetSkillPlantIndex(1); plants.SetPlants();
        Assert.That(plants.CurrentPlantIndex, Is.EqualTo(5));
        data.potionSlots.Clear(); session.SaveUpgradeToGeneral(data); init.isInited = false; init.InitAll();
        Assert.That(plants.CurrentPlantIndex, Is.EqualTo(2));
        data.mainPlantIndex = -1; session.SaveUpgradeToGeneral(data); init.isInited = false; init.InitAll();
        Assert.That(plants.CurrentPlantIndex, Is.EqualTo(7));

        data.mainPlantIndex = 3; data.potionSlots.Add(new PotionEquipmentSnapshot { potion = SelectionPotion(99) });
        session.SaveUpgradeToGeneral(data); init.isInited = false; init.InitAll();
        Assert.That(plants.CurrentPlantIndex, Is.EqualTo(3), "Invalid effect indices cannot replace a valid selection.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator SkillRewardSelectionRestoresInPurchaseOrderEvenIfTheNodeListIsReordered()
    {
        Host("Wallet").AddComponent<CurrencyManager>();
        var map = Asset<SkillMapData>(); map.dataId = "map";
        var a = Asset<SkillNodeData>(); a.dataId = "a"; a.skillMap = map; a.costs = System.Array.Empty<Cost>();
        var b = Asset<SkillNodeData>(); b.dataId = "b"; b.skillMap = map; b.costs = System.Array.Empty<Cost>();
        var rewardA = Asset<MainPlantIndexSkillReward>(); rewardA.plantIndex = 5; a.rewards.Add(rewardA);
        var rewardB = Asset<MainPlantIndexSkillReward>(); rewardB.plantIndex = 1; b.rewards.Add(rewardB);
        var tree = Host("Tree").AddComponent<SkillTreeManager>();
        var host = Host("Upgrades", false); var upgrades = host.AddComponent<MergeBoxUpgradeManager>();
        var store = new MemoryStore(); upgrades.SetSaveStore(store); upgrades.skillTree = tree;
        upgrades.skillNodes = new List<SkillNodeData> { a, b }; host.SetActive(true); upgrades.Initialize();
        Assert.That(tree.UnlockSkill(a), Is.True); Assert.That(tree.UnlockSkill(b), Is.True);
        Assert.That(upgrades.MainPlantIndex, Is.EqualTo(1));
        host.SetActive(false);
        var otherTree = Host("Reloaded tree").AddComponent<SkillTreeManager>();
        var otherHost = Host("Reloaded upgrades", false); var other = otherHost.AddComponent<MergeBoxUpgradeManager>();
        other.SetSaveStore(store); other.skillTree = otherTree; other.skillNodes = new List<SkillNodeData> { b, a };
        otherHost.SetActive(true); other.Initialize(); Assert.That(other.MainPlantIndex, Is.EqualTo(1));
        yield return null;
    }

    [UnityTest]
    public IEnumerator EquippedPotionSnapshotsRestoreReservedUnitsWithoutChargingInventoryTwice()
    {
        using (CodexDiscoveryHistory.OverrideCurrent(new CodexDiscoveryHistory(new CodexStore())))
        {
            var stock = Host("Inventory").AddComponent<InventoryManager>(); var potion = SelectionPotion(2);
            stock.AddItem(potion, 2);
            var equipment = Host("Equipment").AddComponent<PotionEquipmentManager>(); equipment.inventory = stock;
            Assert.That(equipment.TryEquip(3, potion), Is.True); Assert.That(stock.GetItemAmount(potion), Is.EqualTo(1));
            var data = new UpgradeToGeneralData { potionSlots = equipment.CaptureSlots() };
            var copy = data.Copy(); data.potionSlots[3].potion = null;
            var returned = Host("Returned equipment").AddComponent<PotionEquipmentManager>(); returned.inventory = stock;
            returned.RestoreSlots(copy.potionSlots);
            Assert.That(returned.GetPotion(3), Is.SameAs(potion)); Assert.That(stock.GetItemAmount(potion), Is.EqualTo(1));
            Assert.That(returned.TryUnequip(3), Is.True); Assert.That(stock.GetItemAmount(potion), Is.EqualTo(2));
        }
        yield return null;
    }
}

public class MainPlantSelectionAuthoringTests
{
    [Test]
    public void AuthoredBattleAndUpgradeScenesConnectTheFlatExecutorsAndPotionEquipment()
    {
        try
        {
            var battle = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/GameBattleScene.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
            InitManager init = null; ItemUseManager input = null;
            foreach (var root in battle.GetRootGameObjects())
            {
                if (init == null) init = root.GetComponentInChildren<InitManager>(true);
                if (input == null) input = root.GetComponentInChildren<ItemUseManager>(true);
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    Assert.That(UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), Is.Zero, transform.name);
            }
            Assert.That(init, Is.Not.Null); Assert.That(init.potionEffectExecutor, Is.Not.Null);
            Assert.That(input, Is.Not.Null); Assert.That(input.positionProvider, Is.Not.Null);
            Assert.That(input.itemController, Is.Not.Null); Assert.That(input.itemController.throwExecutor, Is.Not.Null);
            var upgrade = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/UpgradeScene.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
            UpgradeManager manager = null;
            foreach (var root in upgrade.GetRootGameObjects())
                if (manager == null) manager = root.GetComponentInChildren<UpgradeManager>(true);
            Assert.That(manager, Is.Not.Null); Assert.That(manager.potionEquipment, Is.Not.Null);
            Assert.That(manager.potionEquipment.SlotCount, Is.EqualTo(8));
        }
        finally { UnityEditor.SceneManagement.EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
    }
}
