using UnityEngine;
public enum SkillMapType
{
    Forest = 0,
    Wind = 1,
}
[CreateAssetMenu(
    fileName = "SkillMapData",
    menuName = "GameData/Skills/Map"
)]
public class SkillMapData : DefaultData
{
    public int totalNodeCount;
    public SkillMapType type;
}
