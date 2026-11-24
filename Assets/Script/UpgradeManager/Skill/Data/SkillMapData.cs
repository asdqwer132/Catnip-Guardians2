using UnityEngine;
[CreateAssetMenu(
    fileName = "SkillMapData",
    menuName = "Upgrade/Skills/Map"
)]
public class SkillMapData : UnlockableDefaultData
{
    [Space(10)]
    public int totalNodeCount;
}
