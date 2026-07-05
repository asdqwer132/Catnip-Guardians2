using UnityEngine;

public class SkillMapTooltipProvider : TooltipProvider
{
    [Header("Target")]
    [SerializeField] private SkillMapData mapData;
    [SerializeField] private SkillTreeManager treeManager;

    public override bool TryGetTooltipData(out TooltipData data)
    {
        data = null;

        if (mapData == null)
            return false;

        if (treeManager == null)
            treeManager = SkillTreeManager.Instance;

        data = new TooltipData
        {
            icon = mapData.icon,
            title = GetSkillName(mapData),
            subTitle = "" + mapData.totalNodeCount,
            amountText = "" + treeManager.GetUnlockedSkillCount(mapData),
            description = mapData.GetDescription(),
        };

        return true;
    }
    private string GetSkillName(SkillMapData nodeData)
    {
        if (nodeData == null)
            return "";

        if (!string.IsNullOrEmpty(nodeData.GetDataName()))
            return nodeData.GetDataName();

        return nodeData.name;
    }
}