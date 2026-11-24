using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Cost
{
    public CurrencyType currencyType;
    public int amount = 1;

    public Cost(CurrencyType currencyType, int amount)
    {
        this.currencyType = currencyType;
        this.amount = amount;
    }
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
    public static event Action DiscoveryChanged;

    [Header("UI Groups")]
    public CurrencyUIGroup[] uiGroups;
    public bool autoFindUIGroupsInChildren = true;

    public const string SaveKey = "CurrencyWallet";

    [Header("Initial Amounts (used only when no saved balance exists)")]
    public Cost[] initialAmounts;

    [Serializable]
    private sealed class WalletSave
    {
        public List<Cost> balances = new List<Cost>();
        public List<CurrencyType> discovered = new List<CurrencyType>();
    }

    private Dictionary<CurrencyType, int> currencies =
        new Dictionary<CurrencyType, int>();
    private readonly HashSet<CurrencyType> discovered = new HashSet<CurrencyType>();
    public bool IsDiscovered(CurrencyType type) => discovered.Contains(type);

    public void MarkDiscovered(CurrencyType type)
    {
        if (!discovered.Add(type)) return;
        SaveCurrencies();
        DiscoveryChanged?.Invoke();
    }

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
        discovered.Clear();

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
                if (saved?.discovered != null) discovered.UnionWith(saved.discovered);
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

        foreach (var entry in currencies)
            if (entry.Value > 0) discovered.Add(entry.Key);
        SaveCurrencies();
        DiscoveryChanged?.Invoke();
    }

    public void ResetToInitialAmounts()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        InitCurrencies();
        UpdateAllUI();
    }

    private void SaveCurrencies()
    {
        var saved = new WalletSave();
        saved.discovered.AddRange(discovered);
        foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
            saved.balances.Add(new Cost (type, GetCurrency(type)));
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
        bool firstDiscovery = discovered.Add(type);
        SaveCurrencies();

        if (firstDiscovery) DiscoveryChanged?.Invoke();
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
    public void SetCurrency(CurrencyType type, int amount)
    {
        if (!currencies.ContainsKey(type))
            return;

        currencies[type] = Mathf.Max(0, amount);

        bool firstDiscovery = amount > 0 && discovered.Add(type);

        SaveCurrencies();

        if (firstDiscovery)
            DiscoveryChanged?.Invoke();

        UpdateUI(type);
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
