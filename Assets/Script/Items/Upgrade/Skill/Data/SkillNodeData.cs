using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SkillNodeData",
    menuName = "GameData/Skill Tree/Skill Node"
)]
public class SkillNodeData : DefaultData
{
    [Header("Map")]
    public SkillMapData skillMap;

    [Header("Cost")]
    public Cost[] costs;

    [Header("Requirement")]
    public List<SkillNodeData> requiredSkills = new List<SkillNodeData>();

    [Header("Rewards")]
    public List<SkillRewardData> rewards = new List<SkillRewardData>();
}
