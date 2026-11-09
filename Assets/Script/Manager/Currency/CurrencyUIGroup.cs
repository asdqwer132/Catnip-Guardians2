using System;
using UnityEngine;

public enum CurrencyDisplaySource { Wallet, CurrentGame, RequiredCost }

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
        CurrencyManager.DiscoveryChanged += RefreshPresentation;
        GameStatisticsManager.InstanceChanged += BindManager;
        BindManager();
    }

    private void BindManager()
    {
        if (boundManager != null) boundManager.UnregisterUI(this);
        if (boundStatistics != null) boundStatistics.OnChanged -= RefreshRoundUI;
        boundManager = DisplaysWallet ? CurrencyManager.instance : null;
        boundStatistics = source == CurrencyDisplaySource.CurrentGame ? GameStatisticsManager.Instance : null;
        if (boundManager != null) boundManager.RegisterUI(this);
        if (boundStatistics != null) boundStatistics.OnChanged += RefreshRoundUI;
        if (source == CurrencyDisplaySource.CurrentGame) RefreshRoundUI();
    }

    private void OnDisable()
    {
        CurrencyManager.InstanceChanged -= BindManager;
        CurrencyManager.DiscoveryChanged -= RefreshPresentation;
        GameStatisticsManager.InstanceChanged -= BindManager;
        if (boundManager != null) boundManager.UnregisterUI(this);
        if (boundStatistics != null) boundStatistics.OnChanged -= RefreshRoundUI;
        boundManager = null;
        boundStatistics = null;
    }

    private void EnsureRows()
    {
        bool valid = false;
        if (currencyUIs != null) foreach (var row in currencyUIs) if (row != null) { valid = true; break; }
        if (!valid) currencyUIs = GetComponentsInChildren<CurrencyUI>(true);
    }

    public void RefreshPresentation()
    {
        EnsureRows();
        if (currencyUIs == null) return;
        foreach (var ui in currencyUIs)
            if (ui != null) ui.SetCurrencyData(currencyData != null ? currencyData : ui.currencyData);
    }

    private void RefreshRoundUI()
    {
        foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
            UpdateUI(type, boundStatistics != null ? boundStatistics.GetCurrency(type) : 0);
    }

    public void SetRequiredCosts(Cost[] costs)
    {
        EnsureRows();
        source = CurrencyDisplaySource.RequiredCost;
        BindManager();
        if (currencyUIs == null) return;
        foreach (var ui in currencyUIs)
        {
            if (ui == null) continue;
            long required = 0;
            if (costs != null) foreach (var cost in costs)
                if (cost != null && cost.currencyType == ui.type) required += Math.Max(0, cost.amount);
            int amount = (int)Math.Min(int.MaxValue, required);
            ui.SetCurrencyData(currencyData != null ? currencyData : ui.currencyData);
            ui.SetRequiredAmount(amount);
            ui.SetAmount(ui.requiredAmountText != null && CurrencyManager.instance != null ? CurrencyManager.instance.GetCurrency(ui.type) : amount);
            ui.gameObject.SetActive(amount > 0);
        }
    }

    public void UpdateUI(CurrencyType type, int amount)
    {
        if (currencyUIs == null) return;
        foreach (var ui in currencyUIs)
        {
            if (ui == null || ui.type != type) continue;
            ui.SetCurrencyData(currencyData != null ? currencyData : ui.currencyData);
            ui.SetAmount(amount);
        }
    }
}
