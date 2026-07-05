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
                item.costPanel.SetActive(false);
            }
            foreach (var cost in costData.costs)
            {
                BoxCost boxCost = GetBoxCost(cost.currencyType);
                boxCost.costText.text = cost.amount.ToString();
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
