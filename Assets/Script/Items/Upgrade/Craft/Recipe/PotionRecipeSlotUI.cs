using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class PotionRecipeSlotUI : ItemDefinitionUI
{
    public Toggle selectButton;
    [SerializeField] private GameObject pannel; //
    public ItemRecipeData Recipe { get; private set; }
    private Action<ItemRecipeData> selected;
    private void Awake() { if (selectButton != null) selectButton.onValueChanged.AddListener(Select); }
    private void OnDestroy() { if (selectButton != null) selectButton.onValueChanged.RemoveListener(Select); }
    public void Bind(ItemRecipeData recipe, Action<ItemRecipeData> onSelected, GameObject pannel)
    {
        Recipe = recipe; selected = onSelected;
        SetData(recipe != null ? recipe.resultItem : null);
        this.pannel = pannel;
    }
    private void Select(bool isOn)
    {
        if (Recipe != null) selected?.Invoke(Recipe);
        pannel?.SetActive(isOn);
    }

}
