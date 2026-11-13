using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SkillNodeData",
    menuName = "Upgrade/Skills/Node"
)]
public class SkillNodeData : DefaultData
{
    public string ProgressKey => skillMap != null && !string.IsNullOrEmpty(skillMap.dataId)
        ? skillMap.dataId + "\n" + dataId : dataId;
    [Header("Extra Data / Node Icon")]
    [Tooltip("Icon shown after unlocking. Empty uses the basic icon.")]
    public Sprite unlockedIcon;
    [Header("Node Level")]
    [Min(1)] public int level = 1;

    [Header("Extra Data / Map")]
    public SkillMapData skillMap;
    [Header("Extra Data / Cost")]
    public Cost[] costs;

    [Header("Extra Data / Requirement")]
    public List<SkillNodeData> requiredSkills = new List<SkillNodeData>();

    [Header("Extra Data / Rewards")]
    public List<SkillRewardData> rewards = new List<SkillRewardData>();


}
