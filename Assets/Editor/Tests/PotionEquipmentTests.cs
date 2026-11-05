using System;
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

public sealed class PotionEquipmentTests
{
    private const string Folder = "Assets/Prefab/UI/UpgradeCanvas/Main/Potion/";
    private readonly List<Object> owned = new List<Object>();
    private InventoryManager inventory;
    private PotionEquipmentManager manager;
    private ItemData first, second;

    private GameObject Host(string name, Transform parent = null, bool active = true)
    {
        var host = new GameObject(name, typeof(RectTransform)); host.SetActive(active); owned.Add(host);
        if (parent != null) host.transform.SetParent(parent, false);
        return host;
    }
    private ItemData Potion(string name, ItemSeries series = ItemSeries.Potions)
    {
        var item = ScriptableObject.CreateInstance<ItemData>(); owned.Add(item);
        item.name = item.dataId = name; item.series = series;
        item.data = new[] { new Description { language = language.english, dataName = name } };
        return item;
    }
    private ItemDefinitionUI Detail(string name, Transform parent)
    {
        var ui = Host(name, parent).AddComponent<ItemDefinitionUI>();
        ui.dataNameText = Host("Name", ui.transform).AddComponent<TextMeshProUGUI>();
        return ui;
    }
    private PotionEquipmentPanelUI Panel(PotionCraftManager craftManager = null)
    {
        var host = Host("Equipment panel", active: false);
        var panel = host.AddComponent<PotionEquipmentPanelUI>(); panel.manager = manager;
        if (craftManager != null) host.AddComponent<ItemCombinationPanelUI>().manager = craftManager;
        var slotRoot = Host("Slots", host.transform);
        var group = slotRoot.AddComponent<ToggleGroup>(); group.allowSwitchOff = true;
        panel.equipmentSlots = new PotionEquipSlotUI[8];
        var slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "PotionEquipSlot.prefab");
        for (int i = 0; i < panel.equipmentSlots.Length; i++)
        {
            var slot = Object.Instantiate(slotPrefab, slotRoot.transform).GetComponent<PotionEquipSlotUI>();
            slot.slotToggle.group = group; panel.equipmentSlots[i] = slot;
        }
        panel.selectedDetails = Host("Selected", host.transform);
        panel.notSelectedDetails = Host("Not selected", host.transform);
        panel.equippedPotion = Detail("Equipped potion", panel.selectedDetails.transform);
        panel.newPotion = Detail("New potion", panel.selectedDetails.transform);
        panel.replacementArrow = Host("Arrow", panel.selectedDetails.transform);
        panel.selectedConfirmArea = Host("Selected actions", host.transform);
        panel.notSelectedConfirmArea = Host("Default actions", host.transform);
        panel.unequipButton = Host("Unequip", panel.selectedConfirmArea.transform).AddComponent<Button>();
        panel.replaceButton = Host("Replace", panel.selectedConfirmArea.transform).AddComponent<Button>();
        panel.clearButton = Host("Clear", panel.notSelectedConfirmArea.transform).AddComponent<Button>();
        panel.inventoryView = Host("Inventory", host.transform).AddComponent<PotionEquipmentInventoryUI>();
        panel.inventoryView.equipmentPanel = panel;
        panel.inventoryView.slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "PotionEquipInventorySlot.prefab");
        panel.inventoryView.slotParent = Host("Rows", panel.inventoryView.transform).transform;
        panel.inventoryView.ClearSearchFilter(); panel.inventoryView.Init();
        host.SetActive(true);
        return panel;
    }
    private static void Click(Toggle toggle)
        => ExecuteEvents.Execute(toggle.gameObject, new PointerEventData(null) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
    private static PotionEquipInventorySlotUI Row(PotionEquipmentPanelUI panel, ItemData item)
        => panel.inventoryView.slotParent.GetComponentsInChildren<PotionEquipInventorySlotUI>().Single(s => s.currentItem.itemData == item);

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        inventory = Host("Inventory manager").AddComponent<InventoryManager>(); InventoryManager.instance = inventory;
        manager = Host("Equipment manager").AddComponent<PotionEquipmentManager>(); manager.inventory = inventory;
        first = Potion("Potion A"); second = Potion("Potion B");
    }
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear(); InventoryManager.instance = null;
        yield return new ExitPlayMode();
    }

    [Test]
    public void EmptyUnlockedSlotsReserveOnePotionAndUnequippingReturnsItOnlyOnce()
    {
        inventory.AddItem(first, 2);
        var material = Potion("Weapon", ItemSeries.Weapon); inventory.AddItem(material, 3);
        manager.SetSlotLocked(1, true);
        Assert.That(manager.TryEquip(-1, first), Is.False); Assert.That(manager.TryEquip(8, first), Is.False);
        Assert.That(manager.TryEquip(1, first), Is.False); Assert.That(manager.TryEquip(0, material), Is.False);
        Assert.That(manager.TryReplace(0, first), Is.False);
        Assert.That(inventory.GetItemAmount(first), Is.EqualTo(2));
        Assert.That(manager.TryEquip(0, first), Is.True);
        Assert.That(manager.GetPotion(0), Is.SameAs(first)); Assert.That(inventory.GetItemAmount(first), Is.EqualTo(1));
        Assert.That(manager.TryEquip(0, first), Is.False);
        Assert.That(manager.TryUnequip(0), Is.True); Assert.That(manager.TryUnequip(0), Is.False);
        Assert.That(inventory.GetItemAmount(first), Is.EqualTo(2));
        Assert.That(inventory.GetItemAmount(material), Is.EqualTo(3));
    }

    [Test]
    public void ReplacementTransfersOneUnitAndInventoryCallbacksCannotDuplicateOrRefundIt()
    {
        inventory.AddItem(first, 2); inventory.AddItem(second, 2); manager.TryEquip(0, first);
        int callbacks = 0;
        Action reenter = () =>
        {
            callbacks++;
            Assert.That(manager.TryUnequip(0), Is.False);
            Assert.That(manager.TryReplace(0, first), Is.False);
            Assert.That(manager.SetSlotLocked(0, true), Is.False);
        };
        inventory.onInventoryChanged += reenter;
        try { Assert.That(manager.TryReplace(0, second), Is.True); }
        finally { inventory.onInventoryChanged -= reenter; }
        Assert.That(callbacks, Is.EqualTo(2));
        Assert.That(manager.GetPotion(0), Is.SameAs(second));
        Assert.That(inventory.GetItemAmount(first), Is.EqualTo(2)); Assert.That(inventory.GetItemAmount(second), Is.EqualTo(1));
        Assert.That(manager.TryReplace(0, second), Is.False, "The same potion must not replace itself.");
        Assert.That(inventory.GetItemAmount(second), Is.EqualTo(1));
        manager.TryUnequip(0); Assert.That(inventory.GetItemAmount(second), Is.EqualTo(2));
    }

    [Test]
    public void AuthoredInventoryEquipsSelectedEmptySlotAndOnlyConfirmedReplacementSpendsStock()
    {
        inventory.AddItem(first, 3); inventory.AddItem(second, 2);
        inventory.AddItem(Potion("Material", ItemSeries.Mineral));
        var crafting = Host("Separate crafting manager").AddComponent<PotionCraftManager>();
        var panel = Panel(crafting);
        Assert.That(panel.inventoryView.slotParent.GetComponentsInChildren<PotionEquipInventorySlotUI>().Length, Is.EqualTo(2));
        Click(Row(panel, first).slotToggle);
        Assert.That(inventory.GetItemAmount(first), Is.EqualTo(2));
        Assert.That(manager.GetPotion(0), Is.SameAs(first), "An unselected destination uses the first empty slot.");
        Click(panel.equipmentSlots[2].slotToggle); Click(Row(panel, first).slotToggle);
        Assert.That(manager.GetPotion(2), Is.SameAs(first)); Assert.That(inventory.GetItemAmount(first), Is.EqualTo(1));
        Assert.That(panel.SelectedSlot, Is.EqualTo(-1)); Assert.That(panel.equipmentSlots[2].slotToggle.isOn, Is.False);
        Click(panel.equipmentSlots[2].slotToggle);
        Assert.That(panel.equippedPotion.dataNameText.text, Is.EqualTo("Potion A"));
        Assert.That(panel.equippedPotion.gameObject.activeSelf, Is.True); Assert.That(panel.newPotion.gameObject.activeSelf, Is.False);
        Assert.That(panel.unequipButton.interactable, Is.True); Assert.That(panel.replaceButton.gameObject.activeSelf, Is.False);

        Click(Row(panel, second).slotToggle);
        Assert.That(manager.GetPotion(2), Is.SameAs(first)); Assert.That(inventory.GetItemAmount(second), Is.EqualTo(2));
        Assert.That(panel.newPotion.dataNameText.text, Is.EqualTo("Potion B"));
        Assert.That(panel.newPotion.gameObject.activeSelf, Is.True); Assert.That(panel.replacementArrow.activeSelf, Is.True);
        Assert.That(panel.unequipButton.interactable && panel.replaceButton.interactable, Is.True);
        crafting.ReturnMaterials();
        Assert.That(Row(panel, second).slotToggle.isOn, Is.True, "Crafting material returns cannot reset equipment inventory selection.");
        Assert.That(crafting.GetCurrentMaterialCount(), Is.Zero);
        panel.replaceButton.onClick.Invoke();
        Assert.That(manager.GetPotion(2), Is.SameAs(second));
        Assert.That(inventory.GetItemAmount(first), Is.EqualTo(2)); Assert.That(inventory.GetItemAmount(second), Is.EqualTo(1));
        Assert.That(panel.PendingPotion, Is.Null); Assert.That(panel.newPotion.gameObject.activeSelf, Is.False);
        panel.unequipButton.onClick.Invoke();
        Assert.That(manager.GetPotion(2), Is.Null); Assert.That(inventory.GetItemAmount(second), Is.EqualTo(2));
        Assert.That(panel.selectedDetails.activeSelf, Is.False);
    }

    [Test]
    public void AutomaticEquipmentSkipsLockedSlotsAndNeverOverwritesWhenFull()
    {
        inventory.AddItem(first, 10); manager.SetSlotLocked(0, true); manager.SetSlotLocked(3, true);
        var panel = Panel(); var row = Row(panel, first);
        foreach (int index in new[] { 1, 2, 4, 5, 6, 7 })
        {
            Click(row.slotToggle);
            Assert.That(manager.GetPotion(index), Is.SameAs(first));
            Assert.That(panel.SelectedSlot, Is.EqualTo(-1));
            Assert.That(panel.equipmentSlots.All(s => !s.slotToggle.isOn), Is.True);
            Assert.That(row.slotToggle.isOn, Is.True);
        }
        Click(row.slotToggle);
        Assert.That(inventory.GetItemAmount(first), Is.EqualTo(4));
        Assert.That(manager.FindFirstEmptySlot(), Is.EqualTo(-1));
        Assert.That(manager.GetPotion(0), Is.Null); Assert.That(manager.GetPotion(3), Is.Null);
        Assert.That(panel.PendingPotion, Is.Null); Assert.That(panel.selectedDetails.activeSelf, Is.False);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void FillingExplicitEmptyEquipmentSlotClearsOnlyDestinationEvenInRequiredToggleGroup(bool allowSwitchOff)
    {
        inventory.AddItem(first, 3); var panel = Panel();
        var group = panel.equipmentSlots[0].slotToggle.group; group.allowSwitchOff = allowSwitchOff;
        Click(panel.equipmentSlots[2].slotToggle);
        var row = Row(panel, first); Click(row.slotToggle);
        Assert.That(manager.GetPotion(2), Is.SameAs(first)); Assert.That(panel.SelectedSlot, Is.EqualTo(-1));
        Assert.That(panel.equipmentSlots.All(s => !s.slotToggle.isOn), Is.True);
        Assert.That(row.slotToggle.isOn, Is.True);
        Click(row.slotToggle);
        Assert.That(manager.GetPotion(0), Is.SameAs(first));
        Assert.That(inventory.GetItemAmount(first), Is.EqualTo(1));
        Assert.That(panel.PendingPotion, Is.Null); Assert.That(group.allowSwitchOff, Is.EqualTo(allowSwitchOff));
    }

    [Test]
    public void ClickingEquippedPotionCannotProposeOrConfirmAnIdenticalReplacement()
    {
        inventory.AddItem(first, 3); inventory.AddItem(second);
        manager.TryEquip(0, first); var panel = Panel();
        Click(panel.equipmentSlots[0].slotToggle); Click(Row(panel, second).slotToggle);
        Assert.That(panel.PendingPotion, Is.SameAs(second));
        var row = Row(panel, first); Click(row.slotToggle); Click(row.slotToggle);
        Assert.That(panel.PendingPotion, Is.Null); Assert.That(panel.SelectedSlot, Is.Zero);
        Assert.That(panel.newPotion.gameObject.activeSelf, Is.False); Assert.That(panel.replaceButton.gameObject.activeSelf, Is.False);
        Assert.That(panel.unequipButton.interactable, Is.True);
        panel.ConfirmReplace();
        Assert.That(inventory.GetItemAmount(first), Is.EqualTo(2));
        Assert.That(inventory.GetItemAmount(second), Is.EqualTo(1));
        Assert.That(manager.GetPotion(0), Is.SameAs(first));
    }

    [Test]
    public void EmptySlotFilledThroughManagerAlsoClearsDestinationSelection()
    {
        inventory.AddItem(first, 2); var panel = Panel();
        Click(panel.equipmentSlots[2].slotToggle);
        Assert.That(manager.TryEquip(2, first), Is.True);
        Assert.That(panel.SelectedSlot, Is.EqualTo(-1)); Assert.That(panel.equipmentSlots[2].slotToggle.isOn, Is.False);
        Click(Row(panel, first).slotToggle);
        Assert.That(manager.GetPotion(0), Is.SameAs(first));
        Assert.That(manager.GetPotion(2), Is.SameAs(first)); Assert.That(inventory.GetItemAmount(first), Is.Zero);
    }

    [Test]
    public void EquipmentValueChangesOnlySelectAndEachClickOrSubmitPerformsExactlyOneAction()
    {
        inventory.AddItem(first, 5); var panel = Panel(); var row = Row(panel, first);
        Assert.That(row.slotToggle, Is.TypeOf<PotionEquipInventoryToggle>());
        int valueChanges = 0; row.slotToggle.onValueChanged.AddListener(_ => valueChanges++);
        row.slotToggle.isOn = true;
        Assert.That(manager.GetPotion(0), Is.Null, "Selection changes cannot trigger equipment actions.");
        Click(row.slotToggle); Click(row.slotToggle);
        ExecuteEvents.Execute(row.gameObject, new BaseEventData(null), ExecuteEvents.submitHandler);
        Assert.That(manager.GetPotion(0), Is.SameAs(first)); Assert.That(manager.GetPotion(1), Is.SameAs(first));
        Assert.That(manager.GetPotion(2), Is.SameAs(first)); Assert.That(manager.GetPotion(3), Is.Null);
        Assert.That(inventory.GetItemAmount(first), Is.EqualTo(2));
        Assert.That(valueChanges, Is.EqualTo(1), "Repeated equipment actions must not replay toggle value changes.");
        ExecuteEvents.Execute(row.gameObject, new PointerEventData(null) { button = PointerEventData.InputButton.Right }, ExecuteEvents.pointerClickHandler);
        row.slotToggle.interactable = false;
        Click(row.slotToggle); ExecuteEvents.Execute(row.gameObject, new BaseEventData(null), ExecuteEvents.submitHandler);
        Assert.That(inventory.GetItemAmount(first), Is.EqualTo(2)); Assert.That(manager.GetPotion(3), Is.Null);
    }

    [Test]
    public void SelectingOnlyAnOccupiedSlotShowsOnlyEquippedPotionAndChangingSlotsCancelsPendingReplacement()
    {
        inventory.AddItem(first, 2); inventory.AddItem(second, 2);
        manager.TryEquip(0, first); manager.TryEquip(1, second);
        var panel = Panel(); Click(panel.equipmentSlots[0].slotToggle);
        Assert.That(panel.equippedPotion.dataNameText.text, Is.EqualTo("Potion A"));
        Assert.That(panel.newPotion.gameObject.activeSelf, Is.False); Assert.That(panel.unequipButton.interactable, Is.True);
        Click(Row(panel, second).slotToggle); Assert.That(panel.PendingPotion, Is.SameAs(second));
        Click(panel.equipmentSlots[1].slotToggle);
        Assert.That(panel.SelectedSlot, Is.EqualTo(1)); Assert.That(panel.equipmentSlots[0].slotToggle.isOn, Is.False);
        Assert.That(panel.PendingPotion, Is.Null); Assert.That(panel.newPotion.gameObject.activeSelf, Is.False);
        Assert.That(panel.equippedPotion.dataNameText.text, Is.EqualTo("Potion B"));
        Click(panel.equipmentSlots[1].slotToggle);
        Assert.That(panel.SelectedSlot, Is.EqualTo(-1)); Assert.That(panel.selectedDetails.activeSelf, Is.False);
        Assert.That(inventory.GetItemAmount(second), Is.EqualTo(1));
    }

    [Test]
    public void ShortageDisablesReplacementWithoutChangingEquipmentAndLocksBlockSelectionAndActions()
    {
        inventory.AddItem(first); inventory.AddItem(second);
        manager.TryEquip(0, first); var panel = Panel();
        Click(panel.equipmentSlots[0].slotToggle); Click(Row(panel, second).slotToggle);
        inventory.RemoveItem(second);
        Assert.That(panel.newPotion.gameObject.activeSelf, Is.True); Assert.That(panel.replaceButton.interactable, Is.False);
        panel.ConfirmReplace();
        Assert.That(manager.GetPotion(0), Is.SameAs(first)); Assert.That(panel.PendingPotion, Is.SameAs(second));
        inventory.AddItem(second); Assert.That(panel.replaceButton.interactable, Is.True);
        manager.SetSlotLocked(0, true);
        Assert.That(panel.SelectedSlot, Is.EqualTo(-1)); Assert.That(panel.PendingPotion, Is.Null);
        Assert.That(panel.equipmentSlots[0].locked.activeSelf, Is.True); Assert.That(panel.equipmentSlots[0].slotToggle.interactable, Is.False);
        Click(panel.equipmentSlots[0].slotToggle); Assert.That(panel.SelectedSlot, Is.EqualTo(-1));
        Assert.That(manager.TryReplace(0, second), Is.False); Assert.That(manager.TryUnequip(0), Is.False);
        manager.SetSlotLocked(0, false); Click(panel.equipmentSlots[0].slotToggle);
        Assert.That(panel.equippedPotion.dataNameText.text, Is.EqualTo("Potion A"));
    }

    [Test]
    public void EightSlotsRemainIndependentAndClearReturnsOnlyUnlockedEquipment()
    {
        inventory.AddItem(first, 8);
        for (int i = 0; i < 8; i++) Assert.That(manager.TryEquip(i, first), Is.True);
        manager.SetSlotLocked(7, true); var panel = Panel();
        panel.clearButton.onClick.Invoke();
        for (int i = 0; i < 7; i++) Assert.That(manager.GetPotion(i), Is.Null);
        Assert.That(manager.GetPotion(7), Is.SameAs(first)); Assert.That(inventory.GetItemAmount(first), Is.EqualTo(7));
        manager.SetSlotLocked(7, false); manager.TryUnequip(7);
        Assert.That(inventory.GetItemAmount(first), Is.EqualTo(8));
    }

    [Test]
    public void ClosingPanelKeepsEquipmentAndReopeningDoesNotDuplicateButtonActions()
    {
        inventory.AddItem(first); manager.TryEquip(0, first); var panel = Panel();
        Click(panel.equipmentSlots[0].slotToggle);
        for (int i = 0; i < 2; i++)
        {
            panel.gameObject.SetActive(false); panel.gameObject.SetActive(true);
            Assert.That(manager.GetPotion(0), Is.SameAs(first));
            Assert.That(panel.SelectedSlot, Is.EqualTo(-1));
        }
        Click(panel.equipmentSlots[0].slotToggle); panel.unequipButton.onClick.Invoke();
        Assert.That(inventory.GetItemAmount(first), Is.EqualTo(1)); Assert.That(manager.GetPotion(0), Is.Null);
    }
}

