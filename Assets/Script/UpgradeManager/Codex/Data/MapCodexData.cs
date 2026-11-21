using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Map Codex", menuName = "Upgrade/Codex/Map")]
public sealed class MapCodexData : UnlockableDefaultData
{
    [Space(10)]
    public Sprite preview;
    [Tooltip("Authored stage records for this map, in display order.")]
    public StageCodexData[] stages = Array.Empty<StageCodexData>();
}
