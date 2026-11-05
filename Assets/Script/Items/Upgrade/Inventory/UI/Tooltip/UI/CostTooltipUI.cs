using System;
using TMPro;
using UnityEngine;

public class CostTooltipUI : ItemTooltipUI
{
    public BoxCost[] extraBoxCosts;
    [Header("Unlock State Panels")]
    public GameObject needCurrencyPanel;
    public GameObject unlockButtonPanel;
    public GameObject unlockedPanel;
    private SkillNodeData displayedSkill;

    public void UnlockSkill()
    {
        if (!IsVisible || displayedSkill == null || SkillTreeManager.Instance == null) return;
        if (SkillTreeManager.Instance.UnlockSkill(displayedSkill) && IsVisible) Hide();
    }

    protected override void ApplyData(TooltipData data)
    {
        displayedSkill = data is CostTooltipData ? data.sourceData as SkillNodeData : null;
        base.ApplyData(data);

        if (data is not CostTooltipData costData)
            return;

        if (needCurrencyPanel != null) needCurrencyPanel.SetActive(!costData.isUnlocked);
        if (unlockButtonPanel != null) unlockButtonPanel.SetActive(!costData.isUnlocked);
        if (unlockedPanel != null) unlockedPanel.SetActive(costData.isUnlocked);

        if (extraBoxCosts != null)
        {
            foreach (var item in extraBoxCosts)
            {
                if (item != null && item.costPanel != null)
                    item.costPanel.SetActive(false);
            }
            if (costData.costs == null || costData.isUnlocked)
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
