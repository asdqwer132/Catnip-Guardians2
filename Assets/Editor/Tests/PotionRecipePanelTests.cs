using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class PotionRecipePanelTests
{
    private sealed class MemoryStore : IPotionRecipeHistoryStore
    {
        public string json;
        public int writes;
        public string Read() => json;
        public void Write(string value) { json = value; writes++; }
    }
    private readonly List<Object> owned = new List<Object>();
    private InventoryManager inventory;
    private ItemCombinationManager manager;
    private PotionRecipeHistory history;
    private MemoryStore store;
    private ItemData a, b, old, potion;
    private ItemRecipeData recipe;
    private GameObject Host(string name, Transform parent = null, bool active = true)
    {
        var value = new GameObject(name); value.SetActive(active);
        if (parent != null) value.transform.SetParent(parent, false);
        owned.Add(value); return value;
    }
    private T Asset<T>() where T : ScriptableObject
    {
        var asset = ScriptableObject.CreateInstance<T>(); owned.Add(asset); return asset;
    }
    private ItemData Item(string id)
    {
        var value = Asset<ItemData>(); value.name = id; value.dataId = id;
        value.series = ItemSeries.Potions; value.grade = ItemGrade.Rare;
        value.data = new[] { new Description { language = language.english, dataName = id,
            description = "Basic " + id, detailDescription = "Detailed " + id } };
        return value;
    }
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        inventory = Host("Inventory").AddComponent<InventoryManager>(); InventoryManager.instance = inventory;
        manager = Host("Potion manager").AddComponent<ItemCombinationManager>();
        history = manager.gameObject.AddComponent<PotionRecipeHistory>(); store = new MemoryStore(); history.SetSaveStore(store);
        manager.recipeHistory = history; manager.returnFailedItem = false;
        a = Item("Material A"); b = Item("Material B"); old = Item("Old material"); potion = Item("Potion A");
        recipe = Asset<ItemRecipeData>(); recipe.resultItem = potion;
        // Deliberately repeat A: rows and preflight must aggregate it to a requirement of two.
        recipe.materials = new[] { new RecipeMaterial { itemData = a, amount = 1 },
            new RecipeMaterial { itemData = a, amount = 1 }, new RecipeMaterial { itemData = b, amount = 1 } };
        manager.recipes.Add(new ItemRecipeList { recipes = new List<ItemRecipeData> { recipe } });
    }
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear(); InventoryManager.instance = null;
        yield return new ExitPlayMode();
    }
    [Test]
    public void OnlySuccessfulCraftingUnlocksHistoryAndItSurvivesReload()
    {
        inventory.AddItem(a, 4); inventory.AddItem(b, 2);
        Assert.That(manager.TryPrepareRecipe(recipe), Is.True);
        Assert.That(history.Count, Is.Zero, "Preparing or previewing cannot discover a potion.");
        Assert.That(manager.GetPreviewResult(), Is.SameAs(potion)); Assert.That(history.Count, Is.Zero);
        Assert.That(manager.TryCombine(out var crafted), Is.True); Assert.That(crafted, Is.SameAs(potion));
        Assert.That(inventory.GetItemAmount(potion), Is.EqualTo(1)); Assert.That(history.HasCrafted(potion), Is.True);
        Assert.That(store.writes, Is.EqualTo(1));
        Assert.That(manager.TryPrepareRecipe(recipe), Is.True); manager.TryCombine(out _);
        Assert.That(store.writes, Is.EqualTo(1), "Crafting the same potion twice must not duplicate its discovery.");
        var reloaded = Host("Reloaded history").AddComponent<PotionRecipeHistory>(); reloaded.SetSaveStore(store);
        var renamedAsset = Item(potion.dataId); renamedAsset.name = "Renamed potion asset";
        Assert.That(reloaded.HasCrafted(renamedAsset), Is.True);
    }
    [Test]
    public void FailureRewardsAndInventoryPurchasesDoNotDiscoverRecipes()
    {
        inventory.AddItem(potion); Assert.That(history.Count, Is.Zero);
        manager.currentMaterials.Add(new InventoryItem(a, 1)); manager.currentMaterials.Add(new InventoryItem(old, 1));
        manager.returnFailedItem = true; manager.failedItem = old;
        Assert.That(manager.TryCombine(out var failed), Is.True); Assert.That(failed, Is.SameAs(old));
        Assert.That(history.Count, Is.Zero); Assert.That(store.writes, Is.Zero);
    }
    [Test]
    public void RealFileHistoryReloadsStableIdsAndAtomicallyUpdatesExistingSave()
    {
        string directory = Path.Combine(Path.GetTempPath(), "catnip-potion-history-" + Guid.NewGuid().ToString("N"));
        try
        {
            var file = new PotionRecipeFileStore(Path.Combine(directory, "discoveries.json"));
            history.SetSaveStore(file); Assert.That(history.RecordCrafted(potion), Is.True);
            Assert.That(history.RecordCrafted(old), Is.True);
            var reloaded = Host("File reload").AddComponent<PotionRecipeHistory>(); reloaded.SetSaveStore(file);
            Assert.That(reloaded.Count, Is.EqualTo(2)); Assert.That(reloaded.HasCrafted(potion), Is.True);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
    [Test]
    public void ShortageOrLockedSlotsLeaveCurrentMaterialsAndInventoryUntouched()
    {
        manager.currentMaterials.Add(new InventoryItem(old, 1));
        inventory.AddItem(a, 1); inventory.AddItem(b, 1);
        Assert.That(manager.CanPrepareRecipe(recipe), Is.False); Assert.That(manager.TryPrepareRecipe(recipe), Is.False);
        Assert.That(manager.currentMaterials.Single().itemData, Is.SameAs(old)); Assert.That(inventory.GetItemAmount(a), Is.EqualTo(1));
        inventory.AddItem(a); manager.SetMaterialSlotLocked(0, true); manager.SetMaterialSlotLocked(1, true);
        Assert.That(manager.TryPrepareRecipe(recipe), Is.False);
        Assert.That(inventory.GetItemAmount(a), Is.EqualTo(2)); Assert.That(manager.currentMaterials.Single().itemData, Is.SameAs(old));
    }
    [Test]
    public void PreparationReusesHeldItemsRefundsOldMaterialsAndRespectsLockedSlots()
    {
        inventory.AddItem(a, 1); inventory.AddItem(b, 1);
        manager.currentMaterials.Add(new InventoryItem(a, 1)); manager.currentMaterials.Add(new InventoryItem(old, 1));
        manager.SetMaterialSlotLocked(1, true); manager.SelectMaterialSlot(2);
        for (int repeat = 0; repeat < 3; repeat++) Assert.That(manager.TryPrepareRecipe(recipe), Is.True);
        Assert.That(manager.GetMaterialAt(1), Is.Null);
        Assert.That(manager.currentMaterials.Where(i => i != null).Count(i => i.itemData == a), Is.EqualTo(2));
        Assert.That(manager.currentMaterials.Where(i => i != null).Count(i => i.itemData == b), Is.EqualTo(1));
        Assert.That(inventory.GetItemAmount(old), Is.EqualTo(1));
        Assert.That(inventory.GetItemAmount(a) + inventory.GetItemAmount(b) + inventory.GetItemAmount(potion), Is.Zero);
        Assert.That(manager.SelectedMaterialSlot, Is.EqualTo(-1)); Assert.That(history.Count, Is.Zero);
    }
    [Test]
    public void FailedReservationRefundsWithdrawalsAndKeepsOldCraftingSlots()
    {
        inventory.AddItem(a, 2); inventory.AddItem(b);
        manager.currentMaterials.Add(new InventoryItem(old, 1));
        bool changed = false;
        Action interfere = () => { if (!changed && inventory.GetItemAmount(a) == 0) { changed = true; inventory.RemoveItem(b); } };
        inventory.onInventoryChanged += interfere;
        try { Assert.That(manager.TryPrepareRecipe(recipe), Is.False); }
        finally { inventory.onInventoryChanged -= interfere; }
        Assert.That(inventory.GetItemAmount(a), Is.EqualTo(2));
        Assert.That(manager.currentMaterials.Single().itemData, Is.SameAs(old)); Assert.That(history.Count, Is.Zero);
    }
    [Test]
    public void DynamicRecipeAndMaterialRowsShowDetailsAndGoToCraftingRechecksStock()
    {
        var root = Host("Recipe browser", active: false); var panel = root.AddComponent<PotionRecipePanelUI>();
        panel.manager = manager; panel.inventory = inventory; panel.history = history;
        panel.recipeContent = Host("Recipes", root.transform).transform;
        panel.materialsContent = Host("Required materials", root.transform).transform;
        panel.recipeSlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/UI/UpgradeCanvas/Main/Potion/PotionRecipeSlot.prefab").GetComponent<PotionRecipeSlotUI>();
        panel.materialSlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/UI/UpgradeCanvas/Main/Potion/NeedMatreial.prefab").GetComponent<PotionRecipeMaterialUI>();
        panel.details = Host("Details", root.transform).AddComponent<ItemDefinitionUI>();
        panel.details.dataNameText = Host("Name", panel.details.transform).AddComponent<TextMeshProUGUI>();
        panel.details.detailDescriptionText = Host("Detail", panel.details.transform).AddComponent<TextMeshProUGUI>();
        panel.goToCraftingButton = Host("Go to crafting", root.transform).AddComponent<Button>();
        panel.craftingTab = Host("Craft tab", root.transform).AddComponent<Toggle>(); panel.craftingTab.isOn = false;
        var craftHost = Host("Craft panel", root.transform, false); panel.craftingPanel = craftHost.AddComponent<ItemCombinationPanelUI>(); panel.craftingPanel.manager = manager;
        panel.craftingTab.onValueChanged.AddListener(craftHost.SetActive);
        root.SetActive(true);
        Assert.That(panel.recipeContent.childCount, Is.Zero); Assert.That(panel.goToCraftingButton.interactable, Is.False);
        inventory.AddItem(a, 2); inventory.AddItem(b); history.RecordCrafted(potion);
        Assert.That(panel.recipeContent.childCount, Is.EqualTo(1));
        var slot = panel.recipeContent.GetComponentInChildren<PotionRecipeSlotUI>(); slot.selectButton.onClick.Invoke();
        Assert.That(panel.SelectedRecipe, Is.SameAs(recipe));
        Assert.That(panel.details.dataNameText.text, Is.EqualTo("Potion A"));
        Assert.That(panel.details.detailDescriptionText.text, Is.EqualTo("Detailed Potion A"));
        var rows = panel.materialsContent.GetComponentsInChildren<PotionRecipeMaterialUI>();
        Assert.That(rows.Length, Is.EqualTo(2));
        Assert.That(rows.Single(row => row.Item == a).requiredCount.text, Is.EqualTo("2"));
        Assert.That(rows.Single(row => row.Item == a).ownedCount.text, Is.EqualTo("2"));
        Assert.That(panel.goToCraftingButton.interactable, Is.True);
        inventory.RemoveItem(a, 2);
        Assert.That(panel.goToCraftingButton.interactable, Is.False);
        panel.goToCraftingButton.onClick.Invoke(); Assert.That(panel.craftingTab.isOn, Is.False);
        inventory.AddItem(a, 2); panel.goToCraftingButton.onClick.Invoke();
        Assert.That(panel.craftingTab.isOn, Is.True); Assert.That(craftHost.activeSelf, Is.True);
        Assert.That(manager.GetPreviewResult(), Is.SameAs(potion)); Assert.That(manager.GetCurrentMaterialCount(), Is.EqualTo(3));
        Assert.That(inventory.GetItemAmount(potion), Is.Zero, "Preparing must not automatically craft or use the potion.");
    }
}

public class PotionRecipeAuthoringTests
{
    [Test]
    public void UpgradeSceneConnectsAuthoredRecipePrefabsAndHasNoPlaceholderRows()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/UpgradeScene.unity", OpenSceneMode.Single);
        try
        {
            var roots = scene.GetRootGameObjects();
            var panel = roots.SelectMany(r => r.GetComponentsInChildren<PotionRecipePanelUI>(true)).Single();
            Assert.That(panel.manager, Is.Not.Null); Assert.That(panel.history, Is.SameAs(panel.manager.recipeHistory));
            Assert.That(panel.inventory, Is.Not.Null); Assert.That(panel.details, Is.Not.Null);
            Assert.That(panel.details.detailDescriptionText, Is.Not.Null);
            Assert.That(panel.recipeContent.childCount, Is.Zero); Assert.That(panel.materialsContent.childCount, Is.Zero);
            Assert.That(panel.recipeSlotPrefab.selectButton, Is.Not.Null);
            Assert.That(panel.recipeSlotPrefab.seriesIcon, Is.Not.Null); Assert.That(panel.recipeSlotPrefab.gradeText, Is.Not.Null);
            Assert.That(panel.materialSlotPrefab.ownedCount, Is.Not.Null); Assert.That(panel.materialSlotPrefab.requiredCount, Is.Not.Null);
            Assert.That(panel.goToCraftingButton, Is.Not.Null); Assert.That(panel.craftingPanel, Is.Not.Null);
            Assert.That(panel.craftingTab, Is.Not.Null); Assert.That(panel.craftingTab.group, Is.Not.Null);
            Assert.That(panel.goToCraftingButton.interactable, Is.False);
            var search = roots.SelectMany(r => r.GetComponentsInChildren<InventorySearchUI>(true)).Single(s => s.GetTargetUI() == panel);
            Assert.That(search, Is.Not.Null);
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
    }
}
