using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One authored inventory view. Its source and click action follow the potion tab.
public sealed class PotionPanelInventoryUI : InventoryUI
{
    private enum Mode { Craft, Equip, Recipe }

    [SerializeField] private ItemCombinationPanelUI crafting;
    [SerializeField] private PotionEquipmentPanelUI equipment;
    [SerializeField] private PotionRecipePanelUI recipes;
    [SerializeField] private Toggle craftTab;
    [SerializeField] private Toggle equipTab;
    [SerializeField] private Toggle recipeTab;
    [SerializeField] private TMP_Text title;

    private readonly List<ItemRecipeData> matchingRecipes = new List<ItemRecipeData>();
    private Mode mode;

    protected override void OnEnable()
    {
        base.OnEnable();
        craftTab.onValueChanged.AddListener(OnCraft);
        equipTab.onValueChanged.AddListener(OnEquip);
        recipeTab.onValueChanged.AddListener(OnRecipes);
        SetMode(recipeTab.isOn ? Mode.Recipe : equipTab.isOn ? Mode.Equip : Mode.Craft);
    }

    protected override void OnDisable()
    {
        craftTab.onValueChanged.RemoveListener(OnCraft);
        equipTab.onValueChanged.RemoveListener(OnEquip);
        recipeTab.onValueChanged.RemoveListener(OnRecipes);
        base.OnDisable();
    }

    private void OnCraft(bool active) { if (active) SetMode(Mode.Craft); }
    private void OnEquip(bool active) { if (active) SetMode(Mode.Equip); }
    private void OnRecipes(bool active) { if (active) SetMode(Mode.Recipe); }

    private void SetMode(Mode selected)
    {
        if (mode != selected)
        {
            ClearSelection();
            tooltipUI?.Hide();
        }
        mode = selected;
        crafting.gameObject.SetActive(mode == Mode.Craft);
        equipment.gameObject.SetActive(mode == Mode.Equip);
        recipes.gameObject.SetActive(mode == Mode.Recipe);
        SetTitle();
        Init();
    }

    private void SetTitle()
    {
        if (title == null) return;
        bool korean = LanguageManager.CurrentLanguage == language.korean;
        title.text = mode switch
        {
            Mode.Equip => korean ? "보유 포션" : "Owned potions",
            Mode.Recipe => korean ? "발견한 레시피" : "Discovered recipes",
            _ => korean ? "보유 재료" : "Owned ingredients"
        };
    }

    protected override int DisplayItemCount => mode == Mode.Recipe ? matchingRecipes.Count : base.DisplayItemCount;

    protected override void CollectDisplayItems()
    {
        if (mode != Mode.Recipe) { base.CollectDisplayItems(); return; }
        matchingRecipes.Clear();
        foreach (var recipe in recipes.VisibleRecipes)
            if (IsItemDataVisible(recipe.resultItem)) matchingRecipes.Add(recipe);
        if (recipes.SelectedRecipe != null && !matchingRecipes.Contains(recipes.SelectedRecipe))
            recipes.SelectRecipe(null);
    }

    protected override bool CanDisplayItem(InventoryItem item)
        => item != null && item.amount > 0 && PotionEquipmentManager.IsPotion(item.itemData) && base.CanDisplayItem(item);

    protected override void BindSlot(BaseItemSlotUI slot, int index)
    {
        if (mode == Mode.Recipe)
        {
            slot.SetItemData(matchingRecipes[index].resultItem);
            return;
        }
        base.BindSlot(slot, index);
    }

    protected override void ConfigureTooltip(ItemTooltipTrigger trigger)
    {
        base.ConfigureTooltip(trigger);
        if (tooltipUI != null) trigger.isPointer = true;
    }

    protected override void ConfigureSlot(BaseItemSlotUI slot)
    {
        if (slot is ClickableItemSlotUI clickable)
            clickable.Clicked += SelectItem;
    }

    protected override void OnSlotsRefreshed()
    {
        foreach (var slot in Slots)
            slot?.SetAmountVisible(mode != Mode.Recipe && showItemCount);
    }

    private void SelectItem(InventoryItem item)
    {
        if (item?.itemData == null) return;
        switch (mode)
        {
            case Mode.Craft:
                crafting.AddMaterial(item.itemData);
                break;
            case Mode.Equip:
                equipment.SelectInventoryPotion(item.itemData);
                break;
            case Mode.Recipe:
                recipes.SelectRecipe(matchingRecipes.Find(recipe => recipe.resultItem == item.itemData));
                break;
        }
    }
}
