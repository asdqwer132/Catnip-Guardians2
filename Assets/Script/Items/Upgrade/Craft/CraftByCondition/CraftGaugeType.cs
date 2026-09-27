using UnityEngine;

// Each asset represents one gauge, such as Fire or Ice.
[CreateAssetMenu(fileName = "GaugeType", menuName = "GameData/Item/Crafting/Gauge Type")]
public sealed class CraftGaugeType : ScriptableObject
{
    public string displayName;
}
