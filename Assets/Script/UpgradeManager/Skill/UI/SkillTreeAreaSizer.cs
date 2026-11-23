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

    [Header("Fit Bounds")]
    [Tooltip("Moves this SkillTreeArea and its contents in parent UI units. Positive X goes right and positive Y goes up.")]
    [SerializeField] private Vector2 boundsOffset;
    [Tooltip("Horizontal and vertical padding totals adjust the size around the existing pivot. Negative values reduce the size.")]
    [SerializeField] private RectOffset padding = new RectOffset();
    [Tooltip("In play mode, include visible nodes that are unlocked or have met their prerequisites.")]
    [FormerlySerializedAs("unlockedNodesOnlyInPlayMode")]
    [SerializeField] private bool visibleNodesOnlyInPlayMode;

    private SkillTreeManager skillTree;
    private bool fitPending;

    // Preserve the graph origin for layouts fitted by earlier versions.
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
        if (nodes == null || nodes.parent != area) return;

        ApplyPositionOffset(area);

        var nodeViews = nodes.GetComponentsInChildren<SkillNodeUI>(true);
        var manager = SkillTreeManager.Instance;
        if (Application.isPlaying && visibleNodesOnlyInPlayMode && manager != null)
            nodeViews = System.Array.FindAll(nodeViews, node => node.gameObject.activeInHierarchy && node.IsVisible &&
                (manager.IsUnlocked(node.SkillNodeData) || manager.HasRequiredSkills(node.SkillNodeData)));
        if (!TryMeasure(nodeViews, area, out var size)) return;

        size += new Vector2(padding.left, padding.bottom) + new Vector2(padding.right, padding.top);
        size = Vector2.Max(Vector2.one * .01f, size);
        if ((area.rect.size - size).sqrMagnitude < .0001f) return;

#if UNITY_EDITOR
        if (!Application.isPlaying) Undo.RecordObject(area, "Fit Skill Tree Area");
#endif

        area.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        area.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);

#if UNITY_EDITOR
        if (!Application.isPlaying) PrefabUtility.RecordPrefabInstancePropertyModifications(area);
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

    private bool TryMeasure(SkillNodeUI[] views, RectTransform space, out Vector2 size)
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

        size = max - min;
        return float.IsFinite(size.x) && float.IsFinite(size.y);
    }
}
