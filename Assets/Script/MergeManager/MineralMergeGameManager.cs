using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public sealed class MineralMergeGameManager : MergegameManager
{
    public int OreCount
    {
        get { int count = 0; foreach (var piece in ActiveItems) if (piece is MineralMergeItemObject && piece.IsLive) count++; return count; }
    }
    public bool TrySpawnOre(WeightedMergeItem[] choices, Collider2D area, int limit, Vector3? deliveryPosition = null)
    {
        if (!CanOperate || OreCount >= Mathf.Max(1, limit) || !MergeGimmickUtility.TryPoint(area, out var position)) return false;
        var entry = MergeGimmickUtility.Choose(choices, ore => ore.item != null && ore.item.prefab != null &&
            ore.item.prefab.GetComponent<MineralMergeItemObject>() != null ? ore.weight : 0f);
        return entry != null && TrySpawnGimmickItem(entry.item, deliveryPosition ?? position, out _);
    }
    protected override void LateUpdate()
    {
        base.LateUpdate();
    }
    public void ClearOres()
    {
        foreach (var piece in new List<MergeItemObject>(ActiveItems))
            if (piece is MineralMergeItemObject && piece.IsLive) piece.ReturnToPool();
    }
    protected override void OnDisable() { ClearOres(); base.OnDisable(); }
}
