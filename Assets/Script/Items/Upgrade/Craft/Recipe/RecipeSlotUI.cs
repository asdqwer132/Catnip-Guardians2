using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeSlotUI : ItemDefinitionUI
{
    [Header("Base Item UI")]
    public TextMeshProUGUI nameText { get => dataNameText; set => dataNameText = value; }
    public GameObject amountImage;
    public TextMeshProUGUI amountText;

    [Header("Button")]
    public Button fillButton;

    [Header("Reference")]
    public ItemRecipeData currentItem;
    public ItemRecipeManager recipeManager;

    private bool isButtonBound;

    private void Awake()
    {
        BindButton();
    }

    private void OnDestroy()
    {
        UnbindButton();
    }

    public virtual void SetSlot(ItemRecipeData item, ItemRecipeManager manager)
    {
        recipeManager = manager;
        SetSlot(item);
    }

    public virtual void SetSlot(ItemRecipeData item)
    {
        BindButton();

        currentItem = item;

        if (item == null || item.resultItem == null)
        {
            ClearSlot();
            return;
        }

        SetData(item.resultItem);
        SetClassification(item.GetItemSeries(), item.GetGrade());

        if (amountImage != null)
            amountImage.SetActive(false);

        if (amountText != null)
            amountText.text = "";

    }

    public virtual void ClearSlot()
    {
        currentItem = null;

        ClearData();

        if (amountImage != null)
            amountImage.SetActive(false);

        if (amountText != null)
            amountText.text = "";

    }

    public ItemRecipeData GetCurrentItem()
    {
        return currentItem;
    }

    private void BindButton()
    {
        if (isButtonBound)
            return;

        if (fillButton == null)
            fillButton = GetComponent<Button>();

        if (fillButton == null)
            fillButton = GetComponentInChildren<Button>(true);

        if (fillButton == null)
            return;

        fillButton.onClick.RemoveListener(OnFillButtonClicked);
        fillButton.onClick.AddListener(OnFillButtonClicked);

        isButtonBound = true;
    }

    private void UnbindButton()
    {
        if (fillButton == null)
            return;

        fillButton.onClick.RemoveListener(OnFillButtonClicked);
        isButtonBound = false;
    }

    private void OnFillButtonClicked()
    {
        if (currentItem == null)
        {
            Debug.LogWarning("[RecipeSlotUI] 선택된 레시피가 없습니다.");
            return;
        }

        if (recipeManager == null)
            recipeManager = GetComponentInParent<ItemRecipeManager>();

        if (recipeManager == null)
        {
            Debug.LogWarning("[RecipeSlotUI] ItemRecipeManager가 없습니다.");
            return;
        }

        recipeManager.TryFillRecipeOne(currentItem);
    }
}