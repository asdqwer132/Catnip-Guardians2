using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Cost
{
    public CurrencyType currencyType;
    public int amount = 1;
}

public enum CurrencyType
{
    Gold,
    Seed,
    Crystal,
    Core,
    Leaf,
    Scrap,
    EndPearl
}

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager instance;

    [Header("UI Groups")]
    public CurrencyUIGroup[] uiGroups;
    public bool autoFindUIGroupsInChildren = true;

    [Header("Start Value")]
    public int defaultStartAmount = 11110;

    private Dictionary<CurrencyType, int> currencies =
        new Dictionary<CurrencyType, int>();

    private void Awake()
    {
        instance = this;

        InitCurrencies();
        InitUIGroups();
    }

    private void Start()
    {
        UpdateAllUI();
    }

    private void InitCurrencies()
    {
        currencies.Clear();

        foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
        {

            currencies[type] = defaultStartAmount;
        }
    }

    private void InitUIGroups()
    {
        if (!autoFindUIGroupsInChildren)
            return;

        if (uiGroups != null && uiGroups.Length > 0)
            return;

        uiGroups = GetComponentsInChildren<CurrencyUIGroup>(true);
    }

    public int GetCurrency(CurrencyType type)
    {
        if (!currencies.ContainsKey(type))
            return 0;

        return currencies[type];
    }

    public void AddCurrency(Cost[] costs)
    {
        if (costs == null)
            return;

        for (int i = 0; i < costs.Length; i++)
            AddCurrency(costs[i]);
    }

    public void AddCurrency(Cost cost)
    {
        if (cost == null)
            return;

        AddCurrency(cost.currencyType, cost.amount);
    }

    public void AddCurrency(CurrencyType type, int amount)
    {
        if (amount <= 0)
            return;


        if (!currencies.ContainsKey(type))
            currencies[type] = 0;

        currencies[type] += amount;

        UpdateUI(type);
    }

    public bool HasCurrency(CurrencyType type, int amount)
    {
        if (amount <= 0)
            return true;

        if (!currencies.ContainsKey(type))
            return false;

        return currencies[type] >= amount;
    }

    public bool SpendCurrency(CurrencyType type, int amount)
    {
        if (amount <= 0)
            return true;

        if (!HasCurrency(type, amount))
            return false;

        currencies[type] -= amount;

        UpdateUI(type);

        return true;
    }

    public bool HasCurrencies(List<Cost> costs)
    {
        if (costs == null)
            return true;

        for (int i = 0; i < costs.Count; i++)
        {
            Cost cost = costs[i];

            if (cost == null)
                continue;

            if (cost.amount <= 0)
                continue;

            if (!HasCurrency(cost.currencyType, cost.amount))
                return false;
        }

        return true;
    }

    public bool HasCurrencies(Cost[] costs)
    {
        if (costs == null)
            return true;

        for (int i = 0; i < costs.Length; i++)
        {
            Cost cost = costs[i];

            if (cost == null)
                continue;

            if (cost.amount <= 0)
                continue;

            if (!HasCurrency(cost.currencyType, cost.amount))
                return false;
        }

        return true;
    }

    public bool SpendCurrencies(List<Cost> costs)
    {
        if (!HasCurrencies(costs))
            return false;

        for (int i = 0; i < costs.Count; i++)
        {
            Cost cost = costs[i];

            if (cost == null)
                continue;

            if (cost.amount <= 0)
                continue;

            SpendCurrency(cost.currencyType, cost.amount);
        }

        return true;
    }

    public bool SpendCurrencies(Cost[] costs)
    {
        if (!HasCurrencies(costs))
            return false;

        for (int i = 0; i < costs.Length; i++)
        {
            Cost cost = costs[i];

            if (cost == null)
                continue;

            if (cost.amount <= 0)
                continue;

            SpendCurrency(cost.currencyType, cost.amount);
        }

        return true;
    }

    private void UpdateUI(CurrencyType type)
    {
        if (!currencies.ContainsKey(type))
            return;

        int amount = currencies[type];

        if (uiGroups == null)
            return;

        for (int i = 0; i < uiGroups.Length; i++)
        {
            if (uiGroups[i] == null)
                continue;

            uiGroups[i].UpdateUI(type, amount);
        }
    }

    private void UpdateAllUI()
    {
        foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
        {
            UpdateUI(type);
        }
    }
}