using UnityEngine;

[CreateAssetMenu(menuName = "Items/Merge/Monster Merge Item")]
public sealed class MonsterMergeItem : MergeItem
{
    [Min(.01f)] public float eatInterval = 3f;
    [Min(1)] public int eatCountPerTick = 1;
    public bool eatOtherMonsters;
    public MergeItemYield[] quarantineRewards = System.Array.Empty<MergeItemYield>();
}
