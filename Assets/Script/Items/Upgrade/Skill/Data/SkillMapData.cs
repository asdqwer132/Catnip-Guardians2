using UnityEngine;
public enum SkillMapType
{
    Forest = 0,
    Wind = 1,
}
[CreateAssetMenu(
    fileName = "SkillMapData",
    menuName = "GameData/Skill Tree/Skill Map"
)]
public class SkillMapData : DefaultData
{
    public int totalNodeCount;
    public SkillMapType type;
}
