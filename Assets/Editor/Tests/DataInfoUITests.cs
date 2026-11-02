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

public sealed class DataInfoUITests
{
    private List<Object> owned;
    private LanguageManager languageManager;
    private GameObject Host(string name, Transform parent = null, bool active = true)
    {
        var host = new GameObject(name, typeof(RectTransform)); host.SetActive(active); owned.Add(host);
        if (parent != null) host.transform.SetParent(parent, false);
        return host;
    }
    private T Asset<T>() where T : ScriptableObject
    { var asset = ScriptableObject.CreateInstance<T>(); owned.Add(asset); return asset; }
    private Sprite Sprite()
    {
        var texture = new Texture2D(2, 2); owned.Add(texture);
        var sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero); owned.Add(sprite); return sprite;
    }
    private TextMeshProUGUI Text(string name, Transform parent, bool marker = false)
    {
        var host = Host(name, parent); var text = host.AddComponent<TextMeshProUGUI>();
        if (marker) host.AddComponent<LocalizedText>(); return text;
    }
    private T View<T>(bool active = true, bool markers = false) where T : DataInfoUI
    {
        var host = Host(typeof(T).Name, active: false); var ui = host.AddComponent<T>();
        ui.icon = Host("Icon", host.transform).AddComponent<Image>();
        ui.dataNameText = Text("Name", host.transform, markers);
        ui.descriptionText = Text("Description", host.transform, markers);
        ui.detailDescriptionText = Text("Detail", host.transform, markers);
        if (ui is ItemTooltipUI tooltip)
        { tooltip.tooltipPanel = Host("Tooltip panel", host.transform); tooltip.positionAtAnchor = false; }
        if (active) host.SetActive(true); return ui;
    }
    private T Data<T>() where T : DefaultData
    {
        var data = Asset<T>(); data.name = "Asset fallback"; data.icon = Sprite();
        data.data = new[] {
            new Description { language = language.english, dataName = "Chest", description = "Rewards", detailDescription = "Details" },
            new Description { language = language.korean, dataName = "상자", description = "보상" }
        }; return data;
    }
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode(); owned = new List<Object>();
        Host("Language mediator").AddComponent<SceneLanguageMediator>();
        languageManager = Host("Language").AddComponent<LanguageManager>();
    }
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear(); yield return new ExitPlayMode();
    }

    [Test]
    public void DefaultDataBindingLocalizesAllFieldsWithoutIndividualTextMarkers()
    {
        var ui = View<DataInfoUI>(); var data = Data<ItemBoxData>(); ui.SetData(data);
        Assert.That(ui.icon.sprite, Is.SameAs(data.icon)); Assert.That(ui.dataNameText.text, Is.EqualTo("Chest"));
        languageManager.SetLanguage(language.korean);
        Assert.That(ui.dataNameText.text, Is.EqualTo("상자")); Assert.That(ui.descriptionText.text, Is.EqualTo("보상"));
        Assert.That(ui.detailDescriptionText.text, Is.EqualTo("Details"), "Missing Korean text falls back to English.");
        ui.enabled = false; languageManager.SetLanguage(language.english); ui.enabled = true;
        Assert.That(ui.dataNameText.text, Is.EqualTo("Chest"));
    }

    [Test]
    public void InlineDefinedDataAndExistingMarkersShareTheSameFieldBindingsAndClearCleanly()
    {
        var ui = View<DataInfoUI>(markers: true);
        var entry = new DefinedDataEntry<ItemSeries> { type = ItemSeries.Weapon, icon = Sprite(), data = new[] {
            new Description { language = language.english, dataName = "Weapon", description = "English description", detailDescription = "English detail" },
            new Description { language = language.korean, dataName = "무기", description = "설명", detailDescription = "상세 설명" } } };
        ui.SetDefinedData(entry); languageManager.SetLanguage(language.korean);
        Assert.That(ui.dataNameText.text, Is.EqualTo("무기")); Assert.That(ui.descriptionText.text, Is.EqualTo("설명"));
        Assert.That(ui.detailDescriptionText.text, Is.EqualTo("상세 설명"));
        ui.ClearData(); languageManager.SetLanguage(language.english);
        Assert.That(ui.dataNameText.text, Is.Empty); Assert.That(ui.descriptionText.text, Is.Empty);
        Assert.That(ui.detailDescriptionText.text, Is.Empty); Assert.That(ui.icon.enabled, Is.False);
        Assert.That(ui.detailDescriptionText.gameObject.activeSelf, Is.False);
        ui.SetDefinedData(entry); Assert.That(ui.detailDescriptionText.gameObject.activeSelf, Is.True);
    }

    [Test]
    public void InitialInspectorSourceBindsOnEnableAndMissingFieldsAreOptional()
    {
        var ui = View<DataInfoUI>(active: false); ui.dataSource = Data<ItemData>();
        ui.dataNameText.text = "Authored placeholder"; ui.gameObject.SetActive(true);
        Assert.That(ui.dataNameText.text, Is.EqualTo("Chest"));
        ui.descriptionText = null; ui.detailDescriptionText = null; ui.icon = null;
        Assert.DoesNotThrow(() => ui.SetData(Data<ItemBoxData>()));
        Assert.DoesNotThrow(ui.ClearData);
    }

    [Test]
    public void BindingAnAssetWithoutTranslationsUsesItsNameAndClearsPreviousInformation()
    {
        var ui = View<DataInfoUI>(); ui.SetData(Data<ItemData>());
        var empty = Asset<ItemBoxData>(); empty.name = "Unnamed chest";
        ui.SetData(empty);
        Assert.That(ui.dataNameText.text, Is.EqualTo("Unnamed chest")); Assert.That(ui.descriptionText.text, Is.Empty);
        Assert.That(ui.detailDescriptionText.gameObject.activeSelf, Is.False); Assert.That(ui.icon.sprite, Is.Null);
    }

    [Test]
    public void ShopInfoUsesStaticSharedBindingAndLeavesOpeningToThePurchasedSlot()
    {
        var ui = View<ShopBoxInfoUI>(); var box = Data<ItemBoxData>();
        ui.boxOpenAnimationUI = ui.icon.gameObject.AddComponent<ShopBoxOpenAnimationUI>();
        ui.boxOpenAnimationUI.boxImage = ui.icon; ui.boxOpenAnimationUI.debugOverrideClip = false;
        ui.buyButton = Host("Buy", ui.transform).AddComponent<Button>();
        ui.ShowBoxInfo(box);
        var animationFrame = Sprite(); ui.icon.sprite = animationFrame;
        languageManager.SetLanguage(language.korean);
        Assert.That(ui.boxNameText.text, Is.EqualTo("상자")); Assert.That(ui.icon.sprite, Is.SameAs(box.icon));
        Assert.That(ui.buyButton.interactable, Is.True);
        ui.ClearInfo(); Assert.That(ui.dataNameText.text, Is.Empty); Assert.That(ui.icon.enabled, Is.False);
        Assert.That(ui.buyButton.interactable, Is.False);
    }

    [Test]
    public void TooltipPayloadKeepsItsIconOverrideAndManualPayloadsUseTheSameDisplay()
    {
        var ui = View<ItemTooltipUI>(); var data = Data<ItemData>(); var overrideIcon = Sprite();
        var anchor = (RectTransform)Host("Anchor").transform;
        ui.Show(new TooltipData { sourceData = data, icon = overrideIcon, dataName = "Chest" }, anchor, this);
        languageManager.SetLanguage(language.korean);
        Assert.That(ui.dataNameText.text, Is.EqualTo("상자")); Assert.That(ui.icon.sprite, Is.SameAs(overrideIcon));
        ui.Show(new TooltipData { dataName = "Manual", description = "Manual description", detailDescription = "Manual detail" }, anchor, this);
        languageManager.SetLanguage(language.english);
        Assert.That(ui.dataNameText.text, Is.EqualTo("Manual")); Assert.That(ui.descriptionText.text, Is.EqualTo("Manual description"));
        Assert.That(ui.detailDescriptionText.text, Is.EqualTo("Manual detail")); Assert.That(ui.icon.enabled, Is.False);
    }
}

public sealed class DataInfoUIAuthoringTests
{
    [Test]
    public void ExistingShopAndTooltipReferencesSurviveMovingTheirFieldsToTheSharedBase()
    {
        var tooltip = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/UI/UpgradeCanvas/Inventory/Tooltip.prefab").GetComponent<ItemTooltipUI>();
        Assert.That(tooltip.dataNameText, Is.Not.Null); Assert.That(tooltip.icon, Is.Not.Null);
        Assert.That(tooltip.descriptionText, Is.Not.Null);
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/UpgradeScene.unity");
        var shop = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ShopBoxInfoUI>(true)).Single();
        Assert.That(shop.boxIcon, Is.Not.Null); Assert.That(shop.boxNameText, Is.Not.Null);
        Assert.That(shop.boxNameText, Is.SameAs(shop.dataNameText)); Assert.That(shop.boxIcon, Is.SameAs(shop.icon));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }
}
