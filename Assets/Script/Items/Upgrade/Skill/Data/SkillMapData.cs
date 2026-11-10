using UnityEngine;
[CreateAssetMenu(
    fileName = "SkillMapData",
    menuName = "Upgrade/Skills/Map"
)]
public class SkillMapData : DefaultData
{
    [Header("Extra Data")]
    public int totalNodeCount;
}
