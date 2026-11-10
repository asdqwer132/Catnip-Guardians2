using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Map Codex", menuName = "Upgrade/Codex/Map")]
public sealed class MapCodexData : DefaultData
{
    [Header("Extra Data")]
    public Sprite preview;
    [Tooltip("Authored stage records for this map, in display order.")]
    public StageCodexData[] stages = Array.Empty<StageCodexData>();
}
