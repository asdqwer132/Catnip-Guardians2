using System;
using System.Collections.Generic;
using UnityEngine;

public class GameStatisticsManager : MonoBehaviour
{
    public static GameStatisticsManager Instance { get; private set; }

    public StatisticsUI ui;

    [Header("Round Statistics")]
    [SerializeField] private int roundKillCount;

    [Header("Round Currency")]
    [SerializeField] private List<RoundCurrencyInfo> roundCurrencies = new List<RoundCurrencyInfo>();

    public int RoundKillCount => roundKillCount;
    public int RoundGold => GetCurrency(CurrencyType.Gold);
    public IReadOnlyList<RoundCurrencyInfo> RoundCurrencies => roundCurrencies;

    public event Action OnChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitCurrencies();
    }
    private void OnEnable()
    {
        OnChanged += RefreshUI;
    }

    private void OnDisable()
    {
        OnChanged -= RefreshUI;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        InitCurrencies();
    }
#endif
    private void Start()
    {
        ui.RefreshUI();
    }
    public void Confirm()
    {
        foreach (var currency in roundCurrencies)
        {
            CurrencyManager.instance.AddCurrency(currency.currencyType, currency.amount);
        }
    }
    public void ResetRound()
    {
        roundKillCount = 0;

        InitCurrencies();

        for (int i = 0; i < roundCurrencies.Count; i++)
        {
            roundCurrencies[i].amount = 0;
        }

        OnChanged?.Invoke();
    }
    private void RefreshUI() => ui.RefreshUI(); 
    public void AddKill(int amount = 1)
    {
        if (amount <= 0)
            return;

        roundKillCount += amount;

        OnChanged?.Invoke();
    }

    public void AddGold(int amount)
    {
        AddCurrency(CurrencyType.Gold, amount);
    }

    public void AddCurrency(CurrencyType currencyType, int amount)
    {
        if (amount <= 0)
            return;

        RoundCurrencyInfo currency = GetCurrencyInfo(currencyType);

        if (currency == null)
        {
            currency = new RoundCurrencyInfo(currencyType, 0);
            roundCurrencies.Add(currency);
        }

        currency.amount += amount;

        OnChanged?.Invoke();
    }

    public int GetCurrency(CurrencyType currencyType)
    {
        RoundCurrencyInfo currency = GetCurrencyInfo(currencyType);

        if (currency == null)
            return 0;

        return currency.amount;
    }

    private RoundCurrencyInfo GetCurrencyInfo(CurrencyType currencyType)
    {
        for (int i = 0; i < roundCurrencies.Count; i++)
        {
            if (roundCurrencies[i].currencyType == currencyType)
                return roundCurrencies[i];
        }

        return null;
    }

    private void InitCurrencies()
    {
        Array values = Enum.GetValues(typeof(CurrencyType));

        for (int i = 0; i < values.Length; i++)
        {
            CurrencyType currencyType = (CurrencyType)values.GetValue(i);

            if (GetCurrencyInfo(currencyType) == null)
            {
                roundCurrencies.Add(new RoundCurrencyInfo(currencyType, 0));
            }
        }

        SortCurrencies();
    }

    private void SortCurrencies()
    {
        roundCurrencies.Sort((a, b) => a.currencyType.CompareTo(b.currencyType));
    }
}

[Serializable]
public class RoundCurrencyInfo
{
    public CurrencyType currencyType;
    public int amount;

    public RoundCurrencyInfo(CurrencyType currencyType, int amount)
    {
        this.currencyType = currencyType;
        this.amount = amount;
    }
}