public sealed class PotionEquipmentAuthoringTests
{
    [Test]
    public void UpgradeSceneConnectsEightAuthoredSlotsSeparateInventoryAndConfirmationUI()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/UpgradeScene.unity", OpenSceneMode.Single);
        try
        {
            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            var panel = all.Select(t => t.GetComponent<PotionEquipmentPanelUI>()).Single(c => c != null);
            Assert.That(panel.manager.inventory, Is.Not.Null); Assert.That(panel.manager.SlotCount, Is.EqualTo(8));
            Assert.That(panel.inventoryView.equipmentPanel, Is.SameAs(panel));
            Assert.That(panel.equipmentSlots.Length, Is.EqualTo(8));
            foreach (var slot in panel.equipmentSlots)
            {
                Assert.That(slot, Is.Not.Null); Assert.That(slot.icon, Is.Not.Null); Assert.That(slot.ready, Is.Not.Null); Assert.That(slot.locked, Is.Not.Null);
                Assert.That(slot.slotToggle.group, Is.Not.Null);
                Assert.That(slot.slotToggle.onValueChanged.GetPersistentEventCount(), Is.Zero, "Old visibility callbacks must not compete with confirmation state.");
            }
            var prefab = panel.inventoryView.slotPrefab;
            Assert.That(prefab.GetComponent<PotionEquipInventorySlotUI>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<SubPlantInventorySlotUI>(), Is.Null);
            Assert.That(prefab.GetComponent<PotionEquipInventoryToggle>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<RepeatClickToggle>(), Is.Null);
            Assert.That(prefab.GetComponent<PotionEquipInventorySlotUI>().amountText, Is.Not.Null);
            Assert.That(panel.equippedPotion.icon, Is.Not.Null); Assert.That(panel.newPotion.dataNameText, Is.Not.Null);
            Assert.That(panel.unequipButton, Is.Not.Null); Assert.That(panel.replaceButton, Is.Not.Null); Assert.That(panel.clearButton, Is.Not.Null);
            var craft = all.Select(t => t.GetComponent<ItemCombinationPanelUI>()).Single(c => c != null);
            Assert.That(craft.manager, Is.Not.Null);
            var craftInventory = craft.transform.Find("Main/Main/Padding/Pannels/InventoryPotionCraft/Inventory").GetComponent<InventoryUI>();
            Assert.That(craftInventory.slotPrefab.GetComponent<SubPlantInventorySlotUI>(), Is.Not.Null);
            Assert.That(craftInventory.slotPrefab.GetComponent<RepeatClickToggle>(), Is.Not.Null);
            var search = panel.inventoryView.GetComponentInChildren<InventorySearchUI>(true);
            Assert.That(search.GetTargetUI(), Is.SameAs(panel.inventoryView));
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
    }
}
