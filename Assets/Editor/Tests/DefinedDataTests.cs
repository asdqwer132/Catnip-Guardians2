using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class DefinedDataTests
{
    private List<Object> owned;
    private GameObject Host(string name, Transform parent = null, bool active = true)
    {
        var root = new GameObject(name, typeof(RectTransform)); root.SetActive(active); owned.Add(root);
        if (parent != null) root.transform.SetParent(parent, false);
        return root;
    }
    private T Asset<T>() where T : ScriptableObject
    { var data = ScriptableObject.CreateInstance<T>(); owned.Add(data); return data; }
    private Sprite Icon()
    {
        var texture = new Texture2D(2, 2); owned.Add(texture);
        var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero); owned.Add(sprite); return sprite;
    }
    private DefinedDataEntry<T> Entry<T>(T type, string english, string korean)
        => new DefinedDataEntry<T> { type = type, icon = Icon(), data = new[] {
            new Description { language = language.english, dataName = english },
            new Description { language = language.korean, dataName = korean } } };
    private TextMeshProUGUI Text(string name, Transform parent) => Host(name, parent).AddComponent<TextMeshProUGUI>();
    private LanguageManager manager;
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode(); owned = new List<Object>();
        Host("Scene language").AddComponent<SceneLanguageMediator>();
        manager = Host("Core language").AddComponent<LanguageManager>();
    }
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear(); yield return new ExitPlayMode();
    }
    [Test]
    public void OneCurrencyCollectionDispatchesByTypeAndUnknownTypesNeverBorrowAnotherEntry()
    {
        var data = Asset<CurrencyData>();
        data.entries.Add(Entry(CurrencyType.CurrencyMain, "Gold", "골드"));
        data.entries.Add(Entry(CurrencyType.CurrencySub1, "Ruby", "루비"));
        IDefinedDataProvider<CurrencyType> provider = data;
        Assert.That(provider.GetData((CurrencyType)999), Is.Null);
        var root = Host("Currency", active: false); var group = root.AddComponent<CurrencyUIGroup>(); group.currencyData = data;
        var name = Text("Name", root.transform); var amount = Text("Amount", root.transform);
        var icon = Host("Icon", root.transform).AddComponent<Image>();
        group.currencyUIs = new[] { new CurrencyUIGroup.CurrencyUI { type = CurrencyType.CurrencySub1, nameText = name, amountText = amount, icon = icon } };
        root.SetActive(true); group.UpdateUI(CurrencyType.CurrencySub1, 25);
        Assert.That(name.text, Is.EqualTo("Ruby")); Assert.That(icon.sprite, Is.SameAs(data.entries[1].icon));
        manager.SetLanguage(language.korean);
        Assert.That(name.text, Is.EqualTo("루비")); Assert.That(amount.text, Is.EqualTo("25"));
        group.currencyUIs[0].type = (CurrencyType)999; group.UpdateUI((CurrencyType)999, 3);
        Assert.That(icon.enabled, Is.False); Assert.That(icon.sprite, Is.Null);
        Assert.That(name.text, Is.EqualTo("999")); Assert.That(amount.text, Is.EqualTo("3"));
        DefinedData item = Asset<ItemData>(); Assert.That(item, Is.Not.Null, "DefaultData assets also support the common contract.");
    }
    private ItemTooltipUI Tooltip()
    {
        var root = Host("Tooltip owner", active: false);
        var ui = root.AddComponent<ItemTooltipUI>(); ui.positionAtAnchor = false;
        ui.tooltipPanel = Host("Panel", root.transform);
        ui.seriesText = Text("Series", ui.tooltipPanel.transform); ui.gradeText = Text("Grade", ui.tooltipPanel.transform);
        ui.seriesText.gameObject.AddComponent<LocalizedText>(); ui.gradeText.gameObject.AddComponent<LocalizedText>();
        ui.seriesIcon = Host("Series Icon", ui.tooltipPanel.transform).AddComponent<Image>();
        ui.gradeIcon = Host("Grade Icon", ui.tooltipPanel.transform).AddComponent<Image>();
        ui.seriesData = Asset<ItemSeriesData>(); ui.gradeData = Asset<ItemGradeData>();
        ui.seriesData.entries.Add(Entry(ItemSeries.Weapon, "Weapon", "무기"));
        ui.seriesData.entries.Add(Entry(ItemSeries.Food, "Food", "음식"));
        ui.gradeData.entries.Add(Entry(ItemGrade.Epic, "Epic", "영웅"));
        root.SetActive(true); return ui;
    }
    [Test]
    public void TooltipNamesAndIconsUseSeriesAndGradeDefinitionsAndStayLiveAcrossLanguageChanges()
    {
        var ui = Tooltip(); var anchor = (RectTransform)Host("Slot").transform;
        var item = Asset<ItemData>(); item.series = ItemSeries.Weapon; item.grade = ItemGrade.Epic;
        ui.Show(ItemTooltipData.FromItem(item, 2), anchor, this);
        Assert.That(ui.seriesText.text, Is.EqualTo("Weapon"));
        Assert.That(ui.seriesIcon.sprite, Is.SameAs(ui.seriesData.GetData(ItemSeries.Weapon).Icon));
        Assert.That(ui.gradeIcon.sprite, Is.SameAs(ui.gradeData.GetData(ItemGrade.Epic).Icon));
        manager.SetLanguage(language.korean);
        Assert.That(ui.seriesText.text, Is.EqualTo("무기")); Assert.That(ui.gradeText.text, Is.EqualTo("영웅"));
        item.series = ItemSeries.Food; ui.Show(ItemTooltipData.FromItem(item, 5), anchor, this);
        Assert.That(ui.seriesText.text, Is.EqualTo("음식")); Assert.That(ui.seriesIcon.sprite, Is.SameAs(ui.seriesData.GetData(ItemSeries.Food).Icon));
        manager.SetLanguage(language.english); Assert.That(ui.seriesText.text, Is.EqualTo("Food"));
        ui.seriesText = null; ui.gradeText = null;
        Assert.DoesNotThrow(() => ui.Show(ItemTooltipData.FromItem(item, 1), anchor, this));
        Assert.That(ui.seriesIcon.enabled, Is.True);
    }
    [Test]
    public void ReusedTooltipClearsItemOnlyIconsForMissingDefinitionsAndOtherTooltipKinds()
    {
        var ui = Tooltip(); var anchor = (RectTransform)Host("Slot").transform;
        var item = Asset<ItemData>(); item.series = ItemSeries.Weapon; item.grade = ItemGrade.Epic;
        ui.Show(ItemTooltipData.FromItem(item, 1), anchor, this);
        ui.Show(new BagTooltipData { bagType = "Equipment" }, anchor, this);
        Assert.That(ui.gradeText.text, Is.EqualTo("Equipment"));
        Assert.That(ui.seriesIcon.enabled || ui.gradeIcon.enabled, Is.False);
        item.series = ItemSeries.None; item.grade = ItemGrade.None;
        ui.Show(ItemTooltipData.FromItem(item, 1), anchor, this);
        manager.SetLanguage(language.korean);
        Assert.That(ui.gradeText.text, Is.Empty); Assert.That(ui.seriesText.text, Is.Empty);
        Assert.That(ui.seriesIcon.sprite, Is.Null); Assert.That(ui.gradeIcon.sprite, Is.Null);
        item.series = ItemSeries.Magic; item.grade = ItemGrade.Rare;
        ui.Show(ItemTooltipData.FromItem(item, 1), anchor, this);
        Assert.That(ui.seriesText.text, Is.EqualTo("Magic")); Assert.That(ui.gradeText.text, Is.EqualTo("Rare"));
        Assert.That(ui.seriesIcon.enabled || ui.gradeIcon.enabled, Is.False);
    }
}
