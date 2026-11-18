using UnityEngine;

public sealed class MonsterMergeItemObject : MergeItemObject
{
    public override void Initialize(MergegameManager owner, MergeItem definition, uint serial, float mergeDelay)
    {
        if (definition is MonsterMergeItem data)
        {
            eatInterval = data.eatInterval;
            eatCountPerTick = data.eatCountPerTick;
            eatOtherMonsters = data.eatOtherMonsters;
            quarantineRewards = data.quarantineRewards;
        }
        base.Initialize(owner, definition, serial, mergeDelay);
        ResetPoolState();
    }
    [Min(.01f)] public float eatInterval = 3f;
    [Min(1)] public int eatCountPerTick = 1;
    public bool eatOtherMonsters;
    public MergeItemYield[] quarantineRewards = System.Array.Empty<MergeItemYield>();
    private float nextEat;
    private bool quarantined;
    protected override void ResetPoolState() { nextEat = Time.time + Mathf.Max(.01f, eatInterval); quarantined = false; }
    public void TickMonster(Collider2D quarantine)
    {
        if (!IsLive || Owner == null || !Owner.CanOperate || IsHeld) return;
        if (!quarantined && MergeGimmickUtility.Touches(quarantine, this))
        {
            quarantined = true;
            PlayPresentation(MergeItemPresentationEvent.Gimmick, "Monster.Quarantine");
            MergeGimmickUtility.Emit(Owner, quarantineRewards, transform.position);
            ReturnToPool();
            return;
        }
        if (Time.time < nextEat) return;
        nextEat = Time.time + Mathf.Max(.01f, eatInterval);
        int eaten = 0;
        var candidates = new System.Collections.Generic.List<MergeItemObject>(Owner.ActiveItems);
        foreach (var other in candidates)
        {
            if (other == null || other == this || (!eatOtherMonsters && other is MonsterMergeItemObject) || !other.CanSelect) continue;
            if ((colider2d.IsTouching(other.colider2d) || colider2d.Distance(other.colider2d).isOverlapped) && Owner.TryRemoveItem(other, other.LifeId))
            {
                PlayPresentation(MergeItemPresentationEvent.Gimmick, "Monster.Eat");
                if (++eaten >= eatCountPerTick) break;
            }
        }
    }
}
