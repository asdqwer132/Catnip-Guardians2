using System;
using UnityEngine;

public enum MergeBoxSpriteRole { SideWall, BottomWall, BackFrame, Fixed }

[Serializable]
public sealed class MergeBoxSpritePart
{
    public MergeBoxSpriteRole role;
    public SpriteRenderer renderer;
    public BoxCollider2D wallCollider;
    [HideInInspector] public Vector2 originalSize, colliderSize;
    [HideInInspector] public Vector3 position, colliderPosition;
    [HideInInspector] public Vector2 units, colliderUnits;

    [SerializeField, HideInInspector] private Vector2 originalColliderOffset;
    [SerializeField, HideInInspector] private bool colliderOffsetCaptured;

    public void Capture(Transform box)
    {
        if (renderer == null) return;
        originalSize = renderer.size;
        position = box.InverseTransformPoint(renderer.transform.position);
        units = Units(box, renderer.transform);
        if (wallCollider == null) return;
        originalColliderOffset = wallCollider.offset; colliderOffsetCaptured = true;
        colliderSize = wallCollider.size;
        colliderPosition = box.InverseTransformPoint(wallCollider.transform.position);
        colliderUnits = Units(box, wallCollider.transform);
    }
    private static Vector2 Units(Transform box, Transform part) => new Vector2(
        Mathf.Max(.0001f, box.InverseTransformVector(part.TransformVector(Vector3.right)).magnitude),
        Mathf.Max(.0001f, box.InverseTransformVector(part.TransformVector(Vector3.up)).magnitude));
    public void Apply(Transform box, Vector2 delta, Vector2 ratio)
    {
        if (renderer == null) return;
        Vector2 resize = role == MergeBoxSpriteRole.Fixed ? Vector2.zero : role == MergeBoxSpriteRole.SideWall ? new Vector2(0, delta.y) :
            role == MergeBoxSpriteRole.BottomWall ? new Vector2(delta.x, 0) : delta;
        renderer.size = Vector2.Max(Vector2.one * .01f, originalSize + resize / units);
        Vector2 positionRatio = role == MergeBoxSpriteRole.Fixed ? Vector2.one : ratio;
        if (wallCollider == null) { Place(box, renderer.transform, position, positionRatio); return; }
        // Older prefabs have no captured offset yet. Capture it once before applying padding.
        if (!colliderOffsetCaptured) { originalColliderOffset = wallCollider.offset; colliderOffsetCaptured = true; }
        // This controller owns the collider geometry; auto-tiling would overwrite its insets.
        if (wallCollider.autoTiling) wallCollider.autoTiling = false;
        wallCollider.offset = originalColliderOffset;
        wallCollider.size = Vector2.Max(Vector2.one * .01f, colliderSize + resize / colliderUnits);
        if (wallCollider.transform.IsChildOf(renderer.transform))
        {
            Place(box, renderer.transform, position, positionRatio);
            if (wallCollider.transform != renderer.transform) Place(box, wallCollider.transform, colliderPosition, positionRatio);
        }
        else
        {
            Place(box, wallCollider.transform, colliderPosition, positionRatio);
            Place(box, renderer.transform, position, positionRatio);
        }
    }
    public void ApplyBackgroundPadding(Transform box, Vector4 padding)
    {
        if (renderer == null || role != MergeBoxSpriteRole.BackFrame) return;
        Vector2 available = Vector2.Max(Vector2.zero, (renderer.size - Vector2.one * .01f) * units);
        float horizontal = padding.x + padding.z, vertical = padding.y + padding.w;
        float x = horizontal > 0 ? Mathf.Min(1, available.x / horizontal) : 0;
        float y = vertical > 0 ? Mathf.Min(1, available.y / vertical) : 0;
        padding = new Vector4(padding.x * x, padding.y * y, padding.z * x, padding.w * y);
        Vector3 previousCenter = renderer.localBounds.center;
        renderer.size -= new Vector2((padding.x + padding.z) / units.x, (padding.y + padding.w) / units.y);
        // Preserve the unpadded center even when the sprite has an off-center pivot.
        renderer.transform.position += renderer.transform.TransformVector(previousCenter - renderer.localBounds.center) +
            box.TransformVector(new Vector3((padding.x - padding.z) * .5f, (padding.y - padding.w) * .5f, 0));
    }
    public void ApplyAdjustment(Transform box, MergeBoxPartAdjustment adjustment)
    {
        if (renderer == null) return;
        Vector3 offset = box.TransformVector(adjustment.positionOffset);
        Transform colliderTransform = wallCollider != null ? wallCollider.transform : null;
        // Move a shared transform or parent only once; siblings each receive the offset.
        if (colliderTransform == null || !renderer.transform.IsChildOf(colliderTransform))
            renderer.transform.position += offset;
        if (colliderTransform != null && !colliderTransform.IsChildOf(renderer.transform))
            colliderTransform.position += offset;
        if (wallCollider == null) return;
        if (colliderTransform == renderer.transform) renderer.transform.position += offset;

        Vector4 padding = adjustment.colliderPadding.Value;
        Vector2 available = Vector2.Max(Vector2.zero, (wallCollider.size - Vector2.one * .01f) * colliderUnits);
        float horizontal = padding.x + padding.z, vertical = padding.y + padding.w;
        float x = horizontal > 0 ? Mathf.Min(1, available.x / horizontal) : 0;
        float y = vertical > 0 ? Mathf.Min(1, available.y / vertical) : 0;
        padding = new Vector4(padding.x * x, padding.y * y, padding.z * x, padding.w * y);
        wallCollider.size -= new Vector2((padding.x + padding.z) / colliderUnits.x, (padding.y + padding.w) / colliderUnits.y);
        wallCollider.offset += (Vector2)colliderTransform.InverseTransformVector(box.TransformVector(
            new Vector3((padding.x - padding.z) * .5f, (padding.y - padding.w) * .5f, 0)));
    }
    public static void Place(Transform box, Transform target, Vector3 original, Vector2 ratio)
    {
        if (target != null) target.position = box.TransformPoint(new Vector3(original.x * ratio.x, original.y * ratio.y, original.z));
    }
}

