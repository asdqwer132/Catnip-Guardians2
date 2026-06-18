using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeSlotUI : MonoBehaviour
{
    [Header("Base Item UI")]
    public Image icon;
    public TextMeshProUGUI nameText;
    public GameObject amountImage;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI gradeText;

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

        if (item == null)
        {
            ClearSlot();
            return;
        }

        if (icon != null)
        {
            icon.sprite = item.Icon;
            icon.enabled = item.Icon != null;
        }

        if (nameText != null)
            nameText.text = item.GetDataName();

        if (amountImage != null)
            amountImage.SetActive(false);

        if (amountText != null)
            amountText.text = "";

        if (gradeText != null)
            gradeText.text = item.GetGrade().ToString();
    }

    public virtual void ClearSlot()
    {
        currentItem = null;

        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
        }

        if (nameText != null)
            nameText.text = "";

        if (amountImage != null)
            amountImage.SetActive(false);

        if (amountText != null)
            amountText.text = "";

        if (gradeText != null)
            gradeText.text = "";
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