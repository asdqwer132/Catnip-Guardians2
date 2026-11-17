using UnityEngine;

public sealed class MonsterMergeItemObject : MergeItemObject
{
    [Min(.01f)] public float eatInterval = 3f;
    [Min(1)] public int eatCountPerTick = 1;
    public bool eatOtherMonsters;
    public MergeItemYield[] quarantineRewards = System.Array.Empty<MergeItemYield>();
    private float nextEat;
    private bool quarantined;
    private readonly System.Collections.Generic.List<MergeSpawnRequest> pendingRewards = new System.Collections.Generic.List<MergeSpawnRequest>();
    public override void RemapPendingSpawns(Matrix4x4 change)
    {
        for (int i = 0; i < pendingRewards.Count; i++)
        { var request = pendingRewards[i]; request.position = change.MultiplyPoint3x4(request.position); pendingRewards[i] = request; }
    }
    protected override void ResetPoolState() { nextEat = Time.time + Mathf.Max(.01f, eatInterval); quarantined = false; pendingRewards.Clear(); }
    public void TickMonster(Collider2D quarantine)
    {
        if (!IsLive || Owner == null || !Owner.CanOperate || IsHeld) return;
        if (!quarantined && MergeGimmickUtility.Touches(quarantine, this))
        {
            quarantined = true;
            foreach (var reward in quarantineRewards)
            {
                if (reward == null || reward.item == null) continue;
                int count = reward.RollCount();
                for (int i = 0; i < count; i++) pendingRewards.Add(new MergeSpawnRequest(reward.item, reward.Position(transform.position)));
            }
        }
        if (quarantined)
        {
            if (pendingRewards.Count == 0) ReturnToPool();
            else Owner.TryReplaceGimmickItem(this, LifeId, pendingRewards);
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
                if (++eaten >= eatCountPerTick) break;
        }
    }
}
