using System;
using UnityEngine;

public enum CurrencyDisplaySource { Wallet, CurrentGame }

// Selects the amount source and forwards values to authored CurrencyUI components.
public class CurrencyUIGroup : MonoBehaviour
{
    [Header("Currency UIs")]
    public CurrencyUI[] currencyUIs;
    public CurrencyData currencyData;
    [Tooltip("Wallet: total owned currency / Current Game: currency earned in this game")]
    public CurrencyDisplaySource source;
    public bool DisplaysWallet => source == CurrencyDisplaySource.Wallet;

    private CurrencyManager boundManager;
    private GameStatisticsManager boundStatistics;

    private void OnEnable()
    {
        RefreshPresentation();
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

    public void RefreshPresentation()
    {
        if (currencyUIs == null) return;
        foreach (var ui in currencyUIs)
            if (ui != null) ui.SetCurrencyData(currencyData);
    }

    private void RefreshRoundUI()
    {
        foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
            UpdateUI(type, boundStatistics != null ? boundStatistics.GetCurrency(type) : 0);
    }

    public void UpdateUI(CurrencyType type, int amount)
    {
        if (currencyUIs == null) return;
        foreach (var ui in currencyUIs)
        {
            if (ui == null || ui.type != type) continue;
            ui.SetCurrencyData(currencyData);
            ui.SetAmount(amount);
        }
    }
}
