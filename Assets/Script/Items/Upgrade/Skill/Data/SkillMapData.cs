using UnityEngine;
public enum SkillMapType
{
    Forest = 0,
    Wind = 1,
}
[CreateAssetMenu(
    fileName = "SkillMapData",
    menuName = "03.Upgrade/Skills/Map"
)]
public class SkillMapData : DefaultData
{
    public int totalNodeCount;
    public SkillMapType type;
}
