using System;
using TMPro;
using UnityEngine;

public class CostTooltipUI : ItemTooltipUI
{
    public BoxCost[] extraBoxCosts;
    [Header("Unlock State Panels")]
    public GameObject needCurrencyPanel;
    public GameObject unlockButtonPanel;
    public GameObject lockedStatusPanel;
    public GameObject unlockedPanel;
    private SkillNodeData displayedSkill;
    private UnityEngine.UI.Button boundUnlockButton;

    public void UnlockSkill()
    {
        Debug.Log($"[SkillPurchase] Confirm clicked: node={displayedSkill?.dataId ?? "none"}, visible={IsVisible}", this);
        if (!IsVisible || displayedSkill == null || SkillTreeManager.Instance == null)
        { Debug.LogWarning("[SkillPurchase] Rejected: no displayed skill, hidden tooltip, or missing SkillTreeManager.", this); return; }
        if (SkillTreeManager.Instance.UnlockSkill(displayedSkill) && IsVisible) Hide();
    }

    protected override void ApplyData(TooltipData data)
    {
        displayedSkill = data is CostTooltipData ? data.sourceData as SkillNodeData : null;
        base.ApplyData(data);
        if (unlockButtonPanel != null)
        {
            var button = unlockButtonPanel.GetComponentInChildren<UnityEngine.UI.Button>(true);
            if (boundUnlockButton != null) boundUnlockButton.onClick.RemoveListener(UnlockSkill);
            boundUnlockButton = button;
            if (button != null)
            {
                bool persistent = false;
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                    if (button.onClick.GetPersistentTarget(i) == this && button.onClick.GetPersistentMethodName(i) == nameof(UnlockSkill)) persistent = true;
                if (!persistent) button.onClick.AddListener(UnlockSkill);
            }
        }

        if (data is not CostTooltipData costData)
            return;

        if (boundUnlockButton != null) boundUnlockButton.interactable = displayedSkill != null && !costData.isUnlocked;
        if (needCurrencyPanel != null) needCurrencyPanel.SetActive(!costData.isUnlocked);
        if (unlockButtonPanel != null) unlockButtonPanel.SetActive(!costData.isUnlocked);
        if (lockedStatusPanel != null) lockedStatusPanel.SetActive(!costData.isUnlocked);
        if (unlockedPanel != null) unlockedPanel.SetActive(costData.isUnlocked);

        if (needCurrencyPanel != null)
        {
            var groups = needCurrencyPanel.GetComponentsInChildren<CurrencyUIGroup>(true);
            if (groups.Length == 0 && needCurrencyPanel.GetComponentInChildren<CurrencyUI>(true) != null)
                groups = new[] { needCurrencyPanel.AddComponent<CurrencyUIGroup>() };
            foreach (var group in groups) group.SetRequiredCosts(costData.costs);
        }

        if (extraBoxCosts != null)
        {
            foreach (var item in extraBoxCosts)
            {
                if (item != null && item.costPanel != null)
                {
                    Debug.Log(item.costPanel.name);
                    item.costPanel.SetActive(false);
                }
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
