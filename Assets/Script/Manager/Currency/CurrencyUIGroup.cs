using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum CurrencyDisplaySource { Wallet, CurrentGame }

public class CurrencyUIGroup : MonoBehaviour, ISceneLocalizedUI
{
    [Header("Currency UIs")]
    public CurrencyUI[] currencyUIs;
    [Tooltip("재화 타입별 이름·아이콘을 모은 공용 에셋입니다.")]
    public CurrencyData currencyData;
    [Tooltip("Wallet: 전체 보유 재화 / Current Game: 이번 게임에서 획득한 재화")]
    public CurrencyDisplaySource source;
    public bool DisplaysWallet => source == CurrencyDisplaySource.Wallet;

    [Serializable]
    public class CurrencyUI
    {
        public CurrencyType type;
        public GameObject textObj;
        [FormerlySerializedAs("textUI")]
        public TextMeshProUGUI amountText;
        [Tooltip("선택 사항: 비우면 이름을 표시하지 않습니다.")]
        public TextMeshProUGUI nameText;
        [Tooltip("선택 사항: 비우면 아이콘을 갱신하지 않습니다.")]
        public Image icon;
    }

    private Scene registeredScene;
    private language displayLanguage = language.english;
    private CurrencyManager boundManager;
    private GameStatisticsManager boundStatistics;
    private void OnEnable()
    {
        registeredScene = gameObject.scene;
        SceneLanguageMediator.Register(this);
        CurrencyManager.InstanceChanged += BindManager;
        GameStatisticsManager.InstanceChanged += BindManager;
        BindManager();
    }
    private void BindManager()
    {
        if (boundManager != null) boundManager.UnregisterUI(this);
        if (boundStatistics != null) boundStatistics.OnChanged -= RefreshRoundUI;
        boundManager = DisplaysWallet ? CurrencyManager.instance : null;
        boundStatistics = DisplaysWallet ? null : GameStatisticsManager.Instance;
        if (boundManager != null) boundManager.RegisterUI(this);
        if (boundStatistics != null) boundStatistics.OnChanged += RefreshRoundUI;
        if (!DisplaysWallet) RefreshRoundUI();
    }
    private void OnDisable()
    {
        SceneLanguageMediator.Unregister(this, registeredScene);
        CurrencyManager.InstanceChanged -= BindManager;
        GameStatisticsManager.InstanceChanged -= BindManager;
        if (boundManager != null) boundManager.UnregisterUI(this);
        if (boundStatistics != null) boundStatistics.OnChanged -= RefreshRoundUI;
        boundManager = null;
        boundStatistics = null;
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
        displayLanguage = selected;
        if (currencyUIs == null) return;
        foreach (var ui in currencyUIs) ApplyPresentation(ui);
    }

    private void ApplyPresentation(CurrencyUI ui)
    {
        if (ui == null || currencyData == null) return;
        DefinedData data = currencyData.GetData(ui.type);
        if (ui.nameText != null)
        {
            ui.nameText.text = DefinedDataUtility.GetName(data, displayLanguage, ui.type.ToString());
            LocalizedText.BindDefinedData(ui.nameText, data, ui.type.ToString());
        }
        if (ui.icon != null)
        {
            ui.icon.sprite = data?.Icon;
            ui.icon.enabled = ui.icon.sprite != null;
        }
    }

    private void RefreshRoundUI()
    {
        foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
            UpdateUI(type, boundStatistics != null ? boundStatistics.GetCurrency(type) : 0);
    }

    public void UpdateUI(CurrencyType type, int amount)
    {
        if (currencyUIs == null)
            return;

        for (int i = 0; i < currencyUIs.Length; i++)
        {
            CurrencyUI ui = currencyUIs[i];

            if (ui == null)
                continue;

            if (ui.type != type)
                continue;

            ApplyPresentation(ui);

            if (ui.amountText != null)
                ui.amountText.text = amount.ToString();

            if (ui.textObj != null)
                ui.textObj.SetActive(amount > 0 || type == CurrencyType.CurrencyMain);
        }
    }
}
