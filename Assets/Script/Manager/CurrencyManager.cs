using System;
using System.Collections.Generic;
using TMPro;
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
    End
}

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager instance;

    [Header("UI")]
    public CurrencyUI[] currencyUIs;

    [Header("Start Value")]
    public int defaultStartAmount = 11110;

    [Serializable]
    public class CurrencyUI
    {
        public CurrencyType type;
        public GameObject textObj;
        public TextMeshProUGUI textUI;
    }

    private Dictionary<CurrencyType, int> currencies =
        new Dictionary<CurrencyType, int>();

    private Dictionary<CurrencyType, List<GameObject>> uiObjDictionary =
        new Dictionary<CurrencyType, List<GameObject>>();

    private Dictionary<CurrencyType, List<TextMeshProUGUI>> uiDictionary =
        new Dictionary<CurrencyType, List<TextMeshProUGUI>>();

    private void Awake()
    {
        instance = this;

        InitCurrencies();
        InitCurrencyUIs();
    }

    private void Start()
    {
        UpdateAllUI();
    }

    private void InitCurrencies()
    {
        currencies.Clear();
        uiDictionary.Clear();
        uiObjDictionary.Clear();

        foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
        {

            currencies[type] = defaultStartAmount;
            uiDictionary[type] = new List<TextMeshProUGUI>();
            uiObjDictionary[type] = new List<GameObject>();
        }
    }

    private void InitCurrencyUIs()
    {
        if (currencyUIs == null)
            return;

        foreach (var ui in currencyUIs)
        {
            if (ui == null)
                continue;

            if (!uiDictionary.ContainsKey(ui.type))
                uiDictionary[ui.type] = new List<TextMeshProUGUI>();

            if (!uiObjDictionary.ContainsKey(ui.type))
                uiObjDictionary[ui.type] = new List<GameObject>();

            if (ui.textUI != null)
                uiDictionary[ui.type].Add(ui.textUI);

            if (ui.textObj != null)
                uiObjDictionary[ui.type].Add(ui.textObj);
        }
    }

    public int GetCurrency(CurrencyType type)
    {
        if (!currencies.ContainsKey(type))
            return 0;

        return currencies[type];
    }

    public void AddCurrency(Cost[] cost)
    {
        foreach(var c in cost)
        {
            AddCurrency(c);
        }
    }
    public void AddCurrency(Cost cost)
    {

        if (cost.amount <= 0)
            return;

        if (!currencies.ContainsKey(cost.currencyType))
            currencies[cost.currencyType] = 0;

        currencies[cost.currencyType] += cost.amount;

        UpdateUI(cost.currencyType);
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

        if (uiDictionary.ContainsKey(type))
        {
            foreach (var textUI in uiDictionary[type])
            {
                if (textUI == null)
                    continue;

                textUI.text = amount.ToString();
            }
        }

        if (uiObjDictionary.ContainsKey(type))
        {
            foreach (var textObj in uiObjDictionary[type])
            {
                if (textObj == null)
                    continue;

                textObj.SetActive(amount > 0 || type == CurrencyType.Gold);
            }
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