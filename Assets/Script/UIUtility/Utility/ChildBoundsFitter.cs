using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[AddComponentMenu("Layout/Child Bounds Fitter")]
public class ChildBoundsFitter : MonoBehaviour
{
    [Tooltip("Padding in local UI units. Negative values inset the corresponding edge.")]
    [SerializeField] private RectOffset padding = new RectOffset();

    [Tooltip("Measure only this child subtree. Leave empty to measure the entire hierarchy.")]
    [SerializeField] private RectTransform measurementRoot;

    protected RectTransform MeasurementRoot => measurementRoot != null && measurementRoot != transform && measurementRoot.IsChildOf(transform)
        ? measurementRoot : null;

    protected virtual IEnumerable<RectTransform> GetMeasuredRects()
    {
        if (measurementRoot != null && MeasurementRoot == null) return System.Array.Empty<RectTransform>();
        var root = MeasurementRoot != null ? MeasurementRoot : transform;
        return root.GetComponentsInChildren<RectTransform>(true);
    }

    protected virtual bool FitEmptyBounds => false;

    [ContextMenu("Fit To Child Bounds")]
    public void FitToChildBounds() => FitToBounds(GetMeasuredRects());

    protected void FitToBounds(IEnumerable<RectTransform> rects)
    {
        var parent = (RectTransform)transform;
        var corners = new Vector3[4];
        var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

        var root = MeasurementRoot;
        foreach (var descendant in rects)
        {
            if (descendant == null || descendant == parent || descendant == root) continue;
            descendant.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                Vector2 point = parent.InverseTransformPoint(corner);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
        }
        if (!float.IsFinite(min.x) || !float.IsFinite(min.y) ||
            !float.IsFinite(max.x) || !float.IsFinite(max.y))
        {
            if (!FitEmptyBounds) return;
            min = max = Vector2.zero;
        }

        min -= new Vector2(padding.left, padding.bottom);
        max += new Vector2(padding.right, padding.top);
        var size = Vector2.Max(Vector2.zero, max - min);
        var offset = (min + max) * .5f + Vector2.Scale(size, parent.pivot - Vector2.one * .5f);
        if ((parent.rect.size - size).sqrMagnitude < .000001f && offset.sqrMagnitude < .000001f) return;

        // Keep direct children fixed in world space. Preserving their rectangles
        // also keeps stretched anchors throughout their descendants stable.
        var children = new List<(Transform transform, Vector3 position, Vector2 size)>(parent.childCount);
        foreach (Transform child in parent)
            children.Add((child, child.localPosition, child is RectTransform rect ? rect.rect.size : Vector2.zero));

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var targets = new Object[children.Count + 1];
            targets[0] = parent;
            for (int i = 0; i < children.Count; i++) targets[i + 1] = children[i].transform;
            Undo.RecordObjects(targets, "Fit To Child Bounds");
        }
#endif

        var pivotPosition = parent.TransformPoint(offset);
        SetSize(parent, size);
        parent.position = pivotPosition;
        foreach (var child in children)
        {
            if (child.transform is RectTransform rect) SetSize(rect, child.size);
            child.transform.localPosition = child.position - (Vector3)offset;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(parent);
            foreach (var child in children)
                PrefabUtility.RecordPrefabInstancePropertyModifications(child.transform);
        }
#endif
    }

    private static void SetSize(RectTransform rect, Vector2 size)
    {
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
    }
}
