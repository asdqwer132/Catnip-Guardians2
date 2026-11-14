using System;
using UnityEngine;

public enum MergeBoxSpriteRole { SideWall, BottomWall, BackFrame, Fixed, LeftWall, RightWall }

[Serializable]
public sealed class MergeBoxSpritePart
{
    public MergeBoxSpriteRole role;
    public GameObject target;
    public SpriteRenderer renderer => target != null ? target.GetComponent<SpriteRenderer>() : null;
    public BoxCollider2D wallCollider => target != null ? target.GetComponent<BoxCollider2D>() : null;
    [HideInInspector] public Vector2 originalSize;
    [HideInInspector] public Vector3 position;
    [HideInInspector] public Vector2 units, colliderUnits;

    [NonSerialized] private bool colliderFitted;
    [NonSerialized] private Sprite fittedSprite;
    [NonSerialized] private Bounds fittedBounds;
    [NonSerialized] private Matrix4x4 fittedVisualMatrix, fittedColliderMatrix;
    [NonSerialized] private Vector2 fittedSize, fittedOffset;
    public bool ColliderNeedsFit => renderer != null && renderer.sprite != null && wallCollider != null &&
        (!colliderFitted || fittedSprite != renderer.sprite || fittedBounds != renderer.localBounds ||
         fittedVisualMatrix != renderer.transform.localToWorldMatrix || fittedColliderMatrix != wallCollider.transform.localToWorldMatrix ||
         fittedSize != wallCollider.size || fittedOffset != wallCollider.offset || wallCollider.autoTiling);

    public void Capture(Transform box)
    {
        if (renderer == null) return;
        originalSize = renderer.size;
        position = box.InverseTransformPoint(renderer.transform.position);
        units = Units(box, renderer.transform);
        if (wallCollider == null) return;
        colliderUnits = Units(box, wallCollider.transform);
    }
    private static Vector2 Units(Transform box, Transform part) => new Vector2(
        Mathf.Max(.0001f, box.InverseTransformVector(part.TransformVector(Vector3.right)).magnitude),
        Mathf.Max(.0001f, box.InverseTransformVector(part.TransformVector(Vector3.up)).magnitude));
    public void Apply(Transform box, Vector2 delta, Vector2 ratio)
    {
        if (renderer == null) return;
        Vector2 resize = role == MergeBoxSpriteRole.Fixed ? Vector2.zero : (role == MergeBoxSpriteRole.SideWall || role == MergeBoxSpriteRole.LeftWall || role == MergeBoxSpriteRole.RightWall) ? new Vector2(0, delta.y) :
            role == MergeBoxSpriteRole.BottomWall ? new Vector2(delta.x, 0) : delta;
        Vector2 positionRatio = role == MergeBoxSpriteRole.Fixed ? Vector2.one : new Vector2(ratio.x, 1);
        Place(box, renderer.transform, position, positionRatio);
        // Rebuild from the captured size so repeated edits never accumulate a shift.
        // Keep the visual's lower edge fixed, regardless of its Sprite Editor pivot.
        renderer.size = originalSize;
        float bottom = BottomInBox(box);
        renderer.size = Vector2.Max(Vector2.one * .01f, originalSize + resize / units);
        float lift = bottom - BottomInBox(box);
        renderer.transform.position += box.TransformVector(new Vector3(0, lift, 0));
    }

    private float BottomInBox(Transform box)
    {
        Bounds bounds = renderer.localBounds;
        float bottom = float.PositiveInfinity;
        for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++)
        {
            Vector3 corner = renderer.transform.TransformPoint(new Vector3(x == 0 ? bounds.min.x : bounds.max.x,
                y == 0 ? bounds.min.y : bounds.max.y, bounds.center.z));
            bottom = Mathf.Min(bottom, box.InverseTransformPoint(corner).y);
        }
        return bottom;
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
        Vector3 offset = box.TransformVector(adjustment != null ? adjustment.positionOffset : Vector2.zero);
        renderer.transform.position += offset;
        FitCollider(box, adjustment != null ? adjustment.colliderPadding.Value : Vector4.zero);
    }
    public void FitCollider(Transform box, Vector4 padding)
    {
        if (wallCollider == null || renderer == null || renderer.sprite == null) return;
        Transform colliderTransform = wallCollider.transform;
        // Match the Visual object's transform, preserving the visual when the collider
        // is on its parent. Padding is then measured from the current sprite bounds.
        Vector3 visualPosition = renderer.transform.position;
        Quaternion visualRotation = renderer.transform.rotation;
        if (colliderTransform != renderer.transform && !box.IsChildOf(colliderTransform))
        {
            colliderTransform.SetPositionAndRotation(visualPosition, visualRotation);
            renderer.transform.SetPositionAndRotation(visualPosition, visualRotation);
        }
        if (wallCollider.autoTiling) wallCollider.autoTiling = false;
        Bounds bounds = renderer.localBounds;
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        for (int cornerX = 0; cornerX < 2; cornerX++) for (int cornerY = 0; cornerY < 2; cornerY++)
        {
            Vector3 corner = renderer.transform.TransformPoint(new Vector3(cornerX == 0 ? bounds.min.x : bounds.max.x,
                cornerY == 0 ? bounds.min.y : bounds.max.y, bounds.center.z));
            Vector2 local = colliderTransform.InverseTransformPoint(corner);
            min = Vector2.Min(min, local); max = Vector2.Max(max, local);
        }
        wallCollider.size = Vector2.Max(Vector2.one * .01f, max - min);
        wallCollider.offset = (min + max) * .5f;
        colliderUnits = Units(box, colliderTransform);

        Vector2 available = Vector2.Max(Vector2.zero, (wallCollider.size - Vector2.one * .01f) * colliderUnits);
        float horizontal = padding.x + padding.z, vertical = padding.y + padding.w;
        float x = horizontal > 0 ? Mathf.Min(1, available.x / horizontal) : 0;
        float y = vertical > 0 ? Mathf.Min(1, available.y / vertical) : 0;
        padding = new Vector4(padding.x * x, padding.y * y, padding.z * x, padding.w * y);
        wallCollider.size -= new Vector2((padding.x + padding.z) / colliderUnits.x, (padding.y + padding.w) / colliderUnits.y);
        wallCollider.offset += (Vector2)colliderTransform.InverseTransformVector(box.TransformVector(
            new Vector3((padding.x - padding.z) * .5f, (padding.y - padding.w) * .5f, 0)));
        colliderFitted = true; fittedSprite = renderer.sprite; fittedBounds = renderer.localBounds;
        fittedVisualMatrix = renderer.transform.localToWorldMatrix; fittedColliderMatrix = colliderTransform.localToWorldMatrix;
        fittedSize = wallCollider.size; fittedOffset = wallCollider.offset;
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
    public MergeBoxSpriteRole role;
    [Tooltip("Common box-local position offset for this role in every box.")]
    public Vector2 positionOffset;
    public MergeBoxColliderPadding colliderPadding;
    [NonSerialized] private bool applied;
    [NonSerialized] private MergeBoxSpriteRole appliedRole;
    [NonSerialized] private Vector2 appliedOffset;
    [NonSerialized] private Vector4 appliedPadding;
    internal bool Changed => !applied || role != appliedRole || positionOffset != appliedOffset || colliderPadding.Value != appliedPadding;
    internal void Remember() { applied = true; appliedRole = role; appliedOffset = positionOffset; appliedPadding = colliderPadding.Value; }
}

