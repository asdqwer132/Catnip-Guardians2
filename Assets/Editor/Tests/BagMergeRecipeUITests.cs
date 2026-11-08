using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class BagMergeRecipeUITests
{
    private const string SlotPath = "Assets/Prefab/UI/UpgradeCanvas/Inventory/MergeRecipeSlot.prefab";
    private const string InventorySlotPath = "Assets/Prefab/UI/UpgradeCanvas/Inventory/InventorySlotAmount.prefab";
    private readonly List<Object> owned = new List<Object>();

    private GameObject Host(string name, Transform parent = null, bool active = true)
    {
        var host = new GameObject(name, typeof(RectTransform));
        owned.Add(host);
        host.SetActive(active);
        if (parent != null) host.transform.SetParent(parent, false);
        return host;
    }

    private T Asset<T>() where T : ScriptableObject
    {
        var asset = ScriptableObject.CreateInstance<T>();
        owned.Add(asset);
        return asset;
    }

    private ItemData Item(string name)
    {
        var item = Asset<ItemData>();
        item.name = name;
        item.data = new[] { new Description { language = language.english, dataName = name } };
        var texture = new Texture2D(2, 2); owned.Add(texture);
        item.icon = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * .5f);
        owned.Add(item.icon);
        return item;
    }

    private ItemRecipeData Recipe(ItemData result, params ItemData[] materials)
    {
        var recipe = Asset<ItemRecipeData>();
        recipe.resultItem = result;
        recipe.materials = materials.Select(item => new RecipeMaterial { itemData = item, amount = 1 }).ToArray();
        return recipe;
    }

    private ItemMergeRecipeTooltipUI Panel(params ItemRecipeData[] recipes)
    {
        var host = Host("Authored tooltip", active: false);
        var ui = host.AddComponent<ItemMergeRecipeTooltipUI>();
        ui.tooltipPanel = Host("Details", host.transform);
        ui.dataNameText = Host("Name", ui.tooltipPanel.transform).AddComponent<TextMeshProUGUI>();
        ui.icon = Host("Icon", ui.tooltipPanel.transform).AddComponent<Image>();
        ui.recipeSlotParent = Host("Recipes", ui.tooltipPanel.transform).transform;
        ui.recipeSlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlotPath).GetComponent<MergeRecipeSlotUI>();
        ui.recipes = recipes;
        ui.positionAtAnchor = false;
        host.SetActive(true);
        return ui;
    }

    [SetUp]
    public void SetUp()
    {
        if (!Application.isPlaying) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = owned.Count - 1; i >= 0; i--)
            if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
        if (!Application.isPlaying) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    [UnityTearDown]
    public IEnumerator LeavePlayMode()
    {
        if (Application.isPlaying) yield return new ExitPlayMode();
    }

    [Test]
    public void SelectedIngredientFiltersRecipesWithoutRemovingDistinctEquivalentRecipes()
    {
        var a = Item("A"); var b = Item("B"); var result = Item("Result");
        var first = Recipe(result, b, a);
        var equivalent = Recipe(result, b, a);
        var resultOnly = Recipe(a, b, b);
        var invalid = Recipe(result, a, null);
        var panel = Panel(null, first, equivalent, resultOnly, invalid, first);
        panel.SetData(a);

        CollectionAssert.AreEqual(new[] { first, equivalent }, panel.RecipeSlots.Select(row => row.Recipe));
        Assert.That(panel.RecipeSlots.All(row => row.gameObject.activeSelf), Is.True);
        Assert.That(panel.RecipeSlots.All(row => row.selectedMaterial.icon.sprite == a.icon), Is.True);
        Assert.That(panel.RecipeSlots.All(row => row.resultSlot.icon.sprite == result.icon), Is.True);

        int pooledRows = panel.RecipeSlots.Count;
        panel.SetData(result);
        Assert.That(panel.RecipeSlots.All(row => !row.gameObject.activeSelf && row.Recipe == null), Is.True);
        panel.SetData(a);
        Assert.That(panel.RecipeSlots.Count, Is.EqualTo(pooledRows));
        panel.ClearData();
        Assert.That(panel.RecipeSlots.All(row => !row.gameObject.activeSelf), Is.True);
    }

    [Test]
    public void AuthoredSlotShowsEveryRequiredUnitAndClearsReusedIcons()
    {
        var a = Item("A"); var b = Item("B"); var c = Item("C"); var result = Item("Result");
        var recipe = Recipe(result, a, a, a, b, c);
        var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SlotPath)); owned.Add(instance);
        var row = instance.GetComponent<MergeRecipeSlotUI>();
        Assert.That(row.Bind(recipe, a), Is.True);
        var extra = row.AdditionalMaterialSlots.Where(slot => slot.gameObject.activeSelf).ToArray();
        Assert.That(extra.Length, Is.EqualTo(4));
        CollectionAssert.AreEqual(new[] { a.icon, a.icon, b.icon, c.icon }, extra.Select(slot => slot.icon.sprite));
        Assert.That(row.selectedMaterial.icon.sprite, Is.SameAs(a.icon));
        Assert.That(row.resultSlot.currentItem.itemData, Is.SameAs(result));
        Assert.That(row.resultSlot.icon.transform.parent.name, Is.EqualTo("Result"));

        Assert.That(row.Bind(Recipe(c, b, a), b), Is.True);
        Assert.That(row.AdditionalMaterialSlots.Count, Is.EqualTo(4), "Reuse authored template copies.");
        Assert.That(row.AdditionalMaterialSlots.Count(slot => slot.gameObject.activeSelf), Is.EqualTo(1));
        Assert.That(row.AdditionalMaterialSlots[0].icon.sprite, Is.SameAs(a.icon));
        Assert.That(row.resultSlot.icon.sprite, Is.SameAs(c.icon));
        Assert.That(row.Bind(recipe, result), Is.False);
        Assert.That(row.Recipe, Is.Null);
        Assert.That(row.resultSlot.currentItem, Is.Null);
    }

    [Test]
    public void UpgradeSceneConnectsAuthoredBagRowsAndCurrentMergeRecipeAssets()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/UpgradeScene.unity", OpenSceneMode.Single);
        var roots = scene.GetRootGameObjects();
        var panel = roots.SelectMany(root => root.GetComponentsInChildren<ItemMergeRecipeTooltipUI>(true)).Single();
        var inventory = roots.SelectMany(root => root.GetComponentsInChildren<InventoryUI>(true))
            .Single(ui => ui.name == "BagInventory");
        Assert.That(inventory.tooltipUI, Is.SameAs(panel));
        Assert.That(inventory.showTooltipOnSelection, Is.True);
        Assert.That(AssetDatabase.GetAssetPath(inventory.slotPrefab), Is.EqualTo(InventorySlotPath));
        Assert.That(panel.positionAtAnchor, Is.False);
        Assert.That(panel.recipeSlotParent, Is.SameAs(panel.recipeScroll.content));
        Assert.That(panel.recipeSlotPrefab, Is.Not.Null);
        Assert.That(panel.RecipeSlots.Count, Is.EqualTo(3));
        Assert.That(panel.RecipeSlots.All(row => row != null && !row.gameObject.activeSelf), Is.True);
        Assert.That(panel.alternatePanels.Single().name, Is.EqualTo("NotSelected"));
        var recipes = AssetDatabase.FindAssets("t:ItemRecipeData", new[] { "Assets/Data/Scriptable/Item/MergeRecipe" })
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).ToArray();
        CollectionAssert.AreEquivalent(recipes, panel.recipes.Select(AssetDatabase.GetAssetPath));
        Assert.That(recipes.Length, Is.GreaterThan(0));
        Assert.That(panel.recipes.All(candidate => candidate != null), Is.True);
        var recipe = panel.recipes.First(candidate => candidate.materials.Length > 0);
        panel.SetData(recipe.materials[0].itemData);
        Assert.That(panel.RecipeSlots.Any(row => row.gameObject.activeSelf && row.Recipe == recipe), Is.True);
    }

    [UnityTest]
    public IEnumerator ClickingAuthoredInventoryTogglesUpdatesDetailsAndRecipesWithoutConsumingStock()
    {
        yield return new EnterPlayMode();
        yield return CheckInventoryClicks();
    }

    private IEnumerator CheckInventoryClicks()
    {
        var a = Item("A"); var b = Item("B"); var result = Item("Result");
        var recipe = Recipe(result, a, a);
        var panel = Panel(recipe);
        var manager = Host("Inventory manager").AddComponent<InventoryManager>();
        manager.items.Add(new InventoryItem(a, 12));
        manager.items.Add(new InventoryItem(b, 7));
        var host = Host("Bag inventory");
        var inventory = host.AddComponent<InventoryUI>();
        inventory.slotParent = host.transform;
        inventory.slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(InventorySlotPath);
        inventory.tooltipUI = panel;
        inventory.showTooltipOnSelection = true;
        inventory.selectionGroup = host.AddComponent<ToggleGroup>();
        inventory.selectionGroup.allowSwitchOff = true;
        inventory.Init();
        var rows = host.GetComponentsInChildren<InventorySlotUI>();
        Assert.That(rows.Length, Is.EqualTo(2));
        var initialPosition = ((RectTransform)panel.tooltipPanel.transform).anchoredPosition;
        rows[0].slotToggle.isOn = true;
        Assert.That(panel.IsVisible, Is.True);
        Assert.That(panel.dataNameText.text, Is.EqualTo("A"));
        Assert.That(panel.RecipeSlots.Single().Recipe, Is.SameAs(recipe));
        rows[0].GetComponent<ItemTooltipTrigger>().OnPointerExit(null);
        Assert.That(panel.IsVisible, Is.True, "Click selection persists after the pointer leaves.");
        rows[1].slotToggle.isOn = true;
        Assert.That(rows[0].slotToggle.isOn, Is.False);
        Assert.That(panel.dataNameText.text, Is.EqualTo("B"));
        Assert.That(panel.RecipeSlots.All(row => !row.gameObject.activeSelf), Is.True);
        Assert.That(panel.IsVisible, Is.True);
        Assert.That(((RectTransform)panel.tooltipPanel.transform).anchoredPosition, Is.EqualTo(initialPosition));
        CollectionAssert.AreEqual(new[] { 12, 7 }, manager.items.Select(item => item.amount));
        rows[1].slotToggle.isOn = false;
        Assert.That(panel.IsVisible, Is.False);
        yield return null;
    }
}
