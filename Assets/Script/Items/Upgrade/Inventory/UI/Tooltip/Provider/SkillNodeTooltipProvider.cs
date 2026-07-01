using System.Text;
using UnityEngine;

public class SkillNodeTooltipProvider : TooltipProvider
{
    [Header("Target")]
    [SerializeField] private SkillNodeUI skillNodeUI;

    [Header("Text")]
    [SerializeField] private string unlockedText = "Unlocked";
    [SerializeField] private string canUnlockText = "CanUnlock";
    [SerializeField] private string lockedText = "Locked";

    protected override void Awake()
    {
        base.Awake();

        if (skillNodeUI == null)
            skillNodeUI = GetComponent<SkillNodeUI>();
    }

    public override bool TryGetTooltipData(out TooltipData data)
    {
        data = null;

        if (skillNodeUI == null)
            return false;

        SkillNodeData nodeData = skillNodeUI.SkillNodeData;

        if (nodeData == null)
            return false;

        data = new TooltipData
        {
            icon = nodeData.icon,
            title = GetSkillName(nodeData),
            subTitle = GetStatusText(nodeData),
            amountText = "",
            description = BuildDescription(nodeData)
        };

        return true;
    }

    private string GetSkillName(SkillNodeData nodeData)
    {
        if (nodeData == null)
            return "";

        if (!string.IsNullOrEmpty(nodeData.skillName))
            return nodeData.skillName;

        return nodeData.name;
    }

    private string GetStatusText(SkillNodeData nodeData)
    {
        if (nodeData == null)
            return "";

        if (SkillTreeManager.Instance == null)
            return "";

        bool isUnlocked = SkillTreeManager.Instance.IsUnlocked(nodeData.skillId);

        if (isUnlocked)
            return unlockedText;

        bool canUnlock = SkillTreeManager.Instance.CanUnlock(nodeData);

        return canUnlock ? canUnlockText : lockedText;
    }

    private string BuildDescription(SkillNodeData nodeData)
    {
        StringBuilder sb = new StringBuilder();

        if (!string.IsNullOrEmpty(nodeData.description))
            sb.AppendLine(nodeData.description);

        AppendRequiredSkills(sb, nodeData);
        AppendCosts(sb, nodeData);
        AppendRewards(sb, nodeData);

        return sb.ToString().TrimEnd();
    }

    private void AppendRequiredSkills(StringBuilder sb, SkillNodeData nodeData)
    {
        if (nodeData.requiredSkills == null || nodeData.requiredSkills.Count == 0)
            return;

        AppendSectionGap(sb);
        sb.AppendLine("[Required]");

        for (int i = 0; i < nodeData.requiredSkills.Count; i++)
        {
            SkillNodeData requiredSkill = nodeData.requiredSkills[i];

            if (requiredSkill == null)
                continue;

            sb.AppendLine("- " + GetSkillName(requiredSkill));
        }
    }

    private void AppendCosts(StringBuilder sb, SkillNodeData nodeData)
    {
        if (nodeData.costs == null || nodeData.costs.Count == 0)
            return;

        AppendSectionGap(sb);
        sb.AppendLine("[Cost]");

        for (int i = 0; i < nodeData.costs.Count; i++)
        {
            Cost cost = nodeData.costs[i];

            if (cost == null)
                continue;

            sb.AppendLine("- " + cost.ToString());
        }
    }

    private void AppendRewards(StringBuilder sb, SkillNodeData nodeData)
    {
        if (nodeData.rewards == null || nodeData.rewards.Count == 0)
            return;

        AppendSectionGap(sb);
        sb.AppendLine("[Effect]");

        for (int i = 0; i < nodeData.rewards.Count; i++)
        {
            SkillRewardData reward = nodeData.rewards[i];

            if (reward == null)
                continue;

            sb.AppendLine("- " + reward.ToString());
        }
    }
}