using UnityEngine;

[CreateAssetMenu(menuName = "Items/Merge/Mineral Merge Item")]
public sealed class MineralMergeItem : MergeItem
{
    [Min(1)] public int totalResource = 10;
    public bool chargeOnPickaxeContact = true;
    [Min(0)] public int contactCost = 1;
    [Min(.01f)] public float hitInterval = .5f;
    public MergeItem[] pickaxes = System.Array.Empty<MergeItem>();
    public MergeItemYield[] rewards = System.Array.Empty<MergeItemYield>();
}
