using UnityEngine;

public class StatisticsUI : MonoBehaviour
{
    public GameStatisticsManager statisticsManager;
    public CurrencyUIGroup[] currencyUIGroup;

    public void RefreshUI()
    {
        if (statisticsManager == null || currencyUIGroup == null) return;
        foreach (var item in currencyUIGroup)
        {
            if (item == null) continue;
            foreach (var roundInfo in statisticsManager.RoundCurrencies)
            {
                item.UpdateUI(roundInfo.currencyType, roundInfo.amount);
            }
        }
    }
}
