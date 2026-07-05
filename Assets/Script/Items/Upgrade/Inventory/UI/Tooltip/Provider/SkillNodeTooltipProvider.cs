using System.Text;
using UnityEngine;

public class SkillNodeTooltipProvider : CostTooltipProvider
{
    [Header("Target")]
    [SerializeField] private SkillNodeUI skillNodeUI;

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

        data = new CostTooltipData
        {
            icon = nodeData.icon,
            title = GetSkillName(nodeData),
            amountText = "",
            description = nodeData.GetDescription(),
            costs = nodeData.costs

        };

        return true;
    }
    private string GetSkillName(SkillNodeData nodeData)
    {
        if (nodeData == null)
            return "";

        if (!string.IsNullOrEmpty(nodeData.GetDataName()))
            return nodeData.GetDataName();

        return nodeData.name;
    }

}