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


public partial class MergegameManager
{
    private sealed class Growth
    {
        public MergeItemObject piece;
        public int life;
        public MergePlantGrowthRule rule;
        public float elapsed;
        public MergeItem next;
    }

    private readonly List<Growth> growing = new List<Growth>();
    private readonly List<MergeSpawnRequest> output = new List<MergeSpawnRequest>();
    public int RootedCount => growing.Count;

    public bool TryRoot(MergeItemObject piece, MergePlantGrowthRule rule)
    {
        if (!CanOperate || piece == null || piece.Owner != this || rule == null || rule.seed != piece.Definition ||
            rule.nextItems == null || rule.nextItems.Length == 0 || piece.IsHeld || !piece.TryHold(this, true)) return false;
        var state = new Growth { piece = piece, life = piece.LifeId, rule = rule };
        growing.Add(state);
        piece.PlayPresentation(MergeItemPresentationEvent.Gimmick, "Plant.Root");
        RefreshProgress(state);
        return true;
    }

    public void TickPlants(Collider2D ground, MergePlantGrowthRule[] rules, float deltaTime)
    {
        if (!CanOperate || deltaTime <= 0f) return;
        foreach (var piece in new List<MergeItemObject>(ActiveItems))
        {
            if (piece == null || piece.IsHeld || !piece.CanSelect || !MergeGimmickUtility.Touches(ground, piece)) continue;
            FindOrRoot(piece, rules);
        }
        foreach (var state in growing.ToArray())
        {
            if (!IsCurrent(state)) { growing.Remove(state); continue; }
            state.elapsed += deltaTime;
            RefreshProgress(state);
            if (state.elapsed >= GrowthSeconds(state) && TryComplete(state)) growing.Remove(state);
        }
    }

    // Snapshot original lives so one activation grows each original item only once.
    // Outputs cannot immediately chain through several growth stages in the same skill.
    public int InstantGrowInArea(Collider2D area, MergePlantGrowthRule[] rules)
    {
        if (!CanOperate || area == null || !area.isActiveAndEnabled) return 0;
        var targets = new List<KeyValuePair<MergeItemObject, int>>();
        foreach (var piece in ActiveItems)
            if (piece != null && piece.CanSelect && MergeGimmickUtility.Touches(area, piece))
                targets.Add(new KeyValuePair<MergeItemObject, int>(piece, piece.LifeId));

        int completed = 0;
        foreach (var target in targets)
        {
            if (!CanOperate) break;
            var piece = target.Key;
            if (piece == null || piece.LifeId != target.Value || !piece.CanSelect) continue;
            var state = FindOrRoot(piece, rules);
            if (state == null) continue;
            state.elapsed = Mathf.Max(state.elapsed, GrowthSeconds(state));
            RefreshProgress(state);
            if (TryComplete(state)) { growing.Remove(state); completed++; }
        }
        return completed;
    }

    private Growth FindOrRoot(MergeItemObject piece, MergePlantGrowthRule[] rules)
    {
        foreach (var state in growing)
            if (state.piece == piece && state.life == piece.LifeId) return state;
        if (rules != null) foreach (var rule in rules)
            if (TryRoot(piece, rule)) return growing[growing.Count - 1];
        return null;
    }

    private bool IsCurrent(Growth state)
        => state.piece != null && state.piece.IsLive && state.piece.Owner == this && state.piece.LifeId == state.life && state.piece.IsHeldBy(this);
    private static float GrowthSeconds(Growth state) => Mathf.Max(.01f, state.rule.growthSeconds);
    private static void RefreshProgress(Growth state)
    {
        if (state.piece is PlantMergeItemObject plant)
            plant.SetGrowthProgress(state.elapsed / GrowthSeconds(state));
    }

    private bool TryComplete(Growth state)
    {
        if (!IsCurrent(state)) return false;
        if (state.next == null) state.next = MergeGimmickUtility.ChooseItem(state.rule.nextItems);
        if (state.next == null) return false;
        output.Clear();
        output.Add(new MergeSpawnRequest(state.next, state.piece.transform.position));
        // A full pool/failed replacement keeps the original rooted at 100% and retries.
        return TryReplaceGimmickItem(state.piece, state.life, output, this);
    }

    public void ReleaseRoots()
    {
        foreach (var state in growing)
            if (state.piece != null && state.piece.LifeId == state.life)
            {
                state.piece.ReleaseHold(this);
                if (state.piece is PlantMergeItemObject plant) plant.ClearGrowthProgress();
            }
        growing.Clear();
    }

}
