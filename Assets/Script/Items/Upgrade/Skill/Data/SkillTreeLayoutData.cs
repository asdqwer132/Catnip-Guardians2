using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "03.Upgrade/Skills/Tree Layout", fileName = "SkillTreeLayout")]
public sealed class SkillTreeLayoutData : ScriptableObject
{
    public SkillMapData skillMap;
    [HideInInspector] public int revision;
    public List<SkillNodeData> nodeDatas = new List<SkillNodeData>();
    public List<SkillNodePlacement> nodeLayout = new List<SkillNodePlacement>();
}
