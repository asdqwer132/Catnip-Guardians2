using UnityEngine;

// Harvestable ore uses the same lifecycle, physics and recipe pipeline as every merge item.
public sealed class MineralMergeItemObject : MergeItemObject
{
    [Min(1)] public int totalResource = 10;
    public bool chargeOnPickaxeContact = true;
    [Min(0)] public int contactCost = 1;
    [Min(.01f)] public float hitInterval = .5f;
    public MergeItem[] pickaxes = System.Array.Empty<MergeItem>();
    public MergeItemYield[] rewards = System.Array.Empty<MergeItemYield>();
    private int remainingResource;
    public int RemainingResource => IsLive ? remainingResource : 0;
    private float nextHit;
    private bool pendingHit;
    private readonly System.Collections.Generic.List<MergeSpawnRequest> pendingRewards = new System.Collections.Generic.List<MergeSpawnRequest>();
    public override void RemapPendingSpawns(Matrix4x4 change)
    {
        for (int i = 0; i < pendingRewards.Count; i++)
        { var request = pendingRewards[i]; request.position = change.MultiplyPoint3x4(request.position); pendingRewards[i] = request; }
    }
    protected override void ResetPoolState() { remainingResource = Mathf.Max(1, totalResource); nextHit = 0f; pendingHit = false; pendingResourceCost = 0; pendingRewards.Clear(); }
    public bool TryMine(MergeItemObject pickaxe)
    {
        if (pendingHit) return false;
        if (!(Owner is MineralMergeGameManager owner) || !owner.CanOperate || !CanSelect || RemainingResource <= 0 || Time.time < nextHit ||
            pickaxe == null || pickaxe == this || pickaxe.Owner != owner || !pickaxe.CanSelect ||
            !System.Array.Exists(pickaxes, definition => definition != null && definition == pickaxe.Definition)) return false;
        int budget = RemainingResource;
        foreach (var reward in rewards)
        {
            if (reward == null || reward.item == null) continue;
            int count = reward.RollCount();
            for (int i = 0; i < count && budget > 0; i++)
            {
                int cost = Mathf.Max(0, reward.resourceCostPerItem);
                if (cost > budget) break;
                pendingRewards.Add(new MergeSpawnRequest(reward.item, reward.Position(transform.position)));
                budget -= cost;
            }
        }
        pendingResourceCost = RemainingResource - budget;
        pendingHit = true;
        RetryPendingMine();
        return true;
    }
    private int pendingResourceCost;
    public void RetryPendingMine()
    {
        if (!pendingHit || !IsLive || !(Owner is MineralMergeGameManager owner) || !owner.CanOperate) return;
        if (!owner.SpawnRetryReady(this)) return;
        int life = LifeId;
        if (!owner.TrySpawnGimmickBatch(pendingRewards)) { owner.DelaySpawnRetry(this); return; }
        if (!IsLive || LifeId != life) return;
        remainingResource -= pendingResourceCost;
        pendingHit = false; pendingRewards.Clear();
        nextHit = Time.time + Mathf.Max(.01f, hitInterval);
        if (chargeOnPickaxeContact) remainingResource = Mathf.Max(0, RemainingResource - Mathf.Max(0, contactCost));
        if (RemainingResource <= 0) ReturnToPool();
    }
    protected override void OnCollisionEnter2D(Collision2D collision)
    { base.OnCollisionEnter2D(collision); TryMine(collision.collider.GetComponentInParent<MergeItemObject>()); }
    protected override void OnCollisionStay2D(Collision2D collision)
    { base.OnCollisionStay2D(collision); TryMine(collision.collider.GetComponentInParent<MergeItemObject>()); }
    private void OnTriggerEnter2D(Collider2D other) => TryMine(other.GetComponentInParent<MergeItemObject>());
    private void OnTriggerStay2D(Collider2D other) => TryMine(other.GetComponentInParent<MergeItemObject>());
}
