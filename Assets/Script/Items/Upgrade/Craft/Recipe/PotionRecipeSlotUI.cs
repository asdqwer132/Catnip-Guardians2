using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class PotionRecipeSlotUI : ItemDefinitionUI
{
    public Button selectButton;
    public ItemRecipeData Recipe { get; private set; }
    private Action<ItemRecipeData> selected;
    private void Awake() { if (selectButton != null) selectButton.onClick.AddListener(Select); }
    private void OnDestroy() { if (selectButton != null) selectButton.onClick.RemoveListener(Select); }
    public void Bind(ItemRecipeData recipe, Action<ItemRecipeData> onSelected)
    {
        Recipe = recipe; selected = onSelected;
        SetData(recipe != null ? recipe.resultItem : null);
    }
    private void Select() { if (Recipe != null) selected?.Invoke(Recipe); }
}
