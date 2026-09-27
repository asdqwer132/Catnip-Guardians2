using System;
using UnityEngine;

[Serializable]
public sealed class CraftGaugeAmount
{
    public CraftGaugeType gaugeType;
    [Min(0f)] public float amount = 1f;
}

// Defines what one unit of this inventory item adds before manager-specific weights.
[CreateAssetMenu(fileName = "MaterialGauge", menuName = "GameData/Item/Crafting/Material Gauge Data")]
public sealed class CraftMaterialGaugeData : ScriptableObject
{
    public ItemData itemData;
    public CraftGaugeAmount[] gauges = new CraftGaugeAmount[0];
}
