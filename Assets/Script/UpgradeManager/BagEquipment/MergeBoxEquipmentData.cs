using UnityEngine;

[CreateAssetMenu(menuName = "Catnip/Merge Box Equipment")]
public sealed class MergeBoxEquipmentData : UnlockableDefaultData
{
    [Range(1, 8)] public int boxIndex = 1;
}
