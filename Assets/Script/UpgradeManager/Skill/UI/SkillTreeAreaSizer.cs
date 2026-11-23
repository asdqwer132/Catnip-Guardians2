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
    [Tooltip("Absolute Pos X / Pos Y of this SkillTreeArea in parent UI units.")]
    [SerializeField] private Vector2 boundsOffset;
    [Tooltip("Padding around each edge of the measured node bounds. Negative values reduce the margin.")]
    [SerializeField] private RectOffset padding = new RectOffset();
    [Tooltip("In play mode, include visible nodes that are unlocked or have met their prerequisites.")]
    [FormerlySerializedAs("unlockedNodesOnlyInPlayMode")]
    [SerializeField] private bool visibleNodesOnlyInPlayMode;

    private SkillTreeManager skillTree;
    private bool fitPending;

    // Preserve the graph origin for layouts fitted by earlier versions.
    [SerializeField, HideInInspector] private Vector2 nodeCenterOffset;

    internal Vector2 BoundsOffset => boundsOffset;

    internal Vector2 GetNodeCenterOffset(RectTransform nodeArea)
        => nodeArea == nodes ? nodeCenterOffset : Vector2.zero;

    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        SkillTreeManager.InstanceChanged += BindSkillTree;
        BindSkillTree();
        fitPending = true;
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
        FitSizes((RectTransform)transform);
    }

    [ContextMenu("Fit Skill Tree Areas")]
    public void FitSkillTreeAreas()
    {
        var area = (RectTransform)transform;
        ApplyPositionOffset(area);
        FitSizes(area);
    }

    private void FitSizes(RectTransform area)
    {
        if (nodes == null || !nodes.IsChildOf(area)) return;
        var origin = nodes.parent as RectTransform;
        bool sharedOrigin = origin != null && origin.parent == area && lines != null && lines.parent == origin;

        var nodeViews = nodes.GetComponentsInChildren<SkillNodeUI>(true);
        var manager = SkillTreeManager.Instance;
        if (Application.isPlaying && visibleNodesOnlyInPlayMode && manager != null)
            nodeViews = System.Array.FindAll(nodeViews, node => node.gameObject.activeInHierarchy && node.IsVisible &&
                (manager.IsUnlocked(node.SkillNodeData) || manager.HasRequiredSkills(node.SkillNodeData)));
        if (!TryMeasure(nodeViews, area, out var bounds)) return;

        var size = bounds.size + new Vector2(padding.left, padding.bottom) + new Vector2(padding.right, padding.top);
        size = Vector2.Max(Vector2.one * .01f, size);
        SetSize(area, size);
        SetSize(nodes, size);
        if (lines != null && lines.parent == nodes.parent && lines != nodes) SetSize(lines, size);

        if (sharedOrigin && TryMeasure(nodeViews, area, out bounds))
        {
            var target = new Vector2(area.rect.xMin + padding.left, area.rect.yMax - padding.top);
            var topLeft = new Vector2(bounds.xMin, bounds.yMax);
            MoveLayoutOrigin(origin, target - topLeft);
        }
    }

    private static void MoveLayoutOrigin(RectTransform origin, Vector2 delta)
    {
        if (delta.sqrMagnitude < .0001f) return;
#if UNITY_EDITOR
        if (!Application.isPlaying) Undo.RecordObject(origin, "Align Skill Tree Padding");
#endif
        origin.anchoredPosition += delta;
#if UNITY_EDITOR
        if (!Application.isPlaying) PrefabUtility.RecordPrefabInstancePropertyModifications(origin);
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

    private void ApplyPositionOffset(RectTransform area)
    {
        if (area.anchoredPosition == boundsOffset) return;
#if UNITY_EDITOR
        if (!Application.isPlaying) Undo.RecordObject(area, "Set Skill Tree Area Position");
#endif
        area.anchoredPosition = boundsOffset;
#if UNITY_EDITOR
        if (!Application.isPlaying) PrefabUtility.RecordPrefabInstancePropertyModifications(area);
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
