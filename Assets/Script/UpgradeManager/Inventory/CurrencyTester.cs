using Unity.VisualScripting;
using UnityEngine;

public class CurrencyTester : MonoBehaviour
{

    [SerializeField] private CurrencyType currencyType;
    [SerializeField] private int amount;

    [SerializeField] private bool isTest = true;
    private void OnGUI()
    {
        if (isTest)
        {
            if (GUILayout.Button("SetCurrencyMax"))
                SetCurrencyMax();

            if (GUILayout.Button("ResetCurrency"))
                ResetCurrency();


            if (GUILayout.Button("SetCurrencyByType"))
                SetCurrency();

        }
    }
    private void SetCurrencyMax()
    {
        foreach (CurrencyType type in System.Enum.GetValues(typeof(CurrencyType)))
        {
            CurrencyManager.instance.SetCurrency(type, 999999);
        }
    }
    private void ResetCurrency()
    {
        foreach (CurrencyType type in System.Enum.GetValues(typeof(CurrencyType)))
        {
            CurrencyManager.instance.SetCurrency(type, 0);
        }
    }
    private void SetCurrency()
    {
        CurrencyManager.instance.SetCurrency(currencyType, amount);
    }
}
