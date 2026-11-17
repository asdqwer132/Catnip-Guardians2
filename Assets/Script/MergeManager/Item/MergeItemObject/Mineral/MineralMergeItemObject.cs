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
    protected override void ResetPoolState() { remainingResource = Mathf.Max(1, totalResource); nextHit = 0f; }
    public bool TryMine(MergeItemObject pickaxe)
    {
        if (!(Owner is MineralMergeGameManager owner) || !owner.CanOperate || !CanSelect || RemainingResource <= 0 || Time.time < nextHit ||
            pickaxe == null || pickaxe == this || pickaxe.Owner != owner || !pickaxe.CanSelect ||
            !System.Array.Exists(pickaxes, definition => definition != null && definition == pickaxe.Definition)) return false;
        nextHit = Time.time + Mathf.Max(.01f, hitInterval);
        foreach (var reward in rewards)
        {
            if (reward == null || reward.item == null) continue;
            int count = reward.RollCount();
            for (int i = 0; i < count && RemainingResource > 0; i++)
            {
                int cost = Mathf.Max(0, reward.resourceCostPerItem);
                if (cost > RemainingResource) break;
                if (owner.TrySpawnGimmickItem(reward.item, reward.Position(transform.position), out _)) remainingResource -= cost;
            }
        }
        if (chargeOnPickaxeContact) remainingResource = Mathf.Max(0, RemainingResource - Mathf.Max(0, contactCost));
        if (RemainingResource <= 0) ReturnToPool();
        return true;
    }
    protected override void OnCollisionEnter2D(Collision2D collision)
    { base.OnCollisionEnter2D(collision); TryMine(collision.collider.GetComponentInParent<MergeItemObject>()); }
    protected override void OnCollisionStay2D(Collision2D collision)
    { base.OnCollisionStay2D(collision); TryMine(collision.collider.GetComponentInParent<MergeItemObject>()); }
    private void OnTriggerEnter2D(Collider2D other) => TryMine(other.GetComponentInParent<MergeItemObject>());
    private void OnTriggerStay2D(Collider2D other) => TryMine(other.GetComponentInParent<MergeItemObject>());
}
