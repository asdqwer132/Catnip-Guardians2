using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AssignedInventorySearchTests
{
    private GameObject host;
    private InventorySearchUI search;
    private InventoryUI inventory;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("Authored search");
        host.SetActive(false);
        inventory = host.AddComponent<InventoryUI>();
        search = host.AddComponent<InventorySearchUI>();
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(host);

    private void Assign(string field, object value)
    {
        typeof(InventorySearchUI).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(search, value);
    }

    private GameObject Child(string name, Transform parent)
    {
        var child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child;
    }

    private InventorySearchUI.AssignedDropdownPanel Menu(string name)
    {
        var panel = Child(name + " list", host.transform);
        var button = Child(name + " button", host.transform).AddComponent<Button>();
        var text = Child(name + " caption", button.transform).AddComponent<TextMeshProUGUI>();
        return new InventorySearchUI.AssignedDropdownPanel { openButton = button, optionsPanel = panel, selectedText = text };
    }

    private Toggle Row(InventorySearchUI.AssignedDropdownPanel menu, string label)
    {
        var row = Child(label, menu.optionsPanel.transform).AddComponent<Toggle>();
        row.SetIsOnWithoutNotify(false);
        Child("Label", row.transform).AddComponent<TextMeshProUGUI>().text = label;
        return row;
    }

    [Test]
    public void AuthoredRowsSelectTheirAssignedFiltersWithoutCloningOrRewritingTheList()
    {
        var series = Menu("Series");
        var grade = Menu("Grade");
        Toggle fish = Row(series, "Aquatic materials");
        Toggle allSeries = Row(series, "Everything");
        Toggle food = Row(series, "Ingredients");
        Toggle legendary = Row(grade, "Gold quality");
        Toggle allGrades = Row(grade, "Any quality");
        Assign("useAssignedItems", true);
        Assign("seriesPanel", series);
        Assign("gradePanel", grade);
        Assign("seriesAllToggle", allSeries);
        Assign("gradeAllToggle", allGrades);
        Assign("seriesOptions", new[]
        {
            new InventoryToggleSearchUI.SeriesToggleOption { toggle = fish, value = ItemSeries.Fishing },
            new InventoryToggleSearchUI.SeriesToggleOption { toggle = food, value = ItemSeries.Food }
        });
        Assign("gradeOptions", new[]
        {
            new InventoryToggleSearchUI.GradeToggleOption { toggle = legendary, value = ItemGrade.Legendary }
        });
        host.SetActive(true);
        search.RefreshSearchUI();
        search.RefreshSearchUI();
        int objectCount = host.GetComponentsInChildren<Transform>(true).Length;
        int changes = 0;
        search.OnFilterChanged += () => changes++;

        series.openButton.onClick.Invoke();
        Assert.That(series.optionsPanel.activeSelf, Is.True);
        fish.isOn = true;
        Assert.That(inventory.GetSearchFilter().useSeries, Is.True);
        Assert.That(inventory.GetSearchFilter().series, Is.EqualTo(ItemSeries.Fishing));
        Assert.That(series.selectedText.text, Is.EqualTo("Aquatic materials"));
        Assert.That(series.optionsPanel.activeSelf, Is.False);
        Assert.That(changes, Is.EqualTo(1));

        grade.openButton.onClick.Invoke();
        legendary.isOn = true;
        Assert.That(inventory.GetSearchFilter().grade, Is.EqualTo(ItemGrade.Legendary));
        Assert.That(inventory.GetSearchFilter().useSeries, Is.True, "Grade selection preserves the series filter.");
        search.ResetFilter();
        Assert.That(allSeries.isOn && allGrades.isOn, Is.True);
        Assert.That(series.selectedText.text, Is.EqualTo("Everything"));
        Assert.That(inventory.GetSearchFilter().useSeries || inventory.GetSearchFilter().useGrade, Is.False);

        inventory.seriesMask = new[] { ItemSeries.Fishing };
        search.RefreshSearchUI();
        Assert.That(fish.gameObject.activeSelf, Is.False);
        Assert.That(food.GetComponentInChildren<TMP_Text>(true).text, Is.EqualTo("Ingredients"));
        Assert.That(host.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(objectCount));
    }

    [Test]
    public void AutomaticModeStillRetainsSeriesAndGradeWithoutAssignedRows()
    {
        var dropdown = Child("Automatic", host.transform).AddComponent<TMP_Dropdown>();
        Assign("seriesDropdown", dropdown);
        host.SetActive(true);
        search.RefreshSearchUI();
        search.SetFilterFromExternal(new InventorySearchFilter
        {
            useSeries = true, series = ItemSeries.Food,
            useGrade = true, grade = ItemGrade.Epic
        });
        search.RefreshSearchUI();
        Assert.That(inventory.GetSearchFilter().useSeries, Is.True);
        Assert.That(inventory.GetSearchFilter().useGrade, Is.True);
        Assert.That(dropdown.options[dropdown.value].text, Is.EqualTo("Food"));
    }
}
