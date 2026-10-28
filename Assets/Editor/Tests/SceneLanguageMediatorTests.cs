using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class SceneLanguageMediatorTests
{
    private readonly List<Object> owned = new List<Object>();
    private Scene first, second;
    private LocalizedTextTable table;
    private GameObject Host(string name, Scene scene, bool active = false)
    {
        var host = new GameObject(name, typeof(RectTransform)); host.SetActive(false);
        SceneManager.MoveGameObjectToScene(host, scene); owned.Add(host);
        if (active) host.SetActive(true); return host;
    }
    private LocalizedTextTable Table()
    {
        var value = ScriptableObject.CreateInstance<LocalizedTextTable>(); owned.Add(value);
        value.entries = new[] {
            new LocalizedTextEntry { key = "Inventory", translations = new[] {
                new LocalizedTextValue { language = language.english, text = "Inventory" },
                new LocalizedTextValue { language = language.korean, text = "인벤토리" } } },
            new LocalizedTextEntry { key = "EnglishOnly", translations = new[] {
                new LocalizedTextValue { language = language.english, text = "English fallback" } } }
        }; return value;
    }
    private SceneLanguageMediator Mediator(Scene scene, language standalone = language.english)
    {
        var host = Host("Scene mediator", scene); var mediator = host.AddComponent<SceneLanguageMediator>();
        mediator.translations = table; mediator.standaloneLanguage = standalone; host.SetActive(true); return mediator;
    }
    private LocalizedText Marker(Scene scene, string english = "Inventory", bool legacy = false)
    {
        var host = Host("Authored English object name", scene); var marker = host.AddComponent<LocalizedText>();
        if (legacy) { marker.legacyText = host.AddComponent<Text>(); marker.legacyText.text = english; }
        else { marker.tmpText = host.AddComponent<TextMeshProUGUI>(); marker.tmpText.text = english; }
        host.SetActive(true); return marker;
    }
    private LanguageManager Core(language selected = language.english)
    {
        var host = Host("Core language manager", first); var manager = host.AddComponent<LanguageManager>();
        manager.selectedLan = selected; host.SetActive(true); return manager;
    }
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        first = SceneManager.CreateScene("Localization A"); second = SceneManager.CreateScene("Localization B"); table = Table();
    }
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
        if (first.IsValid() && first.isLoaded) yield return SceneManager.UnloadSceneAsync(first);
        if (second.IsValid() && second.isLoaded) yield return SceneManager.UnloadSceneAsync(second);
        yield return new ExitPlayMode();
    }
    [Test]
    public void TagsAndScenesLoadedBeforeCoreBindLaterAndNewTagsUseCurrentLanguage()
    {
        var a = Marker(first); var b = Marker(second); Mediator(first); Mediator(second);
        Assert.That(a.tmpText.text, Is.EqualTo("Inventory")); var manager = Core(); int notifications = 0;
        manager.LanguageChanged += _ => notifications++;
        manager.selectedLan = language.korean; manager.SetLanguage(language.korean);
        Assert.That(notifications, Is.EqualTo(1)); Assert.That(a.tmpText.text, Is.EqualTo("인벤토리"));
        Assert.That(b.tmpText.text, Is.EqualTo("인벤토리")); Assert.That(Marker(second).tmpText.text, Is.EqualTo("인벤토리"));
        Assert.That(a.gameObject.name, Is.EqualTo("Authored English object name"));
    }
    [Test]
    public void StandalonePreviewMissingTranslationsAndLegacyTextKeepTheirEnglishFallbacks()
    {
        Mediator(first, language.korean); var localized = Marker(first); var legacy = Marker(first, "Inventory", true);
        var unknown = Marker(first, "<b>Original English</b>"); var englishOnly = Marker(first, "Original English");
        englishOnly.key = "EnglishOnly"; englishOnly.Refresh();
        Assert.That(localized.tmpText.text, Is.EqualTo("인벤토리")); Assert.That(legacy.legacyText.text, Is.EqualTo("인벤토리"));
        Assert.That(unknown.tmpText.text, Is.EqualTo("<b>Original English</b>"));
        Assert.That(englishOnly.tmpText.text, Is.EqualTo("English fallback"));
        Assert.That(LanguageManager.instance, Is.Null);
    }
    [Test]
    public void DisabledTagsAndReplacedCoreDoNotLoseRegistrationOrTheAuthoredEnglish()
    {
        Mediator(first); var marker = Marker(first); var manager = Core();
        marker.gameObject.SetActive(false); manager.SetLanguage(1);
        Assert.That(marker.tmpText.text, Is.EqualTo("Inventory")); marker.gameObject.SetActive(true);
        Assert.That(marker.tmpText.text, Is.EqualTo("인벤토리")); Object.DestroyImmediate(manager.gameObject);
        Assert.That(marker.tmpText.text, Is.EqualTo("Inventory")); Core(language.korean);
        Assert.That(marker.tmpText.text, Is.EqualTo("인벤토리"));
    }
    [UnityTest]
    public IEnumerator UnloadingOneSceneDoesNotBreakLanguageChangesInAnother()
    {
        Mediator(first); Mediator(second); var a = Marker(first); Marker(second); var manager = Core();
        yield return SceneManager.UnloadSceneAsync(second);
        manager.SetLanguage(1); Assert.That(a.tmpText.text, Is.EqualTo("인벤토리"));
        manager.SetLanguage(0); Assert.That(a.tmpText.text, Is.EqualTo("Inventory"));
    }
    [Test]
    public void ActiveTextMovedBetweenScenesUsesTheDestinationMediatorsTable()
    {
        var source = Mediator(first); var destination = Mediator(second);
        source.translations = Table(); source.translations.entries[0].translations[1].text = "출발 씬";
        var marker = Marker(first); var manager = Core();
        SceneManager.MoveGameObjectToScene(marker.gameObject, second);
        manager.SetLanguage(1);
        Assert.That(marker.tmpText.text, Is.EqualTo("인벤토리"));
        marker.gameObject.SetActive(false); marker.gameObject.SetActive(true);
        manager.SetLanguage(0); Assert.That(marker.tmpText.text, Is.EqualTo("Inventory"));
    }
    [UnityTest]
    public IEnumerator InspectorLanguageChangesUseTheSameNotificationPath()
    {
        Mediator(first); var marker = Marker(first); var manager = Core();
        var serialized = new SerializedObject(manager); serialized.FindProperty("selectedLanguage").enumValueIndex = 1;
        serialized.ApplyModifiedPropertiesWithoutUndo(); yield return null;
        Assert.That(marker.tmpText.text, Is.EqualTo("인벤토리"));
    }
    [Test]
    public void AuthoredSettingsDropdownChangesCoreAndSynchronizesWhenCoreChanges()
    {
        Mediator(first); var marker = Marker(first); var manager = Core();
        var host = Host("Settings", first); var dropdown = host.AddComponent<TMP_Dropdown>();
        dropdown.options = new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("English"), new TMP_Dropdown.OptionData("Korean") };
        var ui = host.AddComponent<SettingUI>(); ui.languageDropdown = dropdown; host.SetActive(true);
        dropdown.value = 1;
        Assert.That(manager.selectedLan, Is.EqualTo(language.korean)); Assert.That(marker.tmpText.text, Is.EqualTo("인벤토리"));
        manager.selectedLan = language.english; Assert.That(dropdown.value, Is.Zero);
    }
    private ItemData Item()
    {
        var item = ScriptableObject.CreateInstance<ItemData>(); owned.Add(item); item.name = "Item asset";
        item.data = new[] {
            new Description { language = language.english, dataName = "Sword", description = "Weapon", detailDescription = "Damage" },
            new Description { language = language.korean, dataName = "검", description = "무기", detailDescription = "피해" }
        }; return item;
    }
    [Test]
    public void MarkedItemSlotFieldsChangeLanguageWithoutResettingInventoryOrTheSlot()
    {
        Mediator(first); var manager = Core(); var name = Marker(first, "Item Name"); var description = Marker(first, "Description");
        var detail = Marker(first, "Detail"); var host = Host("Item slot", first, true); var slot = host.AddComponent<BaseItemSlotUI>();
        slot.dataNameText = (TextMeshProUGUI)name.tmpText; slot.descriptionText = (TextMeshProUGUI)description.tmpText;
        slot.detailDescriptionText = (TextMeshProUGUI)detail.tmpText; var inventory = new InventoryItem(Item(), 7); slot.SetSlot(inventory);
        Assert.That(name.tmpText.text, Is.EqualTo("Sword")); manager.selectedLan = language.korean;
        Assert.That(name.tmpText.text, Is.EqualTo("검")); Assert.That(description.tmpText.text, Is.EqualTo("무기"));
        Assert.That(detail.tmpText.text, Is.EqualTo("피해")); Assert.That(slot.currentItem, Is.SameAs(inventory)); Assert.That(inventory.amount, Is.EqualTo(7));
        slot.ClearSlot(); manager.selectedLan = language.english; Assert.That(name.tmpText.text, Is.Empty);
    }
    [Test]
    public void ReusedEnglishTextAndNumericValuesDoNotResetToTheirOriginalPlaceholder()
    {
        Mediator(first); var marker = Marker(first, "Count"); var manager = Core();
        marker.tmpText.text = "7"; manager.SetLanguage(1);
        Assert.That(marker.tmpText.text, Is.EqualTo("7"));
        marker.tmpText.text = "Inventory"; marker.Refresh();
        Assert.That(marker.tmpText.text, Is.EqualTo("인벤토리"));
        manager.SetLanguage(0); Assert.That(marker.tmpText.text, Is.EqualTo("Inventory"));
    }
    [Test]
    public void VisibleAuthoredTooltipUpdatesItsBoundItemTextWhenLanguageChanges()
    {
        Mediator(first); var manager = Core();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/UI/UpgradeCanvas/Inventory/Tooltip.prefab");
        var host = Object.Instantiate(prefab); owned.Add(host); host.SetActive(false); SceneManager.MoveGameObjectToScene(host, first); host.SetActive(true);
        var ui = host.GetComponent<ItemTooltipUI>(); ui.positionAtAnchor = false;
        var item = Item(); var anchor = (RectTransform)Host("Anchor", first, true).transform;
        ui.Show(ItemTooltipData.FromItem(item, 1), anchor, this);
        Assert.That(ui.tooltipPanel.activeInHierarchy, Is.True);
        Assert.That(ui.dataNameText.text, Is.EqualTo("Sword")); manager.selectedLan = language.korean;
        Assert.That(ui.dataNameText.text, Is.EqualTo("검")); Assert.That(ui.descriptionText.text, Is.EqualTo("무기"));
        Assert.That(ui.amountText.text, Is.EqualTo("x1"));
    }
}

public class SceneLanguageAuthoringTests
{
    [Test]
    public void EveryAuthoredSceneHasOneMediatorAndOnlyCoreProvidesTheGlobalManager()
    {
        foreach (string sceneName in new[] { "CoreScene", "GameGeneralScene", "GameBattleScene", "GameItemScene", "UpgradeScene", "MainScene", "StoryScene" })
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + sceneName + ".unity", OpenSceneMode.Single);
            int mediators = 0, managers = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var mediator in root.GetComponentsInChildren<SceneLanguageMediator>(true))
                { mediators++; Assert.That(mediator.translations, Is.Not.Null); }
                managers += root.GetComponentsInChildren<LanguageManager>(true).Length;
            }
            Assert.That(mediators, Is.EqualTo(1), sceneName); Assert.That(managers, Is.EqualTo(sceneName == "CoreScene" ? 1 : 0), sceneName);
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }
}
