using System;
using TMPro;
using UnityEngine;

public class CostTooltipUI : ItemTooltipUI
{
    public BoxCost[] extraBoxCosts;
    protected override void ApplyData(TooltipData data)
    {
        base.ApplyData(data);

        if (data is not CostTooltipData costData)
            return;

        if (extraBoxCosts != null)
        {
            foreach (var item in extraBoxCosts)
            {
                if (item != null && item.costPanel != null)
                    item.costPanel.SetActive(false);
            }
            if (costData.costs == null)
                return;

            foreach (var cost in costData.costs)
            {
                if (cost == null)
                    continue;
                BoxCost boxCost = GetBoxCost(cost.currencyType);
                if (boxCost == null)
                    continue;
                if (boxCost.costText != null)
                    boxCost.costText.text = cost.amount.ToString();
                if (boxCost.costPanel != null)
                    boxCost.costPanel.SetActive(true);
            }
        }


    }
    private BoxCost GetBoxCost(CurrencyType currencyType)
    {
        if (extraBoxCosts == null)
            return null;

        return Array.Find(
            extraBoxCosts,
            boxCost => boxCost != null &&
                       boxCost.currencyType == currencyType
        );
    }
}
