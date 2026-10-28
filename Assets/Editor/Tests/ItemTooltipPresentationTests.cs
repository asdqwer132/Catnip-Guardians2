using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class ItemTooltipPresentationTests
{
    private readonly List<Object> owned = new List<Object>();
    private LanguageManager previousLanguage;
    private GameObject Host(string name, Transform parent = null)
    {
        var host = new GameObject(name, typeof(RectTransform)); owned.Add(host);
        if (parent != null) host.transform.SetParent(parent, false);
        return host;
    }
    private TextMeshProUGUI Text(string name, Transform parent) => Host(name, parent).AddComponent<TextMeshProUGUI>();
    private ItemData Item(string name, bool populated = true)
    {
        var item = ScriptableObject.CreateInstance<ItemData>(); owned.Add(item); item.name = name;
        item.grade = populated ? ItemGrade.Epic : ItemGrade.None;
        item.category = populated ? ItemCategory.Attack : ItemCategory.None;
        item.series = populated ? ItemSeries.Weapon : ItemSeries.None;
        item.weight = populated ? 2.5f : 0; item.cooldown = populated ? .75f : 0;
        item.data = populated ? new[] {
            new Description { language = language.english, dataName = "Sword", description = "A weapon", detailDescription = "Deals damage" },
            new Description { language = language.korean, dataName = "검", description = "무기", detailDescription = "피해를 줍니다" }
        } : new Description[0];
        return item;
    }
    private BaseItemSlotUI Slot(out ItemSlotTooltipProvider provider)
    {
        var host = Host("Slot"); var slot = host.AddComponent<BaseItemSlotUI>();
        slot.dataNameText = Text("Name", host.transform); slot.amountText = Text("Amount", host.transform);
        slot.gradeText = Text("Grade", host.transform); slot.detailDescriptionText = Text("Detail", host.transform);
        slot.categoryText = Text("Category", host.transform); slot.seriesText = Text("Series", host.transform);
        provider = host.AddComponent<ItemSlotTooltipProvider>();
        var serialized = new SerializedObject(provider); serialized.FindProperty("slot").objectReferenceValue = slot;
        serialized.ApplyModifiedPropertiesWithoutUndo(); return slot;
    }
    private ItemTooltipUI Tooltip()
    {
        var host = Host("Tooltip"); var ui = host.AddComponent<ItemTooltipUI>(); ui.tooltipPanel = host; ui.positionAtAnchor = false;
        ui.icon = Host("Icon", host.transform).AddComponent<Image>();
        ui.dataNameText = Text("Name", host.transform); ui.descriptionText = Text("Description", host.transform);
        ui.detailDescriptionText = Text("Detail", host.transform); ui.amountText = Text("Amount", host.transform);
        ui.gradeText = Text("Grade", host.transform); ui.categoryText = Text("Category", host.transform);
        ui.seriesText = Text("Series", host.transform); ui.weightText = Text("Weight", host.transform);
        ui.cooldownText = Text("Cooldown", host.transform); return ui;
    }
    [SetUp]
    public void SetUp()
    {
        previousLanguage = LanguageManager.instance;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        LanguageManager.instance = Host("Language").AddComponent<LanguageManager>();
        LanguageManager.instance.selectedLan = language.korean;
    }
    [TearDown]
    public void TearDown()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear(); LanguageManager.instance = previousLanguage;
    }
    [Test]
    public void DirectItemAssignmentShowsLocalizedDetailsQuantityOneAndItemMetadata()
    {
        var slot = Slot(out var provider); var item = Item("Fallback"); slot.SetItemData(item, 1);
        Assert.That(provider.TryGetTooltipData(out var payload), Is.True);
        Assert.That(payload, Is.TypeOf<ItemTooltipData>());
        var info = (ItemTooltipData)payload;
        Assert.That(info.amount, Is.EqualTo(1)); Assert.That(info.grade, Is.EqualTo(ItemGrade.Epic));
        var ui = Tooltip(); ui.Show(provider);
        Assert.That(slot.currentItem.itemData, Is.SameAs(item)); Assert.That(slot.dataNameText.text, Is.EqualTo("검"));
        Assert.That(slot.detailDescriptionText.text, Is.EqualTo("피해를 줍니다"));
        Assert.That(ui.dataNameText.text, Is.EqualTo("검")); Assert.That(ui.descriptionText.text, Is.EqualTo("무기"));
        Assert.That(ui.detailDescriptionText.text, Is.EqualTo("피해를 줍니다"));
        Assert.That(ui.amountText.text, Is.EqualTo("x1")); Assert.That(ui.amountText.gameObject.activeSelf, Is.True);
        Assert.That(ui.gradeText.text, Is.EqualTo("Epic")); Assert.That(ui.categoryText.text, Is.EqualTo("Attack"));
        Assert.That(ui.seriesText.text, Is.EqualTo("Weapon")); Assert.That(ui.weightText.text, Is.EqualTo("2.5"));
        Assert.That(ui.cooldownText.text, Is.EqualTo("0.75"));
    }
    [Test]
    public void InventoryReferencesStayLiveAndPresentationChangesCannotMutateInventoryStock()
    {
        var slot = Slot(out var provider); var item = Item("Inventory"); var inventory = new InventoryItem(item, 20);
        slot.SetSlot(inventory); Assert.That(slot.currentItem, Is.SameAs(inventory));
        inventory.amount = 7; provider.TryGetTooltipData(out var payload);
        Assert.That(((ItemTooltipData)payload).amount, Is.EqualTo(7)); Assert.That(payload.amountText, Is.EqualTo("x7"));
        slot.SetItemData(item, 3);
        Assert.That(inventory.amount, Is.EqualTo(7)); Assert.That(slot.currentItem, Is.Not.SameAs(inventory));
        Assert.That(slot.currentItem.amount, Is.EqualTo(3));
    }
    [Test]
    public void SwitchingToAnItemWithoutDetailsClearsOldFieldsAndClearingSlotRemovesItsPayload()
    {
        var slot = Slot(out var provider); slot.SetItemData(Item("First"), 12); var ui = Tooltip(); ui.Show(provider);
        slot.SetItemData(Item("Fallback name", false), 0); ui.Show(provider);
        Assert.That(ui.dataNameText.text, Is.EqualTo("Fallback name")); Assert.That(ui.descriptionText.text, Is.Empty);
        Assert.That(ui.detailDescriptionText.text, Is.Empty); Assert.That(ui.detailDescriptionText.gameObject.activeSelf, Is.False);
        Assert.That(ui.gradeText.text, Is.Empty); Assert.That(ui.gradeText.gameObject.activeSelf, Is.False);
        Assert.That(ui.categoryText.gameObject.activeSelf, Is.False); Assert.That(ui.seriesText.gameObject.activeSelf, Is.False);
        Assert.That(ui.icon.enabled, Is.False); Assert.That(ui.amountText.text, Is.EqualTo("x0"));
        slot.ClearSlot(); Assert.That(provider.TryGetTooltipData(out _), Is.False);
        Assert.That(slot.dataNameText.text, Is.Empty); Assert.That(slot.amountText.text, Is.Empty);
        Assert.That(slot.detailDescriptionText.text, Is.Empty); Assert.That(slot.seriesText.text, Is.Empty);
    }
    [Test]
    public void ItemSceneCanReadEnglishItemInfoWithoutALanguageManager()
    {
        LanguageManager.instance = null;
        var slot = Slot(out var provider); slot.SetItemData(Item("Fallback"), 4);
        Assert.That(provider.TryGetTooltipData(out var payload), Is.True);
        Assert.That(payload.dataName, Is.EqualTo("Sword")); Assert.That(payload.detailDescription, Is.EqualTo("Deals damage"));
    }
    [Test]
    public void SharedSkillAndBagTooltipsKeepTheirInformationAndClearItemOnlyMetadata()
    {
        var slot = Slot(out var provider); slot.SetItemData(Item("Item"), 1); var ui = Tooltip(); ui.Show(provider);
        var anchor = (RectTransform)slot.transform;
        ui.Show(new CostTooltipData { dataName = "Skill", description = "Skill description", detailDescription = "Skill detail" }, anchor, this);
        Assert.That(ui.dataNameText.text, Is.EqualTo("Skill")); Assert.That(ui.detailDescriptionText.text, Is.EqualTo("Skill detail"));
        Assert.That(ui.gradeText.gameObject.activeSelf, Is.False); Assert.That(ui.seriesText.text, Is.Empty);
        Assert.That(ui.weightText.text, Is.Empty); Assert.That(ui.cooldownText.text, Is.Empty);
        ui.Show(new BagTooltipData { dataName = "Bag", bagType = "Equipment" }, anchor, this);
        Assert.That(ui.gradeText.text, Is.EqualTo("Equipment")); Assert.That(ui.detailDescriptionText.text, Is.Empty);
        ui.Show(new SkillMapTooltipData { dataName = "Map", totalNodesCount = "12", completedNodesCount = "3", amountText = "3" }, anchor, this);
        Assert.That(ui.gradeText.text, Is.EqualTo("12")); Assert.That(ui.amountText.text, Is.EqualTo("3"));
    }
    [Test]
    public void AuthoredPrefabTextReferencesSurviveTheRenamedSerializedFields()
    {
        var tooltip = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/UI/UpgradeCanvas/Inventory/Tooltip.prefab").GetComponentInChildren<ItemTooltipUI>(true);
        Assert.That(tooltip.dataNameText, Is.Not.Null); Assert.That(tooltip.gradeText, Is.Not.Null);
        Assert.That(tooltip.descriptionText, Is.Not.Null);
        var slot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/UI/UpgradeCanvas/Main/Craft/Craft/CombinationMaterialSlot.prefab").GetComponentInChildren<BaseItemSlotUI>(true);
        Assert.That(slot, Is.Not.Null);
        Assert.That(slot.dataNameText, Is.Not.Null);
    }
}
