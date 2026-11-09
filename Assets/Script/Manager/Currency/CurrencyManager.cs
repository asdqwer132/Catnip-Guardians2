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
    CurrencyMain = 0,
    CurrencySub1 = 1,
    CurrencySub2 = 2,
    CurrencySub3 = 3,
    CurrencySub4 = 4,
    CurrencySub5 = 5,
    CurrencySub6 = 6,
    CurrencySub7 = 7,
    CurrencySub8 = 8,
}

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager instance;
    public static event Action InstanceChanged;

    [Header("UI Groups")]
    public CurrencyUIGroup[] uiGroups;
    public bool autoFindUIGroupsInChildren = true;

    public const string SaveKey = "CurrencyWallet";

    [Header("Initial Amounts (used only when no saved balance exists)")]
    public Cost[] initialAmounts =
    {
        new Cost { currencyType = CurrencyType.CurrencyMain, amount = 11110 },
        new Cost { currencyType = CurrencyType.CurrencySub1, amount = 11110 },
        new Cost { currencyType = CurrencyType.CurrencySub2, amount = 11110 },
        new Cost { currencyType = CurrencyType.CurrencySub3, amount = 11110 },
        new Cost { currencyType = CurrencyType.CurrencySub4, amount = 11110 },
        new Cost { currencyType = CurrencyType.CurrencySub5, amount = 11110 },
        new Cost { currencyType = CurrencyType.CurrencySub6, amount = 11110 },
        new Cost { currencyType = CurrencyType.CurrencySub7, amount = 11110 },
        new Cost { currencyType = CurrencyType.CurrencySub8, amount = 11110 }
    };

    [Serializable]
    private sealed class WalletSave
    {
        public List<Cost> balances = new List<Cost>();
    }

    private Dictionary<CurrencyType, int> currencies =
        new Dictionary<CurrencyType, int>();
    private readonly HashSet<CurrencyUIGroup> registeredGroups = new HashSet<CurrencyUIGroup>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        InitCurrencies();
        InitUIGroups();
        InstanceChanged?.Invoke();
    }

    private void Start()
    {
        UpdateAllUI();
    }

    public void RegisterUI(CurrencyUIGroup group)
    {
        if (group == null || !group.DisplaysWallet || !registeredGroups.Add(group)) return;
        foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType))) group.UpdateUI(type, GetCurrency(type));
    }
    public void UnregisterUI(CurrencyUIGroup group) => registeredGroups.Remove(group);
    private void OnDestroy()
    {
        if (instance != this) return;
        instance = null;
        InstanceChanged?.Invoke();
    }

    private void InitCurrencies()
    {
        currencies.Clear();

        foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
            currencies[type] = 0;

        if (initialAmounts != null)
            foreach (var entry in initialAmounts)
                if (entry != null && currencies.ContainsKey(entry.currencyType))
                    currencies[entry.currencyType] = Mathf.Max(0, entry.amount);

        // Apply saved entries after defaults: zero is a saved value too.
        // New currency types without an entry keep their configured initial amount.
        if (PlayerPrefs.HasKey(SaveKey))
        {
            try
            {
                var saved = JsonUtility.FromJson<WalletSave>(PlayerPrefs.GetString(SaveKey));
                if (saved?.balances != null)
                    foreach (var entry in saved.balances)
                        if (entry != null && currencies.ContainsKey(entry.currencyType))
                            currencies[entry.currencyType] = Mathf.Max(0, entry.amount);
            }
            catch (ArgumentException)
            {
                Debug.LogWarning("Currency save is invalid. Using configured initial amounts.", this);
            }
        }

        SaveCurrencies();
    }

    private void SaveCurrencies()
    {
        var saved = new WalletSave();
        foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
            saved.balances.Add(new Cost { currencyType = type, amount = GetCurrency(type) });
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(saved));
        PlayerPrefs.Save();
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
        SaveCurrencies();

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
        SaveCurrencies();

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

        foreach (var group in registeredGroups)
            if (group != null && group.DisplaysWallet) group.UpdateUI(type, amount);
        for (int i = 0; uiGroups != null && i < uiGroups.Length; i++)
        {
            if (uiGroups[i] == null || !uiGroups[i].DisplaysWallet || registeredGroups.Contains(uiGroups[i]))
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
