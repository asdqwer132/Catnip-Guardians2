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
    [Tooltip("Layer containing the skill nodes under the shared layout parent.")]
    [SerializeField] private RectTransform nodes;
    [Tooltip("Layer sharing the same parent as Nodes.")]
    [SerializeField] private RectTransform lines;

    [Header("Fit Bounds")]
    [Tooltip("Space around the measured nodes inside the scroll content.")]
    [SerializeField] private RectOffset padding = new RectOffset();
    [Tooltip("Extra scroll space per edge. Moves the shared layout to make room.")]
    [SerializeField] private RectOffset layoutPadding = new RectOffset();
    [SerializeField, HideInInspector] private Vector2 appliedLayoutShift;
    [Tooltip("In play mode, include visible nodes that are unlocked or have met their prerequisites.")]
    [FormerlySerializedAs("unlockedNodesOnlyInPlayMode")]
    [SerializeField] private bool visibleNodesOnlyInPlayMode;

    private SkillTreeManager skillTree;
    private bool fitPending;

    // Preserve the graph origin for layouts fitted by earlier versions.
    [SerializeField, HideInInspector] private Vector2 nodeCenterOffset;

    [SerializeField, HideInInspector] private Vector2 nodeAlignmentOffset;

    internal Vector2 GetNodePlacementOffset(RectTransform nodeArea, bool usePivot)
        => nodeArea == nodes ? nodeAlignmentOffset + (usePivot ? Vector2.zero : nodeCenterOffset) : Vector2.zero;

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

    private void RequestFit()
    {
        if (visibleNodesOnlyInPlayMode && skillTree != null) fitPending = true;
    }

    private void LateUpdate()
    {
        if (fitPending) FitSizes(applyLayoutPadding: false);
    }

    [ContextMenu("Fit Skill Tree Areas")]
    public void FitSkillTreeAreas() => FitSizes(applyLayoutPadding: true);

    private void FitSizes(bool applyLayoutPadding)
    {
        fitPending = false;
        var area = (RectTransform)transform;
        if (nodes == null || nodes == area || !nodes.IsChildOf(area)) return;
        var origin = nodes.parent as RectTransform;
        bool sharedLayers = origin != null && lines != null && lines != nodes && lines.parent == origin;

        var nodeViews = nodes.GetComponentsInChildren<SkillNodeUI>(true);
        var manager = SkillTreeManager.Instance;
        if (Application.isPlaying && visibleNodesOnlyInPlayMode && manager != null)
            nodeViews = System.Array.FindAll(nodeViews, node => node.gameObject.activeInHierarchy && node.IsVisible &&
                (manager.IsUnlocked(node.SkillNodeData) || manager.HasRequiredSkills(node.SkillNodeData)));
        if (!TryMeasure(nodeViews, area, out var bounds)) return;

        var paddingSize = new Vector2((float)padding.left + padding.right, (float)padding.top + padding.bottom);
        var size = Vector2.Max(Vector2.one * .01f, bounds.size + paddingSize);
        bool canMoveLayout = sharedLayers && origin.parent == area;
        var extra = canMoveLayout
            ? new Vector2((float)layoutPadding.left + layoutPadding.right, (float)layoutPadding.bottom + layoutPadding.top)
            : Vector2.zero;
        var previousPosition = canMoveLayout ? origin.localPosition : Vector3.zero;
        SetSize(area, Vector2.Max(Vector2.one * .01f, size + extra));
        SetSize(nodes, size);
        if (sharedLayers) SetSize(lines, size);
        if (canMoveLayout && applyLayoutPadding)
        {
            var shift = new Vector2(layoutPadding.left, layoutPadding.bottom) - Vector2.Scale(area.pivot, extra);
            if (shift != Vector2.zero || appliedLayoutShift != Vector2.zero)
                ApplyLayoutShift(origin, previousPosition, shift);
        }
    }

    [ContextMenu("Fit Nodes Inside Rectangle")]
    private void FitNodesInsideRectangle()
    {
        if (nodes == null) return;
        var views = nodes.GetComponentsInChildren<SkillNodeUI>(true);
        if (!TryMeasure(views, nodes, out var bounds)) return;

        var rect = nodes.rect;
        var delta = new Vector2(
            FitAxis(rect.xMin + padding.left - bounds.xMin, rect.xMax - padding.right - bounds.xMax),
            FitAxis(rect.yMin + padding.bottom - bounds.yMin, rect.yMax - padding.top - bounds.yMax));
        if (delta.sqrMagnitude < .0001f) return;
#if UNITY_EDITOR
        if (!Application.isPlaying) Undo.RecordObject(this, "Fit Nodes Inside Rectangle");
#endif
        nodeAlignmentOffset += delta;
#if UNITY_EDITOR
        if (!Application.isPlaying) PrefabUtility.RecordPrefabInstancePropertyModifications(this);
#endif

        var groups = new System.Collections.Generic.HashSet<Transform>();
        foreach (var view in views)
        {
            var child = view.transform;
            if (child == nodes) continue;
            while (child.parent != nodes) child = child.parent;
            groups.Add(child);
        }

        foreach (var child in groups)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) Undo.RecordObject(child, "Fit Nodes Inside Rectangle");
#endif
            child.localPosition += (Vector3)delta;
#if UNITY_EDITOR
            if (!Application.isPlaying) PrefabUtility.RecordPrefabInstancePropertyModifications(child);
#endif
        }

        if (lines != null && lines != nodes && lines.parent == nodes.parent)
            foreach (var connection in lines.GetComponentsInChildren<SkillTreeConnectionUI>(true))
                connection.RefreshGeometry(recordUndo: true);
    }

    private static float FitAxis(float minimumShift, float maximumShift)
        => minimumShift <= maximumShift
            ? Mathf.Clamp(0f, minimumShift, maximumShift)
            : (minimumShift + maximumShift) * .5f;

    private void ApplyLayoutShift(RectTransform origin, Vector3 previousPosition, Vector2 shift)
    {
        var position = previousPosition + (Vector3)(shift - appliedLayoutShift);
        if (origin.localPosition == position && appliedLayoutShift == shift) return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Undo.RecordObject(this, "Set Skill Tree Layout Padding");
            Undo.RecordObject(origin, "Set Skill Tree Layout Padding");
        }
#endif
        origin.localPosition = position;
        appliedLayoutShift = shift;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            PrefabUtility.RecordPrefabInstancePropertyModifications(origin);
        }
#endif
    }

    private static void SetSize(RectTransform rect, Vector2 size)
    {
        if ((rect.rect.size - size).sqrMagnitude < .0001f) return;
#if UNITY_EDITOR
        if (!Application.isPlaying) Undo.RecordObject(rect, "Fit Skill Tree Areas");
#endif
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
#if UNITY_EDITOR
        if (!Application.isPlaying) PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
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
}
