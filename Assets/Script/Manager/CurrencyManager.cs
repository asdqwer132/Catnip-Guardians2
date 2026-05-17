using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

    void Awake()
    {
        instance = this;

        foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
        {
            //시작 자원 값
            currencies[type] = 11110;

            uiDictionary[type] = new List<TextMeshProUGUI>();
            uiObjDictionary[type] = new List<GameObject>();
        }

        foreach (var ui in currencyUIs)
        {
            if (ui == null)
                continue;


            if (!uiDictionary.ContainsKey(ui.type))
            {
                uiDictionary[ui.type] = new List<TextMeshProUGUI>();
                uiObjDictionary[ui.type] = new List<GameObject>();
            }

            if (ui.textUI != null)
                uiDictionary[ui.type].Add(ui.textUI);

            if (ui.textObj != null)
                uiObjDictionary[ui.type].Add(ui.textObj);
        }
    }

    void Start()
    {
        UpdateAllUI();
    }

    public int GetCurrency(CurrencyType type)
    {
        if (!currencies.ContainsKey(type))
            return 0;

        return currencies[type];
    }

    public void AddCurrency(CurrencyType type, int amount)
    {
        if (!currencies.ContainsKey(type))
            currencies[type] = 0;

        currencies[type] += amount;
        UpdateUI(type);
    }

    public bool SpendCurrency(CurrencyType type, int amount)
    {
        if (!currencies.ContainsKey(type))
            return false;

        if (currencies[type] < amount)
            return false;

        currencies[type] -= amount;
        UpdateUI(type);

        return true;
    }

    public bool HasCurrency(CurrencyType type, int amount)
    {
        if (!currencies.ContainsKey(type))
            return false;

        return currencies[type] >= amount;
    }

    void UpdateUI(CurrencyType type)
    {
        if (!currencies.ContainsKey(type))
            return;

        if (uiDictionary.ContainsKey(type))
        {
            foreach (var textUI in uiDictionary[type])
            {
                if (textUI == null)
                    continue;

                textUI.text = "" + currencies[type];
            }
        }

        if (uiObjDictionary.ContainsKey(type))
        {
            foreach (var textObj in uiObjDictionary[type])
            {
                if (textObj == null)
                    continue;

                textObj.SetActive(currencies[type] > 0 || type == CurrencyType.Gold);
            }
        }
    }

    void UpdateAllUI()
    {
        foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
        {
            UpdateUI(type);
        }
    }
}