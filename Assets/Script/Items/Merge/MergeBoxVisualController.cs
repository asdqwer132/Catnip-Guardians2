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

    public void Capture(Transform box)
    {
        if (renderer == null) return;
        originalSize = renderer.size;
        position = box.InverseTransformPoint(renderer.transform.position);
        units = Units(box, renderer.transform);
        if (wallCollider == null) return;
        colliderSize = wallCollider.size;
        colliderPosition = box.InverseTransformPoint(wallCollider.transform.position);
        colliderUnits = Units(box, wallCollider.transform);
    }
    private static Vector2 Units(Transform box, Transform part) => new Vector2(
        Mathf.Max(.0001f, box.InverseTransformVector(part.TransformVector(Vector3.right)).magnitude),
        Mathf.Max(.0001f, box.InverseTransformVector(part.TransformVector(Vector3.up)).magnitude));
    public void Apply(Transform box, Vector2 delta, Vector2 ratio)
    {
        if (renderer == null || role == MergeBoxSpriteRole.Fixed) return;
        Vector2 resize = role == MergeBoxSpriteRole.SideWall ? new Vector2(0, delta.y) :
            role == MergeBoxSpriteRole.BottomWall ? new Vector2(delta.x, 0) : delta;
        renderer.size = Vector2.Max(Vector2.one * .01f, originalSize + resize / units);
        Place(box, renderer.transform, position, ratio);
        if (wallCollider == null) return;
        wallCollider.size = Vector2.Max(Vector2.one * .01f, colliderSize + resize / colliderUnits);
        Place(box, wallCollider.transform, colliderPosition, ratio);
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
            Vector2 size = new Vector2(Mathf.Max(.01f, entry.width), Mathf.Max(.01f, entry.height));
            if (!refresh && size == entry.appliedSize && entry.spawnOffset == entry.appliedSpawn && entry.deadlineOffset == entry.appliedDeadline && entry.backgroundPadding.Value == entry.appliedPadding) continue;
            if (!entry.box.HasVisualLayout) continue;
            entry.box.ApplyVisualLayout(size, entry.spawnOffset, entry.deadlineOffset, entry.backgroundPadding.Value);
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
