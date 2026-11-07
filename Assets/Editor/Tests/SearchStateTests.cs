using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class SearchStateTests
{
    private readonly List<Object> owned = new List<Object>();
    private InventoryUI inventory;
    private InventorySearchUI search;
    private GameObject Host()
    { var host = new GameObject("Search test"); host.SetActive(false); owned.Add(host); return host; }
    private InventoryItem Item(Category category, ItemSeries series, Grade grade = Grade.Common)
    {
        var data = ScriptableObject.CreateInstance<ItemData>(); owned.Add(data);
        data.category = category; data.series = series; data.grade = grade;
        return new InventoryItem(data, 1);
    }
    [SetUp] public void SetUp()
    {
        var host = Host(); inventory = host.AddComponent<InventoryUI>(); search = host.AddComponent<InventorySearchUI>();
        search.RefreshSearchUI();
    }
    [TearDown] public void TearDown()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
    }
    [Test] public void SearchAndInventoryShareRuntimeStateAndRefreshKeepsZeroValuedFilters()
    {
        search.OnCategoryChanged((int)Category.Item);
        search.OnGradeChanged((int)Grade.Common);
        search.OnSeriesChanged((int)ItemSeries.Food);
        var filter = inventory.GetSearchFilter();
        Assert.That(search.GetCurrentFilter(), Is.SameAs(filter));
        search.RefreshSearchUI(); search.Init();
        Assert.That(filter.useCategory && filter.category == Category.Item, Is.True);
        Assert.That(filter.useGrade && filter.grade == Grade.Common, Is.True);
        Assert.That(filter.useSeries && filter.series == ItemSeries.Food, Is.True);
        Assert.That(new SerializedObject(inventory).FindProperty("searchFilter"), Is.Null);
        Assert.That(new SerializedObject(search).FindProperty("filter"), Is.Null);
        Assert.That(new SerializedObject(inventory).FindProperty("seriesMask"), Is.Not.Null);
        search.OnCategoryChanged((int)Category.None); search.OnGradeChanged((int)Grade.None); search.OnSeriesChanged((int)ItemSeries.None);
        Assert.That(filter.useCategory || filter.useGrade || filter.useSeries, Is.False);
    }
    [Test] public void BagMasksRemainEffectiveInSearchQueriesIncludingAllAndExternalFilters()
    {
        inventory.categoryMask = new[] { Category.None, Category.Item, Category.Enemy, Category.Character, Category.Map };
        inventory.seriesMask = new[] { ItemSeries.None, ItemSeries.Weapon };
        inventory.gradeMask = new[] { Grade.None };
        var weapon = Item(Category.ItemMaterial, ItemSeries.Weapon);
        var food = Item(Category.ItemMaterial, ItemSeries.Food);
        var effectItem = Item(Category.Item, ItemSeries.Food);
        var filter = search.GetCurrentFilter();
        Assert.That(filter.IsMatch(weapon), Is.False);
        Assert.That(filter.IsMatch(effectItem), Is.False);
        Assert.That(filter.IsMatch(food), Is.True);
        search.OnSeriesChanged((int)ItemSeries.Food);
        search.OnCategoryChanged((int)Category.ItemMaterial);
        search.OnGradeChanged((int)Grade.Common);
        search.RefreshSearchUI();
        Assert.That(filter.useSeries && filter.series == ItemSeries.Food, Is.True);
        Assert.That(filter.useCategory && filter.category == Category.ItemMaterial, Is.True);
        Assert.That(filter.useGrade && filter.grade == Grade.Common, Is.True);
        search.OnSeriesChanged((int)ItemSeries.Weapon); // Masked selection cannot bypass the permanent mask.
        Assert.That(filter.useSeries, Is.False);
        search.ResetFilter();
        Assert.That(filter.IsMatch(weapon), Is.False); Assert.That(filter.IsMatch(food), Is.True);
        search.SetFilterFromExternal(new InventorySearchFilter { useSeries = true, series = ItemSeries.Weapon });
        Assert.That(filter.useSeries, Is.False); Assert.That(filter.IsMatch(weapon), Is.False);
    }
    [Test] public void BagSlotRenderingKeepsMasksWhenTheSearchIsReset()
    {
        var previous = InventoryManager.instance;
        var stock = Host().AddComponent<InventoryManager>(); InventoryManager.instance = stock;
        try
        {
            stock.items = new List<InventoryItem>
            {
                Item(Category.ItemMaterial, ItemSeries.Weapon), Item(Category.ItemMaterial, ItemSeries.Food),
                Item(Category.Item, ItemSeries.Food)
            };
            inventory.categoryMask = new[] { Category.Item };
            inventory.seriesMask = new[] { ItemSeries.Weapon };
            inventory.slotParent = Host().transform;
            inventory.slotPrefab = Host(); inventory.slotPrefab.AddComponent<InventorySlotUI>();
            inventory.Init();
            var rows = inventory.slotParent.GetComponentsInChildren<InventorySlotUI>(true);
            Assert.That(rows.Length, Is.EqualTo(1));
            Assert.That(rows[0].currentItem.itemData, Is.SameAs(stock.items[1].itemData));
            search.OnSeriesChanged((int)ItemSeries.Weapon); search.ResetFilter();
            rows = inventory.slotParent.GetComponentsInChildren<InventorySlotUI>(true);
            Assert.That(rows.Count(r => r.gameObject.activeSelf), Is.EqualTo(1));
            Assert.That(rows.Single(r => r.gameObject.activeSelf).currentItem.itemData, Is.SameAs(stock.items[1].itemData));
        }
        finally { InventoryManager.instance = previous; }
    }

    [Test] public void DropdownRowsMapToGradesWithoutChangingAuthoredUI()
    {
        var host = Host(); host.AddComponent<RectTransform>(); var dropdown = host.AddComponent<TMP_Dropdown>();
        dropdown.options = new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("Everything"), new TMP_Dropdown.OptionData("My common row") };
        dropdown.onValueChanged.AddListener(search.OnGradeDropdownChanged);
        dropdown.onValueChanged.Invoke(1);
        Assert.That(inventory.GetSearchFilter().useGrade, Is.True);
        Assert.That(inventory.GetSearchFilter().grade, Is.EqualTo(Grade.Common));
        search.RefreshSearchUI(); Assert.That(inventory.GetSearchFilter().useGrade, Is.True);
        Assert.That(dropdown.options.Select(o => o.text), Is.EqualTo(new[] { "Everything", "My common row" }));
        dropdown.onValueChanged.Invoke(0); Assert.That(inventory.GetSearchFilter().useGrade, Is.False);
        search.gradeDropdownValues = new[] { Grade.Epic, Grade.None };
        search.OnGradeDropdownChanged(0); Assert.That(inventory.GetSearchFilter().grade, Is.EqualTo(Grade.Epic));
        search.OnGradeDropdownChanged(1); Assert.That(inventory.GetSearchFilter().useGrade, Is.False);
    }
    [Test] public void SyncKeepsEachInventorysOwnMask()
    {
        var host = Host(); var otherInventory = host.AddComponent<InventoryUI>(); var otherSearch = host.AddComponent<InventorySearchUI>();
        otherInventory.seriesMask = new[] { ItemSeries.Food };
        otherSearch.RefreshSearchUI();
        var group = Host().AddComponent<InventorySearchUISyncGroup>();
        var serialized = new SerializedObject(group); var list = serialized.FindProperty("searchUIs"); list.arraySize = 2;
        list.GetArrayElementAtIndex(0).objectReferenceValue = search; list.GetArrayElementAtIndex(1).objectReferenceValue = otherSearch;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        search.OnSeriesChanged((int)ItemSeries.Food); group.SyncFrom(search);
        Assert.That(search.GetCurrentFilter().useSeries, Is.True);
        Assert.That(otherSearch.GetCurrentFilter().useSeries, Is.False);
        Assert.That(otherSearch.GetCurrentFilter().IsMatch(Item(Category.ItemMaterial, ItemSeries.Food)), Is.False);
    }
    [Test] public void AuthoredBagSearchUsesItsInventoryMasksAndGradeDropdownAdapter()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/UpgradeScene.unity");
        try
        {
            var targets = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<InventoryUI>(true));
            var bag = targets.Single(t => t.name == "BagInventory");
            var bagSearch = bag.GetComponentInChildren<InventorySearchUI>(true);
            Assert.That(bagSearch.GetCurrentFilter(), Is.SameAs(bag.GetSearchFilter()));
            Assert.That(bagSearch.GetCurrentFilter().IsMatch(Item(Category.ItemMaterial, ItemSeries.Weapon)), Is.False);
            Assert.That(bagSearch.GetCurrentFilter().IsMatch(Item(Category.ItemMaterial, ItemSeries.Food)), Is.True);
            var potion = targets.Single(t => t.GetType() == typeof(InventoryUI) && t != bag);
            var potionSearch = potion.GetComponentInChildren<InventorySearchUI>(true);
            Assert.That(potionSearch.GetCurrentFilter(), Is.SameAs(potion.GetSearchFilter()));
            Assert.That(potionSearch.GetCurrentFilter().IsMatch(Item(Category.Item, ItemSeries.Potions)), Is.True);
            Assert.That(potionSearch.GetCurrentFilter().IsMatch(Item(Category.ItemMaterial, ItemSeries.Food)), Is.False);
            potionSearch.OnGradeDropdownChanged(1);
            Assert.That(potion.GetSearchFilter().useGrade && potion.GetSearchFilter().grade == Grade.Common, Is.True);
            int count = 0;
            foreach (var dropdown in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TMP_Dropdown>(true)))
                for (int i = 0; i < dropdown.onValueChanged.GetPersistentEventCount(); i++)
                    if (dropdown.onValueChanged.GetPersistentTarget(i) is InventorySearchUI target && dropdown.name == "GradeDropdown")
                    {
                        Assert.That(dropdown.onValueChanged.GetPersistentMethodName(i), Is.EqualTo(nameof(InventorySearchUI.OnGradeDropdownChanged)));
                        Assert.That(target.gradeDropdownValues.Length, Is.EqualTo(dropdown.options.Count)); count++;
                    }
            Assert.That(count, Is.EqualTo(5));
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
    }
}
