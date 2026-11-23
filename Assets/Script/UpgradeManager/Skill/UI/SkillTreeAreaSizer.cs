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
    [Tooltip("In play mode, include visible nodes that are unlocked or have met their prerequisites.")]
    [FormerlySerializedAs("unlockedNodesOnlyInPlayMode")]
    [SerializeField] private bool visibleNodesOnlyInPlayMode;

    private SkillTreeManager skillTree;
    private bool fitPending;

    // Preserve the graph origin for layouts fitted by earlier versions.
    [SerializeField, HideInInspector] private Vector2 nodeCenterOffset;

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

    private void RequestFit()
    {
        if (visibleNodesOnlyInPlayMode && skillTree != null) fitPending = true;
    }

    private void LateUpdate()
    {
        if (fitPending) FitSkillTreeAreas();
    }

    [ContextMenu("Fit Skill Tree Areas")]
    public void FitSkillTreeAreas()
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
        if (TryMeasure(nodeViews, area, out var bounds))
        {
            bool canAlign = sharedLayers && origin.parent == area;
            var lower = new Vector2(padding.left, padding.bottom);
            var upper = new Vector2(padding.right, padding.top);
            var size = Vector2.Max(Vector2.one * .01f, bounds.size + lower + upper);
            SetSize(area, size);
            FitNodeRect(nodeViews, lower, size);
            if (sharedLayers) SetSize(lines, size);

            // Stretch anchors may move the layers on resize. Measure again before aligning.
            if (canAlign && TryMeasure(nodeViews, area, out bounds))
                SetLayoutOrigin(origin, origin.anchoredPosition + (area.rect.min + lower - bounds.min));
        }

        if (!sharedLayers) return;
        foreach (var connection in lines.GetComponentsInChildren<SkillTreeConnectionUI>(true))
            connection.RefreshGeometry(recordUndo: true);
    }

    private void FitNodeRect(SkillNodeUI[] views, Vector2 paddingBefore, Vector2 size)
    {
        if (!TryMeasure(views, nodes, out var bounds)) return;
        var min = bounds.min - paddingBefore;
        var pivot = new Vector2(-min.x / size.x, -min.y / size.y);
        if ((nodes.pivot - pivot).sqrMagnitude < .00000001f && (nodes.rect.size - size).sqrMagnitude < .0001f) return;

        var position = nodes.localPosition;
        var center = nodes.rect.center;
        var children = new (Transform transform, Vector3 position, Vector2 size)[nodes.childCount];
        for (int i = 0; i < children.Length; i++)
        {
            var child = nodes.GetChild(i);
            children[i] = (child, child.localPosition, child is RectTransform rect ? rect.rect.size : Vector2.zero);
        }
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Undo.RecordObject(this, "Fit Skill Node Bounds");
            Undo.RecordObject(nodes, "Fit Skill Node Bounds");
            foreach (var child in children) Undo.RecordObject(child.transform, "Preserve Skill Node Layout");
        }
#endif
        nodes.pivot = pivot;
        SetSize(nodes, size);
        nodes.localPosition = position;
        nodeCenterOffset += center - nodes.rect.center;
        foreach (var child in children)
        {
            if (child.transform is RectTransform rect) SetSize(rect, child.size);
            child.transform.localPosition = child.position;
#if UNITY_EDITOR
            if (!Application.isPlaying) PrefabUtility.RecordPrefabInstancePropertyModifications(child.transform);
#endif
        }
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            PrefabUtility.RecordPrefabInstancePropertyModifications(nodes);
        }
#endif
    }

    private void SetLayoutOrigin(RectTransform origin, Vector2 position)
    {
        if ((origin.anchoredPosition - position).sqrMagnitude < .0001f) return;
#if UNITY_EDITOR
        if (!Application.isPlaying) Undo.RecordObject(origin, "Set Skill Tree Layout Origin");
#endif
        origin.anchoredPosition = position;
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
