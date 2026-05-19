using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SkillNodeData",
    menuName = "Game/Skill Tree/Skill Node"
)]
public class SkillNodeData : ScriptableObject
{
    [Header("Identity")]
    public string skillId;
    public string skillName;

    [TextArea]
    public string description;

    [Header("Icon")]
    public Sprite icon;

    [Header("Cost")]
    public List<Cost> costs = new List<Cost>();

    [Header("Requirement")]
    public List<SkillNodeData> requiredSkills = new List<SkillNodeData>();

    [Header("Rewards")]
    public List<SkillRewardData> rewards = new List<SkillRewardData>();
}