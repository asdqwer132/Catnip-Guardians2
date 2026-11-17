using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public sealed class MineralMergeGameManager : MergegameManager
{
    private MergeItem pendingOre;
    private Vector3 pendingPosition;
    public int OreCount
    {
        get { int count = 0; foreach (var piece in ActiveItems) if (piece is MineralMergeItemObject && piece.IsLive) count++; return count; }
    }
    public bool TrySpawnOre(WeightedMergeItem[] choices, Collider2D area, int limit)
    {
        if (!CanOperate || OreCount >= Mathf.Max(1, limit) || !MergeGimmickUtility.TryPoint(area, out var position)) return false;
        if (pendingOre == null)
        {
            var entry = MergeGimmickUtility.Choose(choices, ore => ore.item != null && ore.item.prefab != null &&
                ore.item.prefab.GetComponent<MineralMergeItemObject>() != null ? ore.weight : 0f);
            if (entry == null) return false;
            pendingOre = entry.item; pendingPosition = position;
        }
        if (!TrySpawnGimmickItem(pendingOre, pendingPosition, out _)) return false;
        pendingOre = null; return true;
    }
    protected override void LateUpdate()
    {
        base.LateUpdate();
        foreach (var piece in new List<MergeItemObject>(ActiveItems))
            if (piece is MineralMergeItemObject ore && ore.IsLive) ore.RetryPendingMine();
    }
    public void ClearOres()
    {
        pendingOre = null;
        foreach (var piece in new List<MergeItemObject>(ActiveItems))
            if (piece is MineralMergeItemObject && piece.IsLive) piece.ReturnToPool();
    }
    protected override void OnDisable() { ClearOres(); base.OnDisable(); }
}
