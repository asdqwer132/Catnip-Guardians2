using UnityEngine;

public class SkillNode : MonoBehaviour
{
    [Header("Manager")]
    public SkillNodeManager manager;
    private ISkillNode[] buffers;

    [Header("Node Info")]
    public string skillName;

    [Header("Cost")]
    public SkillCost[] costs;

    [Header("Required Nodes")]
    public SkillNode[] requiredNodes;

    [Header("UI")]
    public SkillNodeUI ui;

    private bool isUnlocked = false;

    void Start()
    {
        if (ui != null)
            ui.Init(this);

        buffers = GetComponents<ISkillNode>();

        UpdateUI();
    }

    public void TryUnlock()
    {
        if (isUnlocked)
            return;

        if (!CanUnlock())
            return;

        SpendAllCosts();

        Unlock();
    }

    public bool CanUnlock()
    {
        if (isUnlocked)
            return false;

        if (!HasAllRequiredNodes())
        {
            Debug.Log("이전 스킬들을 먼저 해금해야 함");
            return false;
        }

        if (!HasAllCosts())
        {
            Debug.Log(skillName + " 해금에 필요한 자원이 부족합니다.");
            return false;
        }

        return true;
    }

    bool HasAllRequiredNodes()
    {
        if (requiredNodes == null || requiredNodes.Length == 0)
            return true;

        foreach (SkillNode node in requiredNodes)
        {
            if (node == null)
                continue;

            if (!node.IsUnlocked())
                return false;
        }

        return true;
    }

    bool HasAllCosts()
    {
        if (costs == null || costs.Length == 0)
            return true;

        foreach (SkillCost cost in costs)
        {
            if (cost == null)
                continue;

            if (!CurrencyManager.instance.HasCurrency(cost.currencyType, cost.amount))
            {
                Debug.Log(cost.currencyType + " 부족 : 필요 " + cost.amount);
                return false;
            }
        }

        return true;
    }

    void SpendAllCosts()
    {
        if (costs == null || costs.Length == 0)
            return;

        foreach (SkillCost cost in costs)
        {
            if (cost == null)
                continue;

            CurrencyManager.instance.SpendCurrency(cost.currencyType, cost.amount);
        }
    }

    void Unlock()
    {
        isUnlocked = true;

        ApplyAllEffects();

        Debug.Log(skillName + " 해금됨");

        if (manager != null)
            manager.UpdateAllUI();
        else
            UpdateUI();
    }

    void ApplyAllEffects()
    {
        if (buffers == null || buffers.Length == 0)
        {
            Debug.LogWarning(skillName + "에 적용할 ISkillNode가 없습니다.");
            return;
        }

        foreach (ISkillNode skillEffect in buffers)
        {
            if (skillEffect != null)
                skillEffect.ApplyEffect();
        }
    }

    public bool IsUnlocked()
    {
        return isUnlocked;
    }

    public bool IsLockedByRequiredNode()
    {
        return !HasAllRequiredNodes();
    }

    public void UpdateUI()
    {
        if (ui != null)
            ui.UpdateUI();
    }

    public string GetCostText()
    {
        if (costs == null || costs.Length == 0)
            return "Free";

        string text = "";

        for (int i = 0; i < costs.Length; i++)
        {
            if (costs[i] == null)
                continue;

            text += costs[i].currencyType + " " + costs[i].amount;

            if (i < costs.Length - 1)
                text += "\n";
        }

        return text;
    }
}