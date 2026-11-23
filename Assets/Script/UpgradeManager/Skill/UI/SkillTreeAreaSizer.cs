using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[AddComponentMenu("Layout/Skill Tree Area Sizer")]
public sealed class SkillTreeAreaSizer : MonoBehaviour
{
    [Tooltip("Direct child containing the skill nodes.")]
    [SerializeField] private RectTransform nodes;
    [Tooltip("Direct child containing the connection lines.")]
    [SerializeField] private RectTransform lines;

    [ContextMenu("Fit Skill Tree Areas")]
    public void FitSkillTreeAreas()
    {
        var area = (RectTransform)transform;
        if (nodes == null || lines == null || nodes == lines || nodes.parent != area || lines.parent != area)
            return;

        var bounds = new Rect();
        bool hasNodes = false;
        var corners = new Vector3[4];
        foreach (var node in nodes.GetComponentsInChildren<SkillNodeUI>(true))
        {
            if (node.transform == nodes || node.transform is not RectTransform rect) continue;
            rect.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                Vector2 point = nodes.InverseTransformPoint(corner);
                if (!hasNodes)
                {
                    bounds = new Rect(point, Vector2.zero);
                    hasNodes = true;
                    continue;
                }
                bounds = Rect.MinMaxRect(Mathf.Min(bounds.xMin, point.x), Mathf.Min(bounds.yMin, point.y),
                    Mathf.Max(bounds.xMax, point.x), Mathf.Max(bounds.yMax, point.y));
            }
        }

        if (!hasNodes || !float.IsFinite(bounds.width) || !float.IsFinite(bounds.height)) return;
        Vector2 size = bounds.size;
        if (area.rect.size == size && nodes.rect.size == size && lines.rect.size == size) return;

        var nodesPosition = nodes.localPosition;
        var linesPosition = lines.localPosition;
        var placements = new List<ChildPlacement>(nodes.childCount + lines.childCount);
        CaptureChildren(nodes, placements);
        CaptureChildren(lines, placements);

#if UNITY_EDITOR
        var edited = new List<Object> { area, nodes, lines };
        foreach (var placement in placements) edited.Add(placement.Transform);
        if (!Application.isPlaying) Undo.RecordObjects(edited.ToArray(), "Fit Skill Tree Areas");
#endif

        SetSize(area, size);
        SetSize(nodes, size);
        SetSize(lines, size);
        // Stretch anchors can shift local positions when an ancestor changes size.
        nodes.localPosition = nodesPosition;
        lines.localPosition = linesPosition;
        foreach (var placement in placements) placement.Restore();

#if UNITY_EDITOR
        if (!Application.isPlaying)
            foreach (var item in edited)
                if (PrefabUtility.IsPartOfPrefabInstance(item))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(item);
#endif
    }

    private static void CaptureChildren(RectTransform root, List<ChildPlacement> placements)
    {
        foreach (Transform child in root) placements.Add(new ChildPlacement(child));
    }

    private static void SetSize(RectTransform rect, Vector2 size)
    {
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
    }

    private readonly struct ChildPlacement
    {
        public Transform Transform { get; }
        private readonly Vector3 position;
        private readonly Vector2 size;

        public ChildPlacement(Transform child)
        {
            Transform = child;
            position = child.localPosition;
            size = child is RectTransform rect ? rect.rect.size : Vector2.zero;
        }

        public void Restore()
        {
            if (Transform is RectTransform rect) SetSize(rect, size);
            Transform.localPosition = position;
        }
    }
}
