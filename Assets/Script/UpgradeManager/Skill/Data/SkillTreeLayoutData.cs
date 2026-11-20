using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Upgrade/Skills/Tree Layout", fileName = "SkillTreeLayout")]
public sealed class SkillTreeLayoutData : ScriptableObject
{
    public SkillMapData skillMap;
    [HideInInspector] public bool gridPlacement;
    [HideInInspector] public Vector2 gridCellSize = new Vector2(400f, 300f);
    [HideInInspector] public Vector2 gridNodeGap = new Vector2(20f, 20f);
    [HideInInspector] public int revision;
    public List<SkillNodeData> nodeDatas = new List<SkillNodeData>();
    public List<SkillNodePlacement> nodeLayout = new List<SkillNodePlacement>();
}
