using System.Collections.Generic;
using UnityEngine;

public class SkillMapTooltipProvider : TooltipProvider
{
    [Header("Target")]
    [SerializeField] private SkillMapData mapData;
    [SerializeField] private SkillTreeManager treeManager;
    [SerializeField] private Transform nodeRoot;
    private readonly HashSet<SkillNodeData> graphNodes = new HashSet<SkillNodeData>();

    public SkillMapData MapData => mapData;

    protected override void Awake()
    {
        base.Awake();
        if (nodeRoot == null) return;
        foreach (var node in nodeRoot.GetComponentsInChildren<SkillNodeUI>(true))
            if (node.SkillNodeData != null) graphNodes.Add(node.SkillNodeData);
    }

    public override bool TryGetTooltipData(out TooltipData data)
    {
        data = null;

        if (mapData == null)
            return false;

        if (treeManager == null)
            treeManager = SkillTreeManager.Instance;

        if (treeManager == null)
            return false;

        int unlocked = 0;
        if (nodeRoot != null)
        {
            foreach (var node in graphNodes)
                if (treeManager.IsUnlocked(node)) unlocked++;
        }
        else unlocked = treeManager.GetUnlockedSkillCount(mapData);
        string unlockedCount = unlocked.ToString();
        int total = nodeRoot != null ? graphNodes.Count : mapData.totalNodeCount;

        data = new SkillMapTooltipData
        {
            sourceData = mapData,
            icon = mapData.icon,
            dataName = GetSkillName(mapData),
            totalNodesCount = total.ToString(),
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
