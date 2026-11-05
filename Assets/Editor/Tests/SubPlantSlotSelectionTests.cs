using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class SubPlantSlotSelectionTests
{
    private const string MaterialPrefab = "Assets/Prefab/UI/UpgradeCanvas/Main/Potion/PotionCraftMaterialSlot.prefab";
    private const string InventoryPrefab = "Assets/Prefab/UI/UpgradeCanvas/Main/Potion/PotionCraftInventorySlot.prefab";
    private List<Object> owned;
    private InventoryManager inventory;
    private ItemCombinationManager manager;
    private ItemCombinationPanelUI panel;

    private GameObject Host(string name, Transform parent = null, bool active = true)
    {
        var host = new GameObject(name, typeof(RectTransform)); host.SetActive(active); owned.Add(host);
        if (parent != null) host.transform.SetParent(parent, false);
        return host;
    }
    private T Asset<T>() where T : ScriptableObject
    { var asset = ScriptableObject.CreateInstance<T>(); owned.Add(asset); return asset; }
    private ItemData Item(string id, int count = 4)
    {
        var item = Asset<ItemData>(); item.name = item.dataId = id;
        item.icon = AssetDatabase.LoadAssetAtPath<GameObject>(InventoryPrefab).GetComponent<SubPlantInventorySlotUI>().icon.sprite;
        item.data = new[] { new Description { language = language.english, dataName = id } };
        inventory.AddItem(item, count); return item;
    }
    private SubPlantInventorySlotUI InventorySlot(ItemData item)
    {
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(InventoryPrefab), panel.transform);
        var slot = root.GetComponent<SubPlantInventorySlotUI>();
        slot.SetSlot(new InventoryItem(item, inventory.GetItemAmount(item))); return slot;
    }
    private ItemTooltipUI Tooltip()
    {
        var host = Host("Tooltip", active: false); var ui = host.AddComponent<ItemTooltipUI>();
        ui.positionAtAnchor = false; ui.tooltipPanel = Host("Panel", host.transform);
        ui.dataNameText = Host("Name", ui.tooltipPanel.transform).AddComponent<TextMeshProUGUI>();
        host.SetActive(true); return ui;
    }
    private InventoryUI LiveInventory(ItemTooltipUI tooltip)
    {
        var host = Host("Inventory", panel.transform); var ui = host.AddComponent<InventoryUI>();
        ui.slotParent = Host("Rows", host.transform).transform;
        ui.slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(InventoryPrefab);
        ui.tooltipUI = tooltip; ui.showTooltipOnSelection = true;
        ui.ClearSearchFilter(); ui.Init(); return ui;
    }
    private static void Click(Toggle toggle)
        => ExecuteEvents.Execute(toggle.gameObject, new PointerEventData(null) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode(); owned = new List<Object>();
        inventory = Host("Inventory manager").AddComponent<InventoryManager>();
        manager = Host("Combination manager").AddComponent<ItemCombinationManager>();
        var host = Host("Panel", active: false); panel = host.AddComponent<ItemCombinationPanelUI>();
        panel.manager = manager; panel.materialSlots = new SubPlantMaterialSlotUI[4];
        for (int i = 0; i < 4; i++)
        {
            var slot = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MaterialPrefab), host.transform);
            panel.materialSlots[i] = slot.GetComponent<SubPlantMaterialSlotUI>();
        }
        host.SetActive(true);
    }
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear(); yield return new ExitPlayMode();
    }

    [Test]
    public void ReadyLockedAndFilledSlotsHaveDistinctPresentationAndInteraction()
    {
        var slot = panel.materialSlots[0];
        Assert.That(slot.ready.activeSelf, Is.True); Assert.That(slot.slotToggle.interactable, Is.True);
        Click(slot.slotToggle); Assert.That(manager.SelectedMaterialSlot, Is.Zero);
        slot.LockSlot();
        Assert.That(slot.ready.activeSelf, Is.False); Assert.That(slot.slotToggle.interactable, Is.False);
        Assert.That(manager.SelectedMaterialSlot, Is.EqualTo(-1));
        slot.ClearSlot(); Assert.That(slot.ready.activeSelf, Is.True);
        Assert.That(manager.TryAddMaterial(Item("A")), Is.True);
        Assert.That(slot.ready.activeSelf, Is.False); Assert.That(slot.icon.enabled, Is.True);
    }

    [Test]
    public void InventoryToggleFillsTheSelectedDestinationAndMaterialTogglesAreExclusive()
    {
        var item = Item("A");
        Click(panel.materialSlots[1].slotToggle); Click(panel.materialSlots[3].slotToggle);
        Assert.That(panel.materialSlots[1].slotToggle.isOn, Is.False);
        var slot = InventorySlot(item); Click(slot.slotToggle);
        Assert.That(manager.GetMaterialAt(3).itemData, Is.SameAs(item));
        Assert.That(manager.GetMaterialAt(0), Is.Null);
        Assert.That(inventory.GetItemAmount(item), Is.EqualTo(3));
        Assert.That(panel.materialSlots[3].slotToggle.isOn, Is.True);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void GroupedMaterialSlotsSwitchOnTheFirstClickAndRespectAllowSwitchOff(bool allowSwitchOff)
    {
        var group = Host("Material toggle group", panel.transform).AddComponent<ToggleGroup>();
        group.allowSwitchOff = allowSwitchOff;
        foreach (var slot in panel.materialSlots) slot.slotToggle.group = group;
        Click(panel.materialSlots[0].slotToggle);
        Click(panel.materialSlots[2].slotToggle);
        Assert.That(manager.SelectedMaterialSlot, Is.EqualTo(2));
        Assert.That(panel.materialSlots[0].slotToggle.isOn, Is.False);
        Assert.That(panel.materialSlots[2].slotToggle.isOn, Is.True);
        Assert.That(group.ActiveToggles().Count(), Is.EqualTo(1));
        Click(panel.materialSlots[2].slotToggle);
        Assert.That(manager.SelectedMaterialSlot, Is.EqualTo(allowSwitchOff ? -1 : 2));
        Assert.That(group.allowSwitchOff, Is.EqualTo(allowSwitchOff));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ReturnButtonClearsInventorySelectionEvenWhenItsGroupRequiresOneSelection(bool allowSwitchOff)
    {
        var item = Item("A"); var tooltip = Tooltip(); var ui = LiveInventory(tooltip);
        var group = ui.gameObject.AddComponent<ToggleGroup>(); group.allowSwitchOff = allowSwitchOff;
        var row = ui.slotParent.GetComponentInChildren<SubPlantInventorySlotUI>(); row.slotToggle.group = group;
        Click(row.slotToggle);
        Assert.That(row.slotToggle.isOn, Is.True);
        var button = Host("Return materials", panel.transform).AddComponent<Button>();
        button.onClick.AddListener(panel.ReturnMaterials);
        button.onClick.Invoke();
        Assert.That(row.slotToggle.isOn, Is.False);
        Assert.That(group.allowSwitchOff, Is.EqualTo(allowSwitchOff));
        Assert.That(manager.GetCurrentMaterialCount(), Is.Zero);
        Assert.That(inventory.GetItemAmount(item), Is.EqualTo(4));
        Assert.That(tooltip.tooltipPanel.activeSelf, Is.True);
        Assert.That(tooltip.dataNameText.text, Is.EqualTo("A"));
        Click(row.slotToggle);
        Assert.That(manager.GetCurrentMaterialCount(), Is.EqualTo(1));
        Assert.That(inventory.GetItemAmount(item), Is.EqualTo(3));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void BothSingleMaterialReturnMethodsClearTheSelectedInventoryRow(bool returnByIndex)
    {
        var item = Item("A"); var ui = LiveInventory(Tooltip());
        var row = ui.slotParent.GetComponentInChildren<SubPlantInventorySlotUI>();
        Click(row.slotToggle);
        if (returnByIndex) manager.ReturnMaterialAt(0);
        else manager.ReturnMaterial(item);
        Assert.That(row.slotToggle.isOn, Is.False);
        Assert.That(inventory.GetItemAmount(item), Is.EqualTo(4));
        Assert.That(manager.GetCurrentMaterialCount(), Is.Zero);
    }

    [Test]
    public void ReturnSubscriptionsSurviveReopeningAndDoNotClearAnotherManagersInventory()
    {
        Item("A"); var ui = LiveInventory(Tooltip());
        var otherManager = Host("Other recipe manager").AddComponent<ItemRecipeManager>();
        var other = Host("Other inventory").AddComponent<InventoryUI>();
        other.materialSource = otherManager;
        other.slotParent = Host("Other rows", other.transform).transform;
        other.slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(InventoryPrefab);
        other.ClearSearchFilter(); other.Init();
        var otherRow = other.slotParent.GetComponentInChildren<SubPlantInventorySlotUI>();
        otherRow.slotToggle.SetIsOnWithoutNotify(true);
        ui.gameObject.SetActive(false); ui.gameObject.SetActive(true);
        var row = ui.slotParent.GetComponentInChildren<SubPlantInventorySlotUI>();
        Click(row.slotToggle);
        panel.ReturnMaterials();
        Assert.That(row.slotToggle.isOn, Is.False);
        Assert.That(otherRow.slotToggle.isOn, Is.True);
        otherManager.ReturnMaterials();
        Assert.That(otherRow.slotToggle.isOn, Is.False);
    }

    [Test]
    public void NoSelectedSlotFillsReadySlotsInOrderAndReturnsDoNotShiftOtherItems()
    {
        var a = Item("A"); var b = Item("B"); var c = Item("C");
        panel.materialSlots[1].LockSlot();
        manager.AddMaterial(a); manager.AddMaterial(b);
        Assert.That(manager.GetMaterialAt(0).itemData, Is.SameAs(a));
        Assert.That(manager.GetMaterialAt(2).itemData, Is.SameAs(b));
        manager.ReturnMaterialAt(0);
        Assert.That(manager.GetMaterialAt(2).itemData, Is.SameAs(b));
        manager.AddMaterial(c);
        Assert.That(manager.GetMaterialAt(0).itemData, Is.SameAs(c));
        Assert.That(manager.GetMaterialAt(1), Is.Null);
        Assert.That(inventory.GetItemAmount(a), Is.EqualTo(4));
    }

    [Test]
    public void ReplacingAnOccupiedSelectionRefundsItsItemAndFailedAddsLeaveItIntact()
    {
        var a = Item("A"); var b = Item("B", 1);
        manager.SelectMaterialSlot(2); manager.AddMaterial(a);
        Assert.That(manager.TryAddMaterial(b), Is.True);
        Assert.That(inventory.GetItemAmount(a), Is.EqualTo(4));
        Assert.That(inventory.GetItemAmount(b), Is.Zero);
        Assert.That(manager.GetMaterialAt(2).itemData, Is.SameAs(b));
        var absent = Asset<ItemData>();
        Assert.That(manager.TryAddMaterial(absent), Is.False);
        Assert.That(manager.GetMaterialAt(2).itemData, Is.SameAs(b));
        manager.ReturnMaterialAt(2);
        Assert.That(inventory.GetItemAmount(b), Is.EqualTo(1));
    }

    [Test]
    public void FullOrLockedSlotsCannotConsumeInventoryAndDeselectRestoresOrderedFilling()
    {
        var item = Item("A", 8);
        Click(panel.materialSlots[2].slotToggle); Click(panel.materialSlots[2].slotToggle);
        Assert.That(manager.SelectedMaterialSlot, Is.EqualTo(-1));
        for (int i = 0; i < 4; i++) manager.AddMaterial(item);
        Assert.That(manager.TryAddMaterial(item), Is.False);
        Assert.That(inventory.GetItemAmount(item), Is.EqualTo(4));
        manager.ReturnMaterialAt(1); panel.materialSlots[1].LockSlot();
        Assert.That(manager.TryAddMaterial(item), Is.False);
        Assert.That(inventory.GetItemAmount(item), Is.EqualTo(5));
    }

    [Test]
    public void RecipeAutofillAndCombinationHandleEmptyPositionsAndLockedSlots()
    {
        var a = Item("A"); var b = Item("B"); var result = Item("Result", 0);
        var recipe = Asset<ItemRecipeData>();
        recipe.materials = new[] { new RecipeMaterial { itemData = a, amount = 1 }, new RecipeMaterial { itemData = b, amount = 1 } };
        recipe.resultItem = result;
        manager.recipes.Add(new ItemRecipeList { recipes = new List<ItemRecipeData> { recipe } });
        panel.materialSlots[1].LockSlot();
        Assert.That(manager.TryFillRecipe(recipe), Is.True);
        Assert.That(manager.GetMaterialAt(1), Is.Null);
        Assert.That(manager.GetMaterialAt(2).itemData, Is.SameAs(b));
        Assert.That(manager.TryCombine(out var crafted), Is.True);
        Assert.That(crafted, Is.SameAs(result)); Assert.That(inventory.GetItemAmount(result), Is.EqualTo(1));
        Assert.That(manager.GetCurrentMaterialCount(), Is.Zero);
    }

    [Test]
    public void ClickingLastInventoryItemPinsItsSnapshotWhileStillFillingTheSelectedMaterialSlot()
    {
        var a = Item("A", 1); var tooltip = Tooltip(); var ui = LiveInventory(tooltip);
        Click(panel.materialSlots[2].slotToggle);
        var slot = ui.slotParent.GetComponentInChildren<SubPlantInventorySlotUI>();
        Click(slot.slotToggle);
        Assert.That(manager.GetMaterialAt(2).itemData, Is.SameAs(a));
        Assert.That(inventory.GetItemAmount(a), Is.Zero);
        Assert.That(slot.gameObject.activeSelf, Is.False);
        Assert.That(tooltip.tooltipPanel.activeSelf, Is.True); Assert.That(tooltip.dataNameText.text, Is.EqualTo("A"));
        slot.GetComponent<ItemTooltipTrigger>().OnPointerExit(new PointerEventData(null));
        Assert.That(tooltip.tooltipPanel.activeSelf, Is.True);
    }

    [Test]
    public void RecycledRowsKeepTheClickedInfoAndNextSelectionPinsTheNewItem()
    {
        var a = Item("A", 1); var b = Item("B", 3); var tooltip = Tooltip(); var ui = LiveInventory(tooltip);
        var row = ui.slotParent.GetComponentsInChildren<SubPlantInventorySlotUI>().Single(s => s.currentItem.itemData == a);
        Click(row.slotToggle);
        Assert.That(row.currentItem.itemData, Is.SameAs(b)); Assert.That(tooltip.dataNameText.text, Is.EqualTo("A"));
        var hover = InventorySlot(b).GetComponent<ItemTooltipTrigger>(); hover.tooltipUI = tooltip; hover.showOnSelection = false;
        hover.OnPointerEnter(new PointerEventData(null)); hover.OnPointerExit(new PointerEventData(null));
        Assert.That(tooltip.dataNameText.text, Is.EqualTo("A")); Assert.That(tooltip.tooltipPanel.activeSelf, Is.True);
        Click(row.slotToggle);
        Assert.That(tooltip.dataNameText.text, Is.EqualTo("B"));
        Assert.That(manager.GetCurrentMaterialCount(), Is.EqualTo(2));
        ui.gameObject.SetActive(false);
        Assert.That(tooltip.tooltipPanel.activeSelf, Is.False);
    }

    [Test]
    public void HoverModeStillShowsAndHidesInformationAndInventorySelectionStillAddsMaterials()
    {
        var item = Item("A"); var slot = InventorySlot(item); var tooltip = Tooltip();
        var trigger = slot.GetComponent<ItemTooltipTrigger>(); trigger.tooltipUI = tooltip; trigger.showOnSelection = false;
        trigger.OnPointerEnter(new PointerEventData(null)); Assert.That(tooltip.tooltipPanel.activeSelf, Is.True);
        trigger.OnPointerExit(new PointerEventData(null)); Assert.That(tooltip.tooltipPanel.activeSelf, Is.False);
        Click(slot.slotToggle);
        Assert.That(manager.GetMaterialAt(0).itemData, Is.SameAs(item));
    }
}

public sealed class SubPlantSlotAuthoringTests
{
    [Test]
    public void SavedUpgradeSceneAndSlotPrefabsRetainTheRequestedToggleConnections()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/UpgradeScene.unity");
        var roots = scene.GetRootGameObjects();
        var panel = roots.SelectMany(r => r.GetComponentsInChildren<ItemCombinationPanelUI>(true)).Single();
        Assert.That(panel.manager, Is.Not.Null);
        foreach (var slot in panel.materialSlots)
        {
            Assert.That(slot.slotButton, Is.Null);
            Assert.That(slot.slotToggle, Is.Not.Null);
            Assert.That(slot.slotToggle.graphic, Is.Not.Null);
            Assert.That(slot.ready, Is.Not.Null);
        }
        foreach (var inventory in roots.SelectMany(r => r.GetComponentsInChildren<InventoryUI>(true)))
        {
            Assert.That(inventory.showTooltipOnSelection, Is.True);
            Assert.That(inventory.tooltipUI, Is.Not.Null);
            Assert.That(inventory.slotPrefab.GetComponent<Toggle>(), Is.Not.Null);
            Assert.That(inventory.slotPrefab.GetComponent<ItemTooltipTrigger>(), Is.Not.Null);
            Assert.That(inventory.slotPrefab.GetComponent<ItemSlotTooltipProvider>(), Is.Not.Null);
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }
}