[Serializable]
public struct MergeBoxBackgroundPadding
{
    [Min(0)] public float left, right, top, bottom;
    public Vector4 Value => new Vector4(Mathf.Max(0, left), Mathf.Max(0, bottom), Mathf.Max(0, right), Mathf.Max(0, top));
}

[Serializable]
public struct MergeBoxColliderPadding
{
    [Min(0)] public float left, right, top, bottom;
    public Vector4 Value => new Vector4(Mathf.Max(0, left), Mathf.Max(0, bottom), Mathf.Max(0, right), Mathf.Max(0, top));
}

[Serializable]
public sealed class MergeBoxPartAdjustment
{
    public string name;
    public SpriteRenderer renderer;
    [Tooltip("Box-local offset. Moves this visual and its associated collider together.")]
    public Vector2 positionOffset;
    [Tooltip("Insets only this collider in box-local units; asymmetric padding also shifts its center.")]
    public MergeBoxColliderPadding colliderPadding;
    [NonSerialized] internal Vector2 appliedOffset;
    [NonSerialized] internal Vector4 appliedPadding;
    internal bool Changed => positionOffset != appliedOffset || colliderPadding.Value != appliedPadding;
    internal void Remember() { appliedOffset = positionOffset; appliedPadding = colliderPadding.Value; }
}

