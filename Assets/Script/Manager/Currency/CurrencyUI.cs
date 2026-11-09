using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

// One authored currency row. Wallet/round selection belongs to its group.
[DisallowMultipleComponent]
public sealed class CurrencyUI : MonoBehaviour, ISceneLocalizedUI
{
    public CurrencyType type;
    public CurrencyData currencyData;
    [Header("Optional Display References")]
    public GameObject textObj;
    [FormerlySerializedAs("textUI")] public TextMeshProUGUI amountText;
    public TextMeshProUGUI nameText;
    public Image icon;
    public TextMeshProUGUI requiredAmountText;
    public bool hideWhenZero = true;
    [Tooltip("Hide currencies that have never been obtained, independently of Hide When Zero.")]
    public bool hideWhenUndiscovered;

    private Scene registeredScene;
    private int amount;
    private int requiredAmount;
    private bool displaysRequiredCost;

    public void SetRequiredCostDisplay(bool enabled)
    {
        displaysRequiredCost = enabled;
        RefreshVisibility();
    }

    private void OnEnable()
    {
        registeredScene = gameObject.scene;
        SceneLanguageMediator.Register(this);
    }
    private void OnDisable() => SceneLanguageMediator.Unregister(this, registeredScene);

    public void SetAmount(int value)
    {
        amount = value;
        if (amountText != null) amountText.text = value.ToString();
        RefreshVisibility();
    }

    public void SetRequiredAmount(int value)
    {
        requiredAmount = Mathf.Max(0, value);
        if (requiredAmountText != null) requiredAmountText.text = requiredAmount.ToString();
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        if (displaysRequiredCost)
        {
            bool visible = requiredAmount > 0;
            if (textObj != null && textObj != gameObject) textObj.SetActive(visible);
            gameObject.SetActive(visible);
            return;
        }
        if (textObj != null)
            textObj.SetActive((!hideWhenUndiscovered || (CurrencyManager.instance != null && CurrencyManager.instance.IsDiscovered(type)))
                && (!hideWhenZero || amount > 0 || requiredAmount > 0 || type == CurrencyType.CurrencyMain));
    }

    public void SetCurrencyData(CurrencyData data)
    {
        currencyData = data;
        RefreshVisibility();
        RefreshPresentation();
    }

    public void RefreshPresentation()
    {
        if (isActiveAndEnabled && registeredScene != gameObject.scene)
        {
            SceneLanguageMediator.Unregister(this, registeredScene);
            registeredScene = gameObject.scene;
            SceneLanguageMediator.Register(this);
        }
        else SceneLanguageMediator.RefreshText(this);
    }

    void ISceneLocalizedUI.RefreshLanguage() => RefreshPresentation();
    void ISceneLocalizedUI.ApplyLanguage(language selected, LocalizedTextTable table)
    {
        if (currencyData == null) return;
        DataInfoUI.ApplyDefinition(nameText, icon, currencyData.GetData(type), selected, type.ToString(), false);
    }
}
