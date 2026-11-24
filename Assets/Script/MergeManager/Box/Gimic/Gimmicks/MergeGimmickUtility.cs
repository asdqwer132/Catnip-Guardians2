using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class MergeBonusSpawnSettings
{
    public Vector2 scatter = new Vector2(.15f, .1f);
    [Min(0f)] public float outwardSpeed = 1f;
    [Min(0f)] public float upwardSpeed = 1f;
    [Header("생성 중심 폭발 · 힘 0이면 비활성")]
    [Min(0f)] public float explosionRadius = 1f;
    [Min(0f)] public float explosionImpulse = 1f;
    public bool explosionFalloff = true;
    public Vector3 Position(Vector3 center) => center + new Vector3(
        UnityEngine.Random.Range(-Mathf.Abs(scatter.x), Mathf.Abs(scatter.x)),
        UnityEngine.Random.Range(-Mathf.Abs(scatter.y), Mathf.Abs(scatter.y)), 0);
    public void Launch(MergeItemObject piece, Vector3 center)
    {
        Vector2 direction = (Vector2)(piece.transform.position - center);
        if (direction.sqrMagnitude < .00001f) direction = UnityEngine.Random.insideUnitCircle;
        piece.Velocity = direction.normalized * outwardSpeed + Vector2.up * upwardSpeed;
        piece.SetSimulated(true);
        if (explosionRadius <= 0f || explosionImpulse <= 0f || piece.Owner == null) return;
        foreach (var other in piece.Owner.ActiveItems)
        {
            if (other == null || other == piece || !other.IsLive || other.IsHeld || other.body == null) continue;
            Vector2 offset = other.transform.position - center;
            float distance = offset.magnitude;
            if (distance > explosionRadius) continue;
            Vector2 outward = distance > .0001f ? offset / distance : UnityEngine.Random.insideUnitCircle.normalized;
            float strength = explosionImpulse * (explosionFalloff ? 1f - distance / explosionRadius : 1f);
            other.body.AddForce(outward * strength, ForceMode2D.Impulse);
        }
    }
}

[Serializable]
public sealed class MergeItemYield
{
    public MergeItem item;
    [Min(0)] public int minimumCount = 1, maximumCount = 1;
    [Range(0f, 1f)] public float probability = 1f;
    [Min(0)] public int resourceCostPerItem;
    public int RollCount()
    {
        if (probability < 1f && UnityEngine.Random.value >= Mathf.Clamp01(probability)) return 0;
        int min = Mathf.Max(0, minimumCount), max = Mathf.Max(min, maximumCount);
        return UnityEngine.Random.Range(min, max == int.MaxValue ? max : max + 1);
    }
}

[Serializable]
public sealed class WeightedMergeItem
{
    public MergeItem item;
    [Min(0f)] public float weight = 1f;
}

public struct MergeSpawnRequest
{
    public MergeItem item;
    public Vector3 position;
    public MergeSpawnRequest(MergeItem item, Vector3 position) { this.item = item; this.position = position; }
}

public static class MergeGimmickUtility
{
    // Sample inside the actual collider, including rotated boxes and nonrectangular areas.
    public static bool TryPoint(Collider2D area, out Vector3 point)
    {
        point = default;
        if (area == null || !area.isActiveAndEnabled) return false;
        Bounds bounds = area.bounds;
        for (int i = 0; i < 64; i++)
        {
            point = new Vector3(UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                UnityEngine.Random.Range(bounds.min.y, bounds.max.y), area.transform.position.z);
            if (area.OverlapPoint(point)) return true;
        }
        return false;
    }
    public static bool Touches(Collider2D area, MergeItemObject piece)
        => area != null && area.isActiveAndEnabled && piece != null && piece.IsLive &&
           piece.colider2d != null && piece.colider2d.enabled && (area.IsTouching(piece.colider2d) || area.Distance(piece.colider2d).isOverlapped);

    public static T Choose<T>(IReadOnlyList<T> entries, Func<T, float> weight) where T : class
    {
        if (entries == null) return null;
        float total = 0f;
        foreach (var entry in entries) if (entry != null) total += Mathf.Max(0f, weight(entry));
        if (total <= 0f) return null;
        float roll = UnityEngine.Random.value * total;
        T last = null;
        foreach (var entry in entries) if (entry != null)
        { float value = Mathf.Max(0f, weight(entry)); if (value > 0f) last = entry; if (value > 0f && roll < value) return entry; roll -= value; }
        return last;
    }
    public static MergeItem ChooseItem(WeightedMergeItem[] entries)
        => Choose(entries, entry => entry.item != null ? entry.weight : 0f)?.item;

    public static int Emit(MergegameManager board, MergeItemYield[] yields, Vector3 origin)
    {
        int emitted = 0;
        if (board == null || yields == null) return emitted;
        foreach (var yield in yields)
        {
            if (yield == null || yield.item == null) continue;
            int count = yield.RollCount();
            for (int i = 0; i < count; i++)
                if (board.TrySpawnGimmickItem(yield.item, origin, out _)) emitted++;
        }
        return emitted;
    }
}
