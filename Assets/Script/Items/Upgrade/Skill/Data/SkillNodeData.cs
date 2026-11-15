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
    [Space(10)]
    [Tooltip("Icon shown after unlocking. Empty uses the basic icon.")]
    public Sprite unlockedIcon;
    [Space(10)]
    [Min(1)] public int level = 1;

    [Space(10)]
    public SkillMapData skillMap;
    [Space(10)]
    public Cost[] costs;

    [Space(10)]
    public List<SkillNodeData> requiredSkills = new List<SkillNodeData>();

    [Space(10)]
    public List<SkillRewardData> rewards = new List<SkillRewardData>();


}
