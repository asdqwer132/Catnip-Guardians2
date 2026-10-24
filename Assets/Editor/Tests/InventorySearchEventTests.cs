using NUnit.Framework;
using TMPro;
using UnityEditor.Events;
using UnityEngine;

public class InventorySearchEventTests
{
    private GameObject host;
    private InventoryUI inventory;
    private InventorySearchUI search;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("Search events");
        host.SetActive(false);
        inventory = host.AddComponent<InventoryUI>();
        search = host.AddComponent<InventorySearchUI>();
        search.RefreshSearchUI();
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(host);

    [Test]
    public void ExplicitValuesFilterIndependentlyRespectMasksAndCanBeReset()
    {
        search.OnSeriesChanged((int)ItemSeries.Food);
        search.OnGradeChanged((int)ItemGrade.Rare);
        search.OnCategoryChanged((int)ItemCategory.Heal);
        InventorySearchFilter filter = inventory.GetSearchFilter();
        Assert.That(filter.useSeries && filter.series == ItemSeries.Food, Is.True);
        Assert.That(filter.useGrade && filter.grade == ItemGrade.Rare, Is.True);
        Assert.That(filter.useCategory && filter.category == ItemCategory.Heal, Is.True);

        search.OnSeriesChanged(999);
        Assert.That(filter.series, Is.EqualTo(ItemSeries.Food));
        search.OnSeriesChanged((int)ItemSeries.Potions);
        Assert.That(filter.series, Is.EqualTo(ItemSeries.Potions), "Negative enum values are actual filters, not ALL.");
        search.ResetSeriesFilter();
        Assert.That(filter.useSeries, Is.False);
        Assert.That(filter.useGrade && filter.useCategory, Is.True);

        inventory.gradeMask = new[] { ItemGrade.Legendary };
        search.OnGradeChanged((int)ItemGrade.Legendary);
        Assert.That(filter.useGrade, Is.False);
    }

    [Test]
    public void InspectorWiredEventUsesFixedValueWithoutPopulatingOrRewritingAuthoredDropdown()
    {
        var child = new GameObject("Authored dropdown", typeof(RectTransform));
        child.transform.SetParent(host.transform, false);
        var dropdown = child.AddComponent<TMP_Dropdown>();
        dropdown.options.Add(new TMP_Dropdown.OptionData("My first row"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("My second row"));
        // This is the same fixed int argument the user assigns in OnValueChanged's Inspector.
        UnityEventTools.AddIntPersistentListener(dropdown.onValueChanged, search.OnSeriesChanged, (int)ItemSeries.Fishing);
        dropdown.onValueChanged.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
        int objectCount = host.GetComponentsInChildren<Transform>(true).Length;
        search.RefreshSearchUI();
        dropdown.onValueChanged.Invoke(0);
        Assert.That(inventory.GetSearchFilter().series, Is.EqualTo(ItemSeries.Fishing));
        search.ResetFilter();
        search.RefreshSearchUI();
        Assert.That(dropdown.options.Count, Is.EqualTo(2));
        Assert.That(dropdown.options[0].text, Is.EqualTo("My first row"));
        Assert.That(dropdown.value, Is.Zero);
        Assert.That(host.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(objectCount));
        Assert.That(dropdown.onValueChanged.GetPersistentEventCount(), Is.EqualTo(1));
    }
}
