using System;
using TMPro;
using UnityEngine;

public enum CurrencyDisplaySource { Wallet, CurrentGame }

public class CurrencyUIGroup : MonoBehaviour
{
    [Serializable]
    public class CurrencyUI
    {
        public CurrencyType type;
        public GameObject textObj;
        public TextMeshProUGUI textUI;
    }

    [Header("Currency UIs")]
    public CurrencyUI[] currencyUIs;
    [Tooltip("Wallet: 전체 보유 재화 / Current Game: 이번 게임에서 획득한 재화")]
    public CurrencyDisplaySource source;
    public bool DisplaysWallet => source == CurrencyDisplaySource.Wallet;

    private CurrencyManager boundManager;
    private GameStatisticsManager boundStatistics;
    private void OnEnable()
    {
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
        CurrencyManager.InstanceChanged -= BindManager;
        GameStatisticsManager.InstanceChanged -= BindManager;
        if (boundManager != null) boundManager.UnregisterUI(this);
        if (boundStatistics != null) boundStatistics.OnChanged -= RefreshRoundUI;
        boundManager = null;
        boundStatistics = null;
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

            if (ui.textUI != null)
                ui.textUI.text = amount.ToString();

            if (ui.textObj != null)
                ui.textObj.SetActive(amount > 0 || type == CurrencyType.CurrencyMain);
        }
    }
}
