using UnityEngine;

public class SkillMapTooltipProvider : TooltipProvider
{
    [Header("Target")]
    [SerializeField] private SkillMapData mapData;
    [SerializeField] private SkillTreeManager treeManager;

    public SkillMapData MapData => mapData;

    public override bool TryGetTooltipData(out TooltipData data)
    {
        data = null;

        if (mapData == null)
            return false;

        if (treeManager == null)
            treeManager = SkillTreeManager.Instance;

        if (treeManager == null)
            return false;

        string unlockedCount = treeManager.GetUnlockedSkillCount(mapData).ToString();

        data = new SkillMapTooltipData
        {
            sourceData = mapData,
            icon = mapData.icon,
            dataName = GetSkillName(mapData),
            totalNodesCount = "" + mapData.totalNodeCount,
            completedNodesCount = unlockedCount,
            amountText = unlockedCount,
            description = mapData.GetDescription(),
            detailDescription = mapData.GetDetailDescription(),
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