[Serializable]
public sealed class MergeBoxVisualSize
{
    public MergeBoxBindings box;
    [Min(.01f)] public float width = 4.5f;
    [Min(.01f)] public float height = 5.5f;
    public Vector2 spawnOffset;
    public Vector2 deadlineOffset;
    [NonSerialized] internal MergeBoxBindings appliedBox;
    [NonSerialized] internal Vector2 appliedSize, appliedSpawn, appliedDeadline;
}

// One component on the series root; no changes to active box levels or object scale.
[ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(-400)]
public sealed class MergeBoxVisualController : MonoBehaviour
{
    [Header("Common settings for every box")]
    public MergeBoxBackgroundPadding backgroundPadding;
    [Tooltip("Matched by role from each MergeBoxBindings; no duplicate object references are needed here.")]
    public MergeBoxPartAdjustment[] parts = {
        new MergeBoxPartAdjustment { role = MergeBoxSpriteRole.LeftWall },
        new MergeBoxPartAdjustment { role = MergeBoxSpriteRole.RightWall },
        new MergeBoxPartAdjustment { role = MergeBoxSpriteRole.BottomWall },
        new MergeBoxPartAdjustment { role = MergeBoxSpriteRole.BackFrame }
    };
    [Header("Individual box sizes and drop positions")]
    public MergeBoxVisualSize[] boxes = Array.Empty<MergeBoxVisualSize>();
    private bool refresh = true;
    private Vector4 appliedPadding;
    private MergeBoxPartAdjustment[] appliedParts;
    private int appliedPartCount;
    private void OnEnable() => refresh = true;
    private void OnValidate() => refresh = true;
    private MergeBoxPartAdjustment SettingsFor(MergeBoxSpriteRole role)
    {
        if (parts != null) foreach (var setting in parts)
            if (setting != null && setting.role == role) return setting;
        return null;
    }
    private void Update()
    {
        bool commonChanged = refresh || appliedPadding != backgroundPadding.Value || appliedParts != parts || appliedPartCount != (parts?.Length ?? 0);
        if (parts != null) foreach (var setting in parts)
            if (setting != null && setting.Changed) commonChanged = true;
        bool changed = false;
        if (boxes != null) foreach (var entry in boxes)
        {
            if (entry?.box == null || !entry.box.transform.IsChildOf(transform)) continue;
            Vector2 size = new Vector2(Mathf.Max(.01f, entry.width), Mathf.Max(.01f, entry.height));
            if (!entry.box.HasVisualLayout) continue;
            bool apply = commonChanged || entry.box != entry.appliedBox || size != entry.appliedSize ||
                entry.spawnOffset != entry.appliedSpawn || entry.deadlineOffset != entry.appliedDeadline;
            if (apply)
            {
                entry.box.ApplyVisualLayout(size, entry.spawnOffset, entry.deadlineOffset, backgroundPadding.Value);
                entry.appliedBox = entry.box; entry.appliedSize = size;
                entry.appliedSpawn = entry.spawnOffset; entry.appliedDeadline = entry.deadlineOffset;
                changed = true;
            }
            foreach (var part in entry.box.visuals)
            {
                if (part?.renderer == null) continue;
                var setting = SettingsFor(part.role);
                if (apply) part.ApplyAdjustment(entry.box.transform, setting);
                else if (part.ColliderNeedsFit)
                {
                    part.FitCollider(entry.box.transform, setting != null ? setting.colliderPadding.Value : Vector4.zero);
                    changed = true;
                }
            }
        }
        if (parts != null) foreach (var setting in parts) setting?.Remember();
        appliedParts = parts; appliedPartCount = parts?.Length ?? 0;
        appliedPadding = backgroundPadding.Value; refresh = false;
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
