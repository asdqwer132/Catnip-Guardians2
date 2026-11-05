using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class CodexUITests
{
    private sealed class DiscoveryStore : ICodexDiscoveryStore
    {
        private string json;
        public string Read() => json;
        public void Write(string value) => json = value;
        public void Clear() => json = null;
    }
    private System.IDisposable discoveryScope;
    private DiscoveryStore discoveryStore;
    private readonly List<Object> owned = new List<Object>();
    private GameObject Host(string name, Transform parent = null, bool active = true)
    {
        var host = new GameObject(name, typeof(RectTransform)); host.SetActive(active); owned.Add(host);
        if (parent != null) host.transform.SetParent(parent, false);
        return host;
    }
    private T Data<T>(string name) where T : DefaultData
    {
        var data = ScriptableObject.CreateInstance<T>(); owned.Add(data);
        data.name = data.dataId = name;
        data.data = new[] { new Description { language = language.english, dataName = name } };
        return data;
    }
    private TextMeshProUGUI Text(string name, Transform parent) => Host(name, parent).AddComponent<TextMeshProUGUI>();
    private Sprite Sprite()
    {
        var texture = new Texture2D(2, 2); owned.Add(texture);
        var sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero); owned.Add(sprite);
        return sprite;
    }
    private CodexDetailsUI Details(string name)
    {
        var host = Host(name, active: false); var details = host.AddComponent<CodexDetailsUI>();
        details.dataNameText = Text("Name", host.transform);
        details.effectDescriptions = Text("Effects", host.transform);
        details.portraitDescription = Text("Portrait description", host.transform);
        details.portraitPage = Text("Page", host.transform);
        details.portraitImage = Host("Portrait", host.transform).AddComponent<Image>();
        details.previewImage = Host("Preview", host.transform).AddComponent<Image>();
        details.previousPortrait = Host("Previous", host.transform).AddComponent<Button>();
        details.nextPortrait = Host("Next", host.transform).AddComponent<Button>();
        host.SetActive(true); return details;
    }
    private CodexInventoryUI Inventory(CodexDetailsUI details, bool toggle = false)
    {
        var root = Host("Catalog"); var inventory = root.AddComponent<CodexInventoryUI>();
        inventory.discoveredOnly = false; // These tests isolate catalog presentation; discovery rules are tested below.
        inventory.details = details; inventory.slotParent = Host("Rows", root.transform).transform;
        inventory.selectionGroup = inventory.slotParent.gameObject.AddComponent<ToggleGroup>(); inventory.selectionGroup.allowSwitchOff = true;
        var template = Host("Slot template", active: false);
        var control = toggle ? (Component)template.AddComponent<Toggle>() : template.AddComponent<Button>();
        var slot = template.AddComponent<CodexSlotUI>();
        slot.slotToggle = control as Toggle; slot.slotButton = control as Button;
        slot.dataNameText = Text("Name", template.transform);
        slot.amountText = Text("Quantity", template.transform); slot.amountImage = slot.amountText.gameObject;
        inventory.slotPrefab = slot; inventory.ClearSearchFilter();
        return inventory;
    }
    private static CodexSlotUI Row(CodexInventoryUI inventory, DefaultData entry)
        => inventory.slotParent.GetComponentsInChildren<CodexSlotUI>().Single(slot => slot.Entry == entry);
    private static void Click(Toggle toggle)
        => ExecuteEvents.Execute(toggle.gameObject, new PointerEventData(null) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        discoveryStore = new DiscoveryStore();
        discoveryScope = CodexDiscoveryHistory.OverrideCurrent(new CodexDiscoveryHistory(discoveryStore));
        InventoryManager.instance = null;
    }
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear(); InventoryManager.instance = null;
        discoveryScope.Dispose();
        yield return new ExitPlayMode();
    }

    [Test]
    public void DiscoveredCatalogUpdatesAfterDiscoveryAndResetWithoutChangingCharacterOrMapRules()
    {
        var item = Data<ItemData>("Discovered item");
        var enemy = Data<EnemyDataSet>("Battle enemy");
        var display = Data<EnemyDataSet>("Enemy codex display"); enemy.codexEntry = display;
        var character = Data<CharacterCodexData>("Character");
        var map = Data<MapCodexData>("Map");
        var stage = Data<StageCodexData>("Stage");
        var details = Details("Details"); var inventory = Inventory(details);
        inventory.discoveredOnly = true;
        inventory.SetEntries(new DefaultData[] { item, enemy, display, character, map, stage });
        Assert.That(inventory.VisibleEntries, Is.EquivalentTo(new DefaultData[] { character, map, stage }));

        CodexDiscoveryHistory.Current.RecordItem(item);
        Assert.That(inventory.VisibleEntries, Does.Contain(item), "A discovery must refresh enabled UI without owning stock.");
        CodexDiscoveryHistory.Current.RecordEnemy(display);
        Assert.That(inventory.VisibleEntries, Is.EquivalentTo(new DefaultData[] { item, display, character, map, stage }));
        inventory.Select(item);
        Assert.That(details.SelectedData, Is.SameAs(item));

        discoveryStore.Clear(); CodexDiscoveryHistory.Current.Reload();
        Assert.That(inventory.VisibleEntries, Is.EquivalentTo(new DefaultData[] { character, map, stage }));
        Assert.That(inventory.SelectedEntry, Is.Null);
        Assert.That(details.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void CatalogButtonsDisplayNonOwnedItemsAndEnemiesWithoutConsumingOwnedStock()
    {
        var item = Data<ItemData>("Item"); var enemy = Data<EnemyDataSet>("Enemy");
        var details = Details("Details"); var inventory = Inventory(details);
        inventory.SetEntries(new DefaultData[] { item, enemy });
        Assert.That(inventory.VisibleEntries.Count, Is.EqualTo(2)); Assert.That(InventoryManager.instance, Is.Null);
        Row(inventory, enemy).slotButton.onClick.Invoke();
        Assert.That(details.SelectedData, Is.SameAs(enemy)); Assert.That(details.dataNameText.text, Is.EqualTo("Enemy"));
        var stock = Host("Owned inventory").AddComponent<InventoryManager>(); InventoryManager.instance = stock;
        stock.AddItem(item, 3);
        Row(inventory, item).slotButton.onClick.Invoke();
        Assert.That(details.SelectedData, Is.SameAs(item)); Assert.That(stock.GetItemAmount(item), Is.EqualTo(3));
        Assert.That(Row(inventory, item).amountImage.activeSelf, Is.False);
    }

    [Test]
    public void ItemFiltersKeepOtherDataVisibleAndToggleSelectionPersistsUntilAnotherEntry()
    {
        var weapon = Data<ItemData>("Weapon"); weapon.series = ItemSeries.Weapon;
        var food = Data<ItemData>("Food"); food.series = ItemSeries.Food; food.grade = ItemGrade.Rare;
        var enemy = Data<EnemyDataSet>("Enemy"); var details = Details("Details"); var inventory = Inventory(details, toggle: true);
        inventory.SetEntries(new DefaultData[] { weapon, food, enemy }); Click(Row(inventory, weapon).slotToggle);
        inventory.SetSearchFilter(new InventorySearchFilter { useSeries = true, series = ItemSeries.Food, useGrade = true, grade = ItemGrade.Rare });
        Assert.That(inventory.VisibleEntries, Is.EquivalentTo(new DefaultData[] { food, enemy }));
        Assert.That(inventory.SelectedEntry, Is.Null); Assert.That(details.gameObject.activeSelf, Is.False);
        var row = Row(inventory, food); Click(row.slotToggle); Click(row.slotToggle);
        Assert.That(row.slotToggle.isOn, Is.True); Assert.That(inventory.SelectedEntry, Is.SameAs(food));
        Click(Row(inventory, enemy).slotToggle);
        Assert.That(inventory.SelectedEntry, Is.SameAs(enemy)); Assert.That(row.slotToggle.isOn, Is.False);
    }

    [Test]
    public void ItemAndEnemyDetailsShareLocalizedEffectDescriptions()
    {
        var effect = Data<CodexEffectDescription>("Burn");
        effect.data = new[]
        {
            new Description { language = language.english, dataName = "Burn", description = "Fire damage", detailDescription = "English detail" },
            new Description { language = language.korean, dataName = "화상", description = "화염 피해", detailDescription = "한국어 상세" }
        };
        var item = Data<ItemData>("Item"); item.codexEffects = new[] { effect };
        var enemy = Data<EnemyDataSet>("Enemy"); enemy.codexEffects = new[] { effect };
        var details = Details("Details"); details.Show(item);
        Assert.That(details.effectDescriptions.text, Does.Contain("Burn").And.Contain("Fire damage"));
        ((ISceneLocalizedUI)details).ApplyLanguage(language.korean, null);
        Assert.That(details.effectDescriptions.text, Does.Contain("화상").And.Contain("한국어 상세"));
        details.Show(enemy); ((ISceneLocalizedUI)details).ApplyLanguage(language.korean, null);
        Assert.That(details.effectDescriptions.text, Does.Contain("화염 피해").And.Contain("한국어 상세"));
    }

    [Test]
    public void CharacterNavigationKeepsEachSpriteAndItsLocalizedInformationPaired()
    {
        var character = Data<CharacterCodexData>("Character"); var idle = Sprite(); var attack = Sprite();
        character.portraits.Add(new CharacterCodexPortrait { state = CharacterPortraitState.Idle, stateId = "idle", sprite = idle, data = new[]
        { new Description { language = language.english, detailDescription = "Idle detail" }, new Description { language = language.korean, detailDescription = "기본 상세" } } });
        character.portraits.Add(new CharacterCodexPortrait { state = CharacterPortraitState.Attack, stateId = "attack", sprite = attack, data = new[]
        { new Description { language = language.english, description = "Attack description" }, new Description { language = language.korean, description = "공격 설명" } } });
        var details = Details("Character details"); details.Show(character);
        Assert.That(details.portraitImage.sprite, Is.SameAs(idle)); Assert.That(details.portraitDescription.text, Is.EqualTo("Idle detail"));
        Assert.That(details.portraitPage.text, Is.EqualTo("1 / 2")); Assert.That(details.previousPortrait.interactable, Is.False);
        details.nextPortrait.onClick.Invoke();
        Assert.That(details.portraitImage.sprite, Is.SameAs(attack)); Assert.That(details.portraitDescription.text, Is.EqualTo("Attack description"));
        Assert.That(details.nextPortrait.interactable, Is.False);
        ((ISceneLocalizedUI)details).ApplyLanguage(language.korean, null);
        Assert.That(details.portraitDescription.text, Is.EqualTo("공격 설명"));
        details.previousPortrait.onClick.Invoke();
        Assert.That(details.portraitImage.sprite, Is.SameAs(idle)); Assert.That(details.portraitDescription.text, Is.EqualTo("기본 상세"));
    }

    [Test]
    public void MapCatalogUsesStageItemsAndRoutesStageSelectionToSeparateDetails()
    {
        var map = Data<MapCodexData>("Map"); map.preview = Sprite();
        var first = Data<StageCodexData>("Stage 1"); first.map = map; first.stageNumber = 1;
        var second = Data<StageCodexData>("Stage 2"); second.map = map; second.stageNumber = 2; second.preview = Sprite();
        map.stages = new[] { first, second };
        var stageDetails = Details("Stage details"); var stages = Inventory(stageDetails);
        var mapDetails = Details("Map details"); mapDetails.childInventory = stages;
        mapDetails.Show(map);
        Assert.That(stages.VisibleEntries, Is.EquivalentTo(new DefaultData[] { first, second }));
        Assert.That(mapDetails.previewImage.sprite, Is.SameAs(map.preview)); Assert.That(stageDetails.gameObject.activeSelf, Is.False);
        Row(stages, first).slotButton.onClick.Invoke();
        Assert.That(stageDetails.SelectedData, Is.SameAs(first)); Assert.That(stageDetails.previewImage.sprite, Is.SameAs(map.preview));
        Assert.That(mapDetails.gameObject.activeSelf, Is.True, "Stage selection must not recurse into or close the map detail owner.");
        Row(stages, second).slotButton.onClick.Invoke();
        Assert.That(stageDetails.previewImage.sprite, Is.SameAs(second.preview));
        var maps = Inventory(mapDetails, toggle: true);
        maps.SetEntries(new DefaultData[] { map });
        mapDetails.alternatePanels = new[] { stageDetails.gameObject };
        stageDetails.alternatePanels = new[] { mapDetails.gameObject };
        maps.Select(map);
        Row(stages, first).slotButton.onClick.Invoke();
        Assert.That(mapDetails.gameObject.activeSelf, Is.False);
        Click(Row(maps, map).slotToggle);
        Assert.That(mapDetails.gameObject.activeSelf, Is.True, "Clicking the selected map must reopen its stages.");
        Assert.That(stageDetails.gameObject.activeSelf, Is.False);
        var other = Data<MapCodexData>("Other map"); mapDetails.Show(other);
        Assert.That(stages.VisibleEntries.Count, Is.Zero); Assert.That(stageDetails.SelectedData, Is.Null);
    }
}
