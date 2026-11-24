using UnityEngine;

// Harvestable ore uses the same lifecycle, physics and recipe pipeline as every merge item.
public sealed class MineralMergeItemObject : MergeItemObject
{
    public override void Initialize(MergegameManager owner, MergeItem definition, uint serial, float mergeDelay)
    {
        if (definition is MineralMergeItem data)
        {
            totalResource = data.totalResource;
            chargeOnPickaxeContact = data.chargeOnPickaxeContact;
            contactCost = data.contactCost;
            hitInterval = data.hitInterval;
            pickaxes = data.pickaxes;
            rewards = data.rewards;
        }
        base.Initialize(owner, definition, serial, mergeDelay);
        ResetPoolState();
    }
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
        if (!(Owner is { } owner) || !owner.CanOperate || !CanSelect || RemainingResource <= 0 || Time.time < nextHit ||
            pickaxe == null || pickaxe == this || pickaxe.Owner != owner || !pickaxe.CanSelect ||
            !System.Array.Exists(pickaxes, definition => definition != null && definition == pickaxe.Definition)) return false;
        nextHit = Time.time + Mathf.Max(.01f, hitInterval);
        pickaxe.ReturnToPool();
        PlayPresentation(MergeItemPresentationEvent.Gimmick, "Mineral.Mine");
        int budget = RemainingResource;
        foreach (var reward in rewards)
        {
            if (reward == null || reward.item == null) continue;
            int count = reward.RollCount();
            for (int i = 0; i < count && budget > 0; i++)
            {
                int cost = Mathf.Max(0, reward.resourceCostPerItem);
                if (cost > budget) break;
                if (owner.TrySpawnGimmickItem(reward.item, transform.position, out _)) budget -= cost;
            }
        }
        remainingResource = budget;
        if (chargeOnPickaxeContact) remainingResource = Mathf.Max(0, remainingResource - Mathf.Max(0, contactCost));
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
