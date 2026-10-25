using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class UpgradePanelSceneTests
{
    private const string ScenePath = "Assets/Scenes/UpgradeScene.unity";

    private static T InScene<T>() where T : Component
    {
        return SceneManager.GetSceneByPath(ScenePath).GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();
    }

    [UnityTearDown]
    public IEnumerator LeavePlayMode()
    {
        if (Application.isPlaying)
            yield return new ExitPlayMode();
    }

    private static void AssertClickable(Selectable control)
    {
        Canvas.ForceUpdateCanvases();
        Graphic graphic = control.targetGraphic != null ? control.targetGraphic :
            control.GetComponentsInChildren<Graphic>().First(candidate => candidate.raycastTarget);
        // Inventory entries may start below the visible rows; scroll them into view first.
        ScrollRect scroll = control.GetComponentInParent<ScrollRect>();
        if (scroll != null && scroll.viewport != null)
        {
            Vector2 point = scroll.viewport.InverseTransformPoint(
                graphic.rectTransform.TransformPoint(graphic.rectTransform.rect.center));
            if (!scroll.viewport.rect.Contains(point))
            {
                Vector2 offset = scroll.viewport.rect.center - point;
                if (!scroll.horizontal) offset.x = 0f;
                if (!scroll.vertical) offset.y = 0f;
                scroll.content.anchoredPosition += offset;
                Canvas.ForceUpdateCanvases();
            }
        }
        Canvas canvas = graphic.canvas;
        var pointer = new PointerEventData(EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                graphic.rectTransform.TransformPoint(graphic.rectTransform.rect.center))
        };
        var hits = new List<RaycastResult>();
        canvas.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
        Assert.That(hits, Is.Not.Empty, control.name + " must receive UI pointer hits.");
        Assert.That(hits[0].gameObject.transform.IsChildOf(control.transform), Is.True,
            control.name + " is covered by " + hits[0].gameObject.name);
    }

    [UnityTest]
    public IEnumerator AuthoredPanelsConnectSkillsShopAndInventoryOnlyPotionCrafting()
    {
        EditorSceneManager.OpenScene(ScenePath);
        yield return new EnterPlayMode();
        yield return null;
        yield return null;

        UIPanelSwitcher tabs = InScene<UIPanelSwitcher>();
        Assert.That(tabs.panels.Length, Is.EqualTo(3));
        Assert.That(tabs.panelToggles.Length, Is.EqualTo(3));
        Assert.That(tabs.panels[0].gameObject.activeInHierarchy, Is.True);
        AssertClickable(tabs.panelToggles[1]);
        foreach (CurrencyType currency in Enum.GetValues(typeof(CurrencyType)))
            CurrencyManager.instance.AddCurrency(currency, 100000);

        // Hover uses the authored fixed side panel, without changing its position.
        SkillTreeMapPanel maps = InScene<SkillTreeMapPanel>();
        Assert.That(maps.maps.Length, Is.EqualTo(2));
        SkillNodeUI node = maps.maps[0].graph.GetComponentsInChildren<SkillNodeUI>(true)
            .First(candidate => candidate.SkillNodeData != null &&
                (candidate.SkillNodeData.requiredSkills == null || candidate.SkillNodeData.requiredSkills.Count == 0));
        var hover = node.GetComponent<ToggleTooltipTrigger>();
        var pointer = new PointerEventData(EventSystem.current);
        Vector3 fixedPosition = maps.nodeInformation.tooltipPanel.transform.localPosition;
        ExecuteEvents.Execute(hover.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        Assert.That(maps.nodeInformation.tooltipPanel.activeInHierarchy, Is.True);
        Assert.That(maps.nodeInformation.titleText.text, Is.EqualTo(node.SkillNodeData.GetDataName()));
        Assert.That(maps.nodeInformation.tooltipPanel.transform.localPosition, Is.EqualTo(fixedPosition));
        node.GetComponentInChildren<Button>(true).onClick.Invoke();
        Assert.That(SkillTreeManager.Instance.IsUnlocked(node.SkillNodeData.dataId), Is.True);
        ExecuteEvents.Execute(hover.gameObject, pointer, ExecuteEvents.pointerExitHandler);
        Assert.That(maps.nodeInformation.tooltipPanel.activeSelf, Is.False);

        AssertClickable(maps.maps[1].toggle);
        maps.maps[1].toggle.isOn = true;
        Assert.That(maps.maps[0].graph.gameObject.activeSelf, Is.False);
        Assert.That(maps.maps[1].graph.gameObject.activeSelf, Is.True);
        UIZoomController zoom = maps.maps[1].zoom;
        float beforeZoom = zoom.zoomTarget.localScale.x;
        pointer.scrollDelta = Vector2.up;
        zoom.OnScroll(pointer);
        Assert.That(zoom.zoomTarget.localScale.x, Is.GreaterThan(beforeZoom));
        Assert.That(maps.maps[1].scrollRect.horizontal && maps.maps[1].scrollRect.vertical, Is.True);

        tabs.panelToggles[1].isOn = true;
        yield return new WaitForSecondsRealtime(.2f);
        Assert.That(tabs.panels.Count(panel => panel.gameObject.activeInHierarchy), Is.EqualTo(1));
        Assert.That(tabs.panels[1].gameObject.activeInHierarchy, Is.True);
        ShopManager shop = InScene<ShopManager>();
        yield return null; // First activation binds the existing box-button listeners.
        shop.boxButtons[0].button.onClick.Invoke();
        Assert.That(shop.boxInfoUI.pannel.activeInHierarchy, Is.True);
        Assert.That(shop.boxInfoUI.boxNameText.text, Is.Not.Empty);
        AssertClickable(shop.boxInfoUI.buyButton);
        int beforePurchase = InventoryManager.instance.items.Sum(item => item.amount);
        shop.boxInfoUI.buyButton.onClick.Invoke();
        Assert.That(InventoryManager.instance.items.Sum(item => item.amount), Is.EqualTo(beforePurchase + 1));
        Assert.That(shop.purchaseResultPanelUI.panelRoot.activeInHierarchy, Is.True);
        shop.purchaseResultPanelUI.confirmButton.onClick.Invoke();
        Assert.That(shop.purchaseResultPanelUI.panelRoot.activeSelf, Is.False);

        tabs.panelToggles[2].isOn = true;
        yield return new WaitForSecondsRealtime(.2f);
        ItemCombinationPanelUI potions = InScene<ItemCombinationPanelUI>();
        Assert.That(tabs.panels.Count(panel => panel.gameObject.activeInHierarchy), Is.EqualTo(1));
        Assert.That(potions.manager, Is.Not.Null, "The saved potion panel must retain its scene manager reference.");
        Assert.That(potions.manager.recipes, Is.Not.Null);
        foreach (ItemRecipeList list in potions.manager.recipes)
        {
            Assert.That(list, Is.Not.Null);
            Assert.That(list.recipes, Is.Not.Null, "Recipe entries must survive saving and reopening the scene.");
        }
        var availableRecipes = potions.manager.GetAllRecipe();
        Assert.That(availableRecipes.Count, Is.GreaterThan(0));
        ItemRecipeData recipe = availableRecipes[0];
        Assert.That(recipe, Is.Not.Null);
        RecipeInvenUI recipeUI = potions.GetComponentInChildren<RecipeInvenUI>(true);
        Assert.That(recipeUI, Is.Not.Null);
        Assert.That(recipeUI.recipeManager, Is.SameAs(potions.manager));
        foreach (RecipeMaterial material in recipe.materials)
            InventoryManager.instance.AddItem(material.itemData, material.amount);
        yield return null;
        InventoryUI inventoryUI = potions.GetComponentInChildren<InventoryUI>(true);
        foreach (RecipeMaterial material in recipe.materials)
            for (int i = 0; i < material.amount; i++)
            {
                SubPlantInventorySlotUI slot = inventoryUI.slotParent.GetComponentsInChildren<SubPlantInventorySlotUI>()
                    .First(candidate => candidate.currentItem != null && candidate.currentItem.itemData == material.itemData);
                AssertClickable(slot.slotButton);
                slot.slotButton.onClick.Invoke();
            }
        Assert.That(potions.resultSlot.currentItem.itemData, Is.EqualTo(recipe.resultItem));
        int materialCount = potions.manager.GetCurrentMaterialCount();
        ItemData returned = potions.materialSlots[0].currentItem.itemData;
        int beforeReturn = InventoryManager.instance.GetItemAmount(returned);
        potions.materialSlots[0].slotButton.onClick.Invoke();
        Assert.That(potions.manager.GetCurrentMaterialCount(), Is.EqualTo(materialCount - 1));
        Assert.That(InventoryManager.instance.GetItemAmount(returned), Is.EqualTo(beforeReturn + 1));
        // Keep recipe out of a method-wide closure: EnterPlayMode reloads this iterator's domain.
        RecipeSlotUI recipeSlot = null;
        foreach (RecipeSlotUI candidate in recipeUI.recipeSlotParent.GetComponentsInChildren<RecipeSlotUI>())
            if (candidate.currentItem == recipe)
            {
                recipeSlot = candidate;
                break;
            }
        Assert.That(recipeSlot, Is.Not.Null);
        AssertClickable(recipeSlot.fillButton);
        recipeSlot.fillButton.onClick.Invoke();
        Assert.That(potions.manager.GetCurrentMaterialCount(), Is.EqualTo(materialCount));

        int beforeResult = InventoryManager.instance.GetItemAmount(recipe.resultItem);
        // Repeated preview refreshes never consume materials or grant/use a result.
        potions.RefreshUI();
        potions.RefreshUI();
        Assert.That(potions.manager.GetCurrentMaterialCount(), Is.EqualTo(materialCount));
        Assert.That(InventoryManager.instance.GetItemAmount(recipe.resultItem), Is.EqualTo(beforeResult));
        AssertClickable(potions.combineButton);
        potions.combineButton.onClick.Invoke();
        Assert.That(InventoryManager.instance.GetItemAmount(recipe.resultItem), Is.EqualTo(beforeResult + 1));
        Assert.That(potions.manager.GetCurrentMaterialCount(), Is.Zero);
        Assert.That(potions.resultPanel.panelRoot.activeInHierarchy, Is.True);
        potions.Combine();
        Assert.That(InventoryManager.instance.GetItemAmount(recipe.resultItem), Is.EqualTo(beforeResult + 1));

        // Rapid tab changes leave exactly one visible panel and preserve selector identity.
        tabs.panelToggles[0].isOn = true;
        tabs.panelToggles[1].isOn = true;
        tabs.panelToggles[2].isOn = true;
        yield return new WaitForSecondsRealtime(.2f);
        Assert.That(tabs.panels.Count(panel => panel.gameObject.activeInHierarchy), Is.EqualTo(1));
        Assert.That(tabs.panels[2].gameObject.activeInHierarchy, Is.True);
    }
}
