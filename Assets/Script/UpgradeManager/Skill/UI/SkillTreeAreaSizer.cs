using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
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

    [Header("Fit Bounds")]
    [Tooltip("Moves this SkillTreeArea and its contents in parent UI units. Positive X goes right and positive Y goes up.")]
    [SerializeField] private Vector2 boundsOffset;
    [Tooltip("Local UI units per edge. Negative padding insets the edge.")]
    [SerializeField] private RectOffset padding = new RectOffset();
    [Tooltip("In play mode, include visible nodes that are unlocked or have met their prerequisites.")]
    [FormerlySerializedAs("unlockedNodesOnlyInPlayMode")]
    [SerializeField] private bool visibleNodesOnlyInPlayMode;

    private SkillTreeManager skillTree;
    private bool fitPending;

    [SerializeField, HideInInspector] private Vector2 nodeCenterOffset;
    [SerializeField, HideInInspector] private Vector2 appliedBoundsOffset;

    internal Vector2 AppliedBoundsOffset => appliedBoundsOffset;

    internal Vector2 GetNodeCenterOffset(RectTransform nodeArea)
        => nodeArea == nodes ? nodeCenterOffset : Vector2.zero;

    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        SkillTreeManager.InstanceChanged += BindSkillTree;
        BindSkillTree();
    }

    private void OnDisable()
    {
        SkillTreeManager.InstanceChanged -= BindSkillTree;
        if (skillTree != null) skillTree.OnSkillTreeChanged -= RequestFit;
        skillTree = null;
        fitPending = false;
    }

    private void BindSkillTree()
    {
        if (skillTree != null) skillTree.OnSkillTreeChanged -= RequestFit;
        skillTree = SkillTreeManager.Instance;
        if (skillTree != null) skillTree.OnSkillTreeChanged += RequestFit;
        RequestFit();
    }

    private void RequestFit() => fitPending = visibleNodesOnlyInPlayMode && skillTree != null;

    private void LateUpdate()
    {
        if (!fitPending) return;
        fitPending = false;
        if (visibleNodesOnlyInPlayMode) FitSkillTreeAreas();
    }

    [ContextMenu("Fit Skill Tree Areas")]
    public void FitSkillTreeAreas()
    {
        var area = (RectTransform)transform;
        if (nodes == null || lines == null || nodes == lines || nodes.parent != area || lines.parent != area)
            return;

        ApplyPositionOffset(area);

        var nodeViews = nodes.GetComponentsInChildren<SkillNodeUI>(true);
        var manager = SkillTreeManager.Instance;
        if (Application.isPlaying && visibleNodesOnlyInPlayMode && manager != null)
            nodeViews = System.Array.FindAll(nodeViews, node => node.gameObject.activeInHierarchy && node.IsVisible &&
                (manager.IsUnlocked(node.SkillNodeData) || manager.HasRequiredSkills(node.SkillNodeData)));
        if (!TryMeasure(nodeViews, area, out var areaBounds) ||
            !TryMeasure(nodeViews, nodes, out var nodesBounds) ||
            !TryMeasure(nodeViews, lines, out var linesBounds)) return;
        areaBounds = AdjustBounds(areaBounds);
        nodesBounds = AdjustBounds(nodesBounds);
        linesBounds = AdjustBounds(linesBounds);
        if (Matches(area.rect, areaBounds) && Matches(nodes.rect, nodesBounds) && Matches(lines.rect, linesBounds)) return;

        var previousNodeCenter = nodes.rect.center;
        var areaPosition = area.localPosition;
        var nodesPosition = nodes.localPosition;
        var linesPosition = lines.localPosition;
        var placements = new List<ChildPlacement>(nodes.childCount + lines.childCount);
        CaptureChildren(nodes, placements);
        CaptureChildren(lines, placements);

#if UNITY_EDITOR
        var edited = new List<Object> { this, area, nodes, lines };
        foreach (var placement in placements) edited.Add(placement.Transform);
        if (!Application.isPlaying) Undo.RecordObjects(edited.ToArray(), "Fit Skill Tree Areas");
#endif

        FitRect(area, areaBounds);
        FitRect(nodes, nodesBounds);
        FitRect(lines, linesBounds);
        // Keep the authored coordinate origin stable even if the rectangle center moves.
        nodeCenterOffset += previousNodeCenter - nodes.rect.center;
        area.localPosition = areaPosition;
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

    private void ApplyPositionOffset(RectTransform area)
    {
        if (appliedBoundsOffset == boundsOffset) return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Undo.RecordObject(this, "Offset Skill Tree Area");
            Undo.RecordObject(area, "Offset Skill Tree Area");
        }
#endif
        // Apply the difference so unlock events and repeated fits do not accumulate movement.
        area.anchoredPosition += boundsOffset - appliedBoundsOffset;
        appliedBoundsOffset = boundsOffset;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            PrefabUtility.RecordPrefabInstancePropertyModifications(area);
        }
#endif
    }

    private bool TryMeasure(SkillNodeUI[] views, RectTransform space, out Rect bounds)
    {
        var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        var corners = new Vector3[4];
        foreach (var node in views)
        {
            if (node.transform == nodes || node.transform is not RectTransform rect) continue;
            rect.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                Vector2 point = space.InverseTransformPoint(corner);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
        }

        bounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return float.IsFinite(bounds.width) && float.IsFinite(bounds.height);
    }

    private Rect AdjustBounds(Rect bounds)
    {
        var min = bounds.min - new Vector2(padding.left, padding.bottom);
        var max = bounds.max + new Vector2(padding.right, padding.top);
        var center = (min + max) * .5f;
        var size = Vector2.Max(Vector2.one * .01f, max - min);
        return new Rect(center - size * .5f, size);
    }

    private static bool Matches(Rect current, Rect desired)
        => (current.min - desired.min).sqrMagnitude < .0001f &&
           (current.max - desired.max).sqrMagnitude < .0001f;

    private static void FitRect(RectTransform rect, Rect bounds)
    {
        // Align both edges around the current transform position by shifting the pivot.
        rect.pivot = new Vector2(
            bounds.width > .0001f ? -bounds.xMin / bounds.width : rect.pivot.x,
            bounds.height > .0001f ? -bounds.yMin / bounds.height : rect.pivot.y);
        SetSize(rect, bounds.size);
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
