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

public sealed class ItemDefinitionUITests
{
    private List<Object> owned;
    private GameObject Host(string name, Transform parent = null, bool active = true)
    {
        var host = new GameObject(name, typeof(RectTransform)); host.SetActive(active); owned.Add(host);
        if (parent != null) host.transform.SetParent(parent, false); return host;
    }
    private T Asset<T>() where T : ScriptableObject
    { var value = ScriptableObject.CreateInstance<T>(); owned.Add(value); return value; }
    private Sprite Sprite()
    {
        var texture = new Texture2D(2, 2); owned.Add(texture);
        var sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero); owned.Add(sprite); return sprite;
    }
    private T View<T>() where T : ItemDefinitionUI
    {
        var root = Host(typeof(T).Name, active: false); var view = root.AddComponent<T>();
        view.seriesIcon = Host("Class icon", root.transform).AddComponent<Image>();
        view.gradeIcon = Host("Grade frame", root.transform).AddComponent<Image>();
        view.seriesText = Host("Class name", root.transform).AddComponent<TextMeshProUGUI>();
        view.gradeText = Host("Grade name", root.transform).AddComponent<TextMeshProUGUI>();
        if (view is ItemTooltipUI tooltip) { tooltip.tooltipPanel = Host("Panel", root.transform); tooltip.positionAtAnchor = false; }
        root.SetActive(true); return view;
    }
    private DefinedDataManager Definitions()
    {
        var root = Host("Definitions", active: false); var manager = root.AddComponent<DefinedDataManager>();
        manager.seriesData = Asset<ItemSeriesData>(); manager.gradeData = Asset<ItemGradeData>();
        manager.seriesData.entries.Add(new DefinedDataEntry<ItemSeries> { type = ItemSeries.Weapon, icon = Sprite(), data = new[] {
            new Description { language = language.english, dataName = "Weapon" }, new Description { language = language.korean, dataName = "무기" } } });
        manager.gradeData.entries.Add(new DefinedDataEntry<Grade> { type = Grade.Rare, icon = Sprite(), data = new[] {
            new Description { language = language.english, dataName = "Rare" }, new Description { language = language.korean, dataName = "희귀" } } });
        root.SetActive(true); return manager;
    }
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode(); owned = new List<Object>();
        Host("Language mediator").AddComponent<SceneLanguageMediator>();
    }
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear(); yield return new ExitPlayMode();
    }

    [Test]
    public void ItemBoxSlotAndTooltipUseTheSameManagerFramesAndLocalizedNames()
    {
        var defs = Definitions(); var language = Host("Language").AddComponent<LanguageManager>();
        var box = Asset<ItemBoxData>(); box.itemSeries = ItemSeries.Weapon; box.grade = Grade.Rare;
        var item = Asset<ItemData>(); item.series = ItemSeries.Weapon; item.grade = Grade.Rare;
        var info = View<ShopBoxInfoUI>(); var slot = View<BaseItemSlotUI>(); var tooltip = View<ItemTooltipUI>();
        info.ShowBoxInfo(box); var entry = new InventoryItem(item, 7); slot.SetSlot(entry);
        tooltip.Show(ItemTooltipData.FromItem(item, 7), (RectTransform)slot.transform, this);
        language.SetLanguage(global::language.korean);
        foreach (var ui in new ItemDefinitionUI[] { info, slot, tooltip })
        {
            Assert.That(ui.seriesText.text, Is.EqualTo("무기")); Assert.That(ui.gradeText.text, Is.EqualTo("희귀"));
            Assert.That(ui.seriesIcon.sprite, Is.SameAs(defs.GetSeries(ItemSeries.Weapon).Icon));
            Assert.That(ui.gradeIcon.sprite, Is.SameAs(defs.GetGrade(Grade.Rare).Icon));
        }
        Assert.That(slot.currentItem, Is.SameAs(entry)); Assert.That(entry.amount, Is.EqualTo(7));
    }

    [Test]
    public void LateAndRemovedManagersRefreshViewsWithoutLeavingOldFrames()
    {
        var ui = View<ItemDefinitionUI>(); ui.SetClassification(ItemSeries.Weapon, Grade.Rare);
        Assert.That(ui.gradeText.text, Is.EqualTo("Rare")); Assert.That(ui.gradeIcon.enabled, Is.False);
        var defs = Definitions(); Assert.That(ui.gradeIcon.enabled, Is.True);
        Object.DestroyImmediate(defs.gameObject);
        Assert.That(ui.gradeIcon.sprite, Is.Null); Assert.That(ui.seriesIcon.sprite, Is.Null);
        Definitions(); Assert.That(ui.gradeIcon.enabled, Is.True);
        ui.ClearData(); Assert.That(ui.gradeText.text, Is.Empty); Assert.That(ui.gradeIcon.enabled, Is.False);
    }

    [Test]
    public void MapPayloadKeepsItsAlternateLabelAndClearsItemFrames()
    {
        Definitions(); var ui = View<ItemTooltipUI>(); var anchor = (RectTransform)Host("Anchor").transform;
        var item = Asset<ItemData>(); item.series = ItemSeries.Weapon; item.grade = Grade.Rare;
        ui.Show(ItemTooltipData.FromItem(item, 1), anchor, this);
        ui.Show(new SkillMapTooltipData { totalNodesCount = "12" }, anchor, this);
        Assert.That(ui.gradeText.text, Is.EqualTo("12"));
        Assert.That(ui.seriesIcon.enabled || ui.gradeIcon.enabled, Is.False);
    }
}

public sealed class ShopDefinitionAuthoringTests
{
    [Test]
    public void AuthoredShopSlotConnectsItsFramesAndTheOpeningPlayerToItsOwnIcon()
    {
        var slot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/UI/UpgradeCanvas/Main/Shop/ShopSlot.prefab").GetComponent<ShopBoxButton>();
        Assert.That(slot.seriesIcon, Is.Not.Null); Assert.That(slot.gradeIcon, Is.Not.Null); Assert.That(slot.gradeText, Is.Not.Null);
        Assert.That(slot.openAnimation, Is.Not.Null); Assert.That(slot.openAnimation.icon, Is.SameAs(slot.icon));
        Assert.That(slot.openAnimation.targetAnimator.gameObject, Is.SameAs(slot.icon.gameObject));
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/UpgradeScene.unity");
        var info = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ShopBoxInfoUI>(true)).Single();
        Assert.That(info.seriesIcon, Is.Not.Null); Assert.That(info.gradeIcon, Is.Not.Null); Assert.That(info.gradeText, Is.Not.Null);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }
}