[Serializable]
public sealed class MergeBoxVisualSize
{
    public MergeBoxBindings box;
    [Min(.01f)] public float width = 4.5f;
    [Min(.01f)] public float height = 5.5f;
    [Tooltip("Insets only the BackFrame visual, in box-local units. Walls, colliders and drop positions are unchanged.")]
    public MergeBoxBackgroundPadding backgroundPadding;
    [Tooltip("Additional offsets in the box's local coordinates.")]
    public Vector2 spawnOffset;
    public Vector2 deadlineOffset;
    [Tooltip("Automatically matched to this box's role bindings. Each wall/frame can be adjusted separately.")]
    public MergeBoxPartAdjustment[] parts = Array.Empty<MergeBoxPartAdjustment>();
    internal bool SynchronizeParts()
    {
        var visuals = box.visuals;
        int count = 0;
        foreach (var part in visuals) if (part?.renderer != null) count++;
        bool same = parts != null && parts.Length == count;
        int index = 0;
        foreach (var part in visuals)
        {
            if (part?.renderer == null) continue;
            if (same && (parts[index] == null || parts[index].renderer != part.renderer)) same = false;
            index++;
        }
        if (same) return false;
        var matched = new MergeBoxPartAdjustment[count]; index = 0;
        foreach (var part in visuals)
        {
            if (part?.renderer == null) continue;
            MergeBoxPartAdjustment setting = null;
            if (parts != null) foreach (var existing in parts)
                if (existing != null && existing.renderer == part.renderer) { setting = existing; break; }
            matched[index++] = setting ?? new MergeBoxPartAdjustment { renderer = part.renderer,
                name = part.role + " - " + (part.renderer.transform.parent != null ? part.renderer.transform.parent.name + "/" : "") + part.renderer.gameObject.name };
        }
        parts = matched; return true;
    }
    internal bool PartsChanged()
    {
        foreach (var part in parts) if (part.Changed) return true;
        return false;
    }
    internal void ApplyParts()
    {
        foreach (var setting in parts)
        {
            foreach (var part in box.visuals)
                if (part?.renderer == setting.renderer) { part.ApplyAdjustment(box.transform, setting); break; }
            setting.Remember();
        }
    }
    [NonSerialized] internal Vector2 appliedSize, appliedSpawn, appliedDeadline;
    [NonSerialized] internal Vector4 appliedPadding;
}

// One component on the series root; no changes to active box levels or object scale.
[ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(-400)]
public sealed class MergeBoxVisualController : MonoBehaviour
{
    public MergeBoxVisualSize[] boxes = Array.Empty<MergeBoxVisualSize>();
    private bool refresh = true;
    private void OnEnable() => refresh = true;
    private void OnValidate() => refresh = true;
    private void Update()
    {
        bool changed = false;
        if (boxes != null) foreach (var entry in boxes)
        {
            if (entry?.box == null || !entry.box.transform.IsChildOf(transform)) continue;
            bool partsChanged = entry.SynchronizeParts() || entry.PartsChanged();
            Vector2 size = new Vector2(Mathf.Max(.01f, entry.width), Mathf.Max(.01f, entry.height));
            if (!refresh && !partsChanged && size == entry.appliedSize && entry.spawnOffset == entry.appliedSpawn && entry.deadlineOffset == entry.appliedDeadline && entry.backgroundPadding.Value == entry.appliedPadding) continue;
            if (!entry.box.HasVisualLayout) continue;
            entry.box.ApplyVisualLayout(size, entry.spawnOffset, entry.deadlineOffset, entry.backgroundPadding.Value);
            entry.ApplyParts();
            entry.appliedPadding = entry.backgroundPadding.Value;
            entry.appliedSize = size; entry.appliedSpawn = entry.spawnOffset; entry.appliedDeadline = entry.deadlineOffset;
            changed = true;
        }
        refresh = false;
        if (changed && Application.isPlaying) Physics2D.SyncTransforms();
    }
    [ContextMenu("Capture Current Layout As Baseline")]
    public void CaptureCurrentLayout()
    {
        if (boxes == null) return;
        foreach (var entry in boxes)
            if (entry?.box != null && entry.box.transform.IsChildOf(transform))
                entry.box.CaptureVisualLayout(new Vector2(entry.width, entry.height));
        refresh = true;
    }
    [ContextMenu("Apply Box Sizes")]
    public void ApplySizes() { refresh = true; Update(); }
}
