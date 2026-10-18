using System;
using TMPro;
using UnityEngine;

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

    private CurrencyManager boundManager;
    private void OnEnable()
    {
        CurrencyManager.InstanceChanged += BindManager;
        BindManager();
    }
    private void BindManager()
    {
        if (boundManager != null) boundManager.UnregisterUI(this);
        boundManager = CurrencyManager.instance;
        if (boundManager != null) boundManager.RegisterUI(this);
    }
    private void OnDisable()
    {
        CurrencyManager.InstanceChanged -= BindManager;
        if (boundManager != null) boundManager.UnregisterUI(this);
        boundManager = null;
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
                ui.textObj.SetActive(amount > 0 || type == CurrencyType.Gold);
        }
    }
}
