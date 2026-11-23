using System;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Layout/Skill Tree Child Bounds Fitter")]
public sealed class SkillTreeChildBoundsFitter : ChildBoundsFitter
{
    [Tooltip("Fit only purchased skill nodes. Refit when the set of purchased nodes in this subtree changes.")]
    [SerializeField] private bool unlockedNodesOnly;

    private SkillTreeManager manager;
    private readonly HashSet<RectTransform> lastFit = new HashSet<RectTransform>();
    private readonly HashSet<RectTransform> unlockedRects = new HashSet<RectTransform>();
    private readonly List<SkillNodeUI> nodes = new List<SkillNodeUI>();
    private bool refreshPending;
    private bool hasFit;

    private void OnEnable()
    {
        SkillTreeManager.InstanceChanged += BindManager;
        BindManager();
    }

    private void OnDisable()
    {
        SkillTreeManager.InstanceChanged -= BindManager;
        if (manager != null) manager.OnSkillTreeChanged -= RequestRefresh;
        manager = null;
        hasFit = false;
        refreshPending = false;
        lastFit.Clear();
    }

    private void BindManager()
    {
        if (manager != null) manager.OnSkillTreeChanged -= RequestRefresh;
        manager = SkillTreeManager.Instance;
        if (manager != null) manager.OnSkillTreeChanged += RequestRefresh;
        RequestRefresh();
    }

    private void RequestRefresh() => refreshPending = true;

    private void LateUpdate()
    {
        if (!refreshPending) return;
        refreshPending = false;
        if (!unlockedNodesOnly || manager == null || MeasurementRoot == null) return;

        CollectUnlockedRects(manager);
        if (hasFit && lastFit.SetEquals(unlockedRects)) return;
        FitToBounds(unlockedRects);
        lastFit.Clear();
        lastFit.UnionWith(unlockedRects);
        hasFit = true;
    }

    protected override bool FitEmptyBounds =>
        unlockedNodesOnly && MeasurementRoot != null && SkillTreeManager.Instance != null;

    protected override IEnumerable<RectTransform> GetMeasuredRects()
    {
        if (MeasurementRoot == null) return Array.Empty<RectTransform>();
        if (!unlockedNodesOnly) return base.GetMeasuredRects();

        CollectUnlockedRects(SkillTreeManager.Instance);
        return unlockedRects;
    }

    private void CollectUnlockedRects(SkillTreeManager skillTree)
    {
        unlockedRects.Clear();
        if (skillTree == null || MeasurementRoot == null) return;

        nodes.Clear();
        MeasurementRoot.GetComponentsInChildren(true, nodes);
        foreach (var node in nodes)
            if (node.transform is RectTransform rect && skillTree.IsUnlocked(node.SkillNodeData))
                unlockedRects.Add(rect);
        nodes.Clear();
    }
}
