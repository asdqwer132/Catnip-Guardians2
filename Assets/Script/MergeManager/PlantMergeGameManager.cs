using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class MergePlantGrowthRule
{
    public MergeItem seed;
    [Min(.01f)] public float growthSeconds = 5f;
    public WeightedMergeItem[] nextItems = Array.Empty<WeightedMergeItem>();
}

[DefaultExecutionOrder(-200)]
public sealed class PlantMergeGameManager : MergegameManager
{
    private sealed class Growth
    { public MergeItemObject piece; public int life; public MergePlantGrowthRule rule; public float elapsed; public MergeItem next; }
    private readonly List<Growth> growing = new List<Growth>();
    private readonly List<MergeSpawnRequest> output = new List<MergeSpawnRequest>();
    public int RootedCount => growing.Count;
    public bool TryRoot(MergeItemObject piece, MergePlantGrowthRule rule)
    {
        if (!CanOperate || piece == null || piece.Owner != this || rule == null || rule.seed != piece.Definition ||
            rule.nextItems == null || rule.nextItems.Length == 0 || piece.IsHeld || !piece.TryHold(this, true)) return false;
        growing.Add(new Growth { piece = piece, life = piece.LifeId, rule = rule }); return true;
    }
    public void TickPlants(Collider2D ground, MergePlantGrowthRule[] rules, float deltaTime)
    {
        if (!CanOperate || deltaTime <= 0f) return;
        foreach (var piece in new List<MergeItemObject>(ActiveItems))
        {
            if (piece == null || piece.IsHeld || !piece.CanSelect || !MergeGimmickUtility.Touches(ground, piece)) continue;
            if (rules != null) foreach (var rule in rules) if (TryRoot(piece, rule)) break;
        }
        for (int i = growing.Count - 1; i >= 0; i--)
        {
            var state = growing[i];
            if (state.piece == null || !state.piece.IsLive || state.piece.LifeId != state.life) { growing.RemoveAt(i); continue; }
            state.elapsed += deltaTime;
            if (state.elapsed < Mathf.Max(.01f, state.rule.growthSeconds)) continue;
            if (state.next == null) state.next = MergeGimmickUtility.ChooseItem(state.rule.nextItems);
            if (state.next == null) continue;
            output.Clear(); output.Add(new MergeSpawnRequest(state.next, state.piece.transform.position));
            if (TryReplaceGimmickItem(state.piece, state.life, output, this)) growing.RemoveAt(i);
        }
    }
    public void ReleaseRoots()
    {
        foreach (var state in growing)
            if (state.piece != null && state.piece.LifeId == state.life) state.piece.ReleaseHold(this);
        growing.Clear();
    }
    protected override void OnDisable() { ReleaseRoots(); base.OnDisable(); }
}
