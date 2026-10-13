#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class PrefabRangeGizmoDrawer
{
    private static readonly List<PrefabVisualRange> ranges = new List<PrefabVisualRange>();
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    private static void DrawDamageArea(DamageArea source, GizmoType flags) => DrawDefault(source, source);
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    private static void DrawArea(ReactiveGroundArea source, GizmoType flags) => DrawDefault(source, source);
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    private static void DrawSummon(SummonActor source, GizmoType flags) => DrawDefault(source, source);
    private static void DrawDefault(MonoBehaviour component, IPrefabRangeProvider provider)
    {
        if (component == null || component.GetComponentInChildren<PrefabRangeGizmo>(true) != null || !Selected(component.transform)) return;
        ranges.Clear(); provider.CollectPrefabRanges(ranges); DrawRanges(false, Color.white, true);
    }
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    private static void DrawConfigured(PrefabRangeGizmo component, GizmoType flags)
    {
        if (component == null || !component.enabled || (component.selectedOnly && !Selected(component.transform))) return;
        component.CollectRanges(ranges); DrawRanges(component.useCustomColor, component.color, component.showLabels);
    }
    private static bool Selected(Transform target)
    {
        foreach (Transform selected in Selection.transforms)
            if (selected != null && (selected == target || selected.IsChildOf(target) || target.IsChildOf(selected))) return true;
        return false;
    }
    private static void DrawRanges(bool customColor, Color color, bool labels)
    {
        Color previous = Gizmos.color;
        try
        {
            for (int i = 0; i < ranges.Count; i++)
            {
                PrefabVisualRange range = ranges[i];
                Gizmos.color = customColor ? color : RangeColor(range.kind);
                if (range.shape == PrefabRangeShape.Rectangle)
                {
                    Gizmos.DrawLine(range.corner0, range.corner1); Gizmos.DrawLine(range.corner1, range.corner2);
                    Gizmos.DrawLine(range.corner2, range.corner3); Gizmos.DrawLine(range.corner3, range.corner0);
                }
                else
                {
                    Vector3 last = range.ArcPoint(0f);
                    const int segments = 64;
                    for (int step = 1; step <= segments; step++)
                    { Vector3 next = range.ArcPoint(step / (float)segments); Gizmos.DrawLine(last, next); last = next; }
                    if (range.shape == PrefabRangeShape.Sector && range.angle < 360f)
                    { Gizmos.DrawLine(range.center, range.ArcPoint(0f)); Gizmos.DrawLine(range.center, range.ArcPoint(1f)); }
                }
                if (labels) Handles.Label(range.center + Vector3.up * (0.05f + i * 0.12f), range.label +
                    (range.shape == PrefabRangeShape.Rectangle ? " (Rectangle)" : " r=" + range.radius.ToString("0.###")));
            }
        }
        finally { Gizmos.color = previous; }
    }
    private static Color RangeColor(PrefabRangeKind kind)
    {
        if (kind == PrefabRangeKind.Damage) return new Color(1f, 0.35f, 0.2f, 1f);
        if (kind == PrefabRangeKind.Detection) return new Color(0.15f, 0.85f, 1f, 1f);
        if (kind == PrefabRangeKind.Support) return new Color(0.8f, 0.4f, 1f, 1f);
        return new Color(0.6f, 0.9f, 0.6f, 1f);
    }
}
#endif
