using UnityEngine;

public class StatisticsUI : MonoBehaviour
{
    public GameStatisticsManager statisticsManager;
    public CurrencyUIGroup[] currencyUIGroup;

    public void RefreshUI()
    {
        foreach (var item in currencyUIGroup)
        {
            foreach (var roundInfo in statisticsManager.RoundCurrencies)
            {
                item.UpdateUI(roundInfo.currencyType, roundInfo.amount);
            }
        }
    }
}
