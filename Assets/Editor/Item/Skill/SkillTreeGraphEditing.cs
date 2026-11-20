using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SkillTreeGraphEditing
{
    public static void Add(SkillTreeLayoutData layout, SkillNodeData node, Vector2 position)
    {
        if (layout == null || node == null || layout.nodeDatas.Contains(node)) return;
        if (node.skillMap == null && layout.skillMap != null)
        { Undo.RecordObject(node, "Assign skill map"); node.skillMap = layout.skillMap; EditorUtility.SetDirty(node); }
        Undo.RecordObject(layout, "Add skill node"); layout.nodeDatas.Add(node);
        var placement = new SkillNodePlacement { node = node, position = position };
        if (layout.gridPlacement) placement.gridPosition = GridCoordinate(position, GridPitch(layout));
        layout.nodeLayout.Add(placement);
        ArrangeGrid(layout);
        layout.revision++; EditorUtility.SetDirty(layout);
    }
    public static bool Connect(SkillTreeLayoutData layout, SkillNodeData parent, SkillNodeData child)
    {
        if (layout == null || parent == null || child == null || parent == child ||
            !layout.nodeDatas.Contains(parent) || !layout.nodeDatas.Contains(child)) return false;
        var visited = new HashSet<SkillNodeData>();
        if (DependsOn(parent, child, visited)) return false;
        if (child.requiredSkills != null && child.requiredSkills.Contains(parent)) return false;
        Undo.RecordObject(child, "Connect skill prerequisite");
        if (child.requiredSkills == null) child.requiredSkills = new List<SkillNodeData>();
        child.requiredSkills.Add(parent); EditorUtility.SetDirty(child); return true;
    }
    private static bool DependsOn(SkillNodeData node, SkillNodeData target, HashSet<SkillNodeData> visited)
    {
        if (node == target) return true;
        if (node == null || !visited.Add(node) || node.requiredSkills == null) return false;
        foreach (var parent in node.requiredSkills) if (DependsOn(parent, target, visited)) return true;
        return false;
    }
    public static void Disconnect(SkillNodeData parent, SkillNodeData child)
    {
        if (child == null || child.requiredSkills == null || !child.requiredSkills.Contains(parent)) return;
        Undo.RecordObject(child, "Disconnect skill prerequisite"); child.requiredSkills.Remove(parent); EditorUtility.SetDirty(child);
    }
    public static void Move(SkillTreeLayoutData layout, SkillNodeData node, Vector2 position)
    {
        Undo.RecordObject(layout, "Move skill node");
        var entry = layout.nodeLayout.Find(value => value != null && value.node == node);
        if (entry == null) layout.nodeLayout.Add(new SkillNodePlacement { node = node, position = position });
        else entry.position = position;
        layout.revision++; EditorUtility.SetDirty(layout);
    }
    public static void Remove(SkillTreeLayoutData layout, SkillNodeData node)
    {
        Undo.RecordObject(layout, "Remove skill node"); layout.nodeDatas.Remove(node);
        layout.nodeLayout.RemoveAll(value => value == null || value.node == node);
        foreach (var child in layout.nodeDatas) if (child != null) Disconnect(node, child);
        ArrangeGrid(layout);
        layout.revision++; EditorUtility.SetDirty(layout);
    }

    private static readonly Vector2 GridNodeSize = new Vector2(220f, 200f);

    public static Vector2 GridPitch(SkillTreeLayoutData layout)
    {
        int largestGroup = layout.nodeLayout.Where(entry => entry != null && entry.node != null)
            .GroupBy(entry => entry.gridPosition).Select(group => group.Count()).DefaultIfEmpty(1).Max();
        int columns = Mathf.CeilToInt(Mathf.Sqrt(largestGroup));
        int rows = Mathf.CeilToInt((float)largestGroup / columns);
        return Vector2.Max(layout.gridCellSize,
            Vector2.Scale(GridNodeSize + layout.gridNodeGap, new Vector2(columns, rows)));
    }

    public static Vector2Int GridCoordinate(Vector2 position, Vector2 pitch)
        => new Vector2Int(Mathf.RoundToInt(position.x / pitch.x), Mathf.RoundToInt(position.y / pitch.y));

    public static void SetGridMode(SkillTreeLayoutData layout, bool enabled)
    {
        if (layout.gridPlacement == enabled) return;
        Undo.RecordObject(layout, "Change skill graph placement mode");
        layout.gridPlacement = enabled;
        if (enabled)
        {
            var pitch = Vector2.Max(layout.gridCellSize, GridNodeSize + layout.gridNodeGap);
            foreach (var node in layout.nodeDatas)
                if (node != null)
                    GetPlacement(layout, node);
            foreach (var entry in layout.nodeLayout)
                if (entry != null) entry.gridPosition = GridCoordinate(entry.position, pitch);
            ArrangeGrid(layout);
        }
        layout.revision++;
        EditorUtility.SetDirty(layout);
    }

    public static void SetGridSpacing(SkillTreeLayoutData layout, Vector2 cellSize, Vector2 gap)
    {
        Undo.RecordObject(layout, "Change skill graph grid spacing");
        layout.gridCellSize = Vector2.Max(Vector2.one, cellSize);
        layout.gridNodeGap = Vector2.Max(Vector2.zero, gap);
        ArrangeGrid(layout);
        layout.revision++;
        EditorUtility.SetDirty(layout);
    }

    public static void MoveToGrid(SkillTreeLayoutData layout, IEnumerable<KeyValuePair<SkillNodeData, Vector2Int>> changes)
    {
        Undo.RecordObject(layout, "Move skill nodes in grid");
        foreach (var change in changes)
            GetPlacement(layout, change.Key).gridPosition = change.Value;
        ArrangeGrid(layout);
        layout.revision++;
        EditorUtility.SetDirty(layout);
    }

    private static SkillNodePlacement GetPlacement(SkillTreeLayoutData layout, SkillNodeData node)
    {
        var entry = layout.nodeLayout.Find(value => value != null && value.node == node);
        if (entry != null) return entry;
        entry = new SkillNodePlacement { node = node };
        layout.nodeLayout.Add(entry);
        return entry;
    }

    private static void ArrangeGrid(SkillTreeLayoutData layout)
    {
        if (!layout.gridPlacement) return;
        var pitch = GridPitch(layout);
        var stride = GridNodeSize + layout.gridNodeGap;
        foreach (var group in layout.nodeLayout.Where(entry => entry != null && entry.node != null).GroupBy(entry => entry.gridPosition))
        {
            int columns = Mathf.CeilToInt(Mathf.Sqrt(group.Count()));
            int index = 0;
            foreach (var entry in group)
            {
                entry.position = Vector2.Scale((Vector2)group.Key, pitch)
                    + new Vector2(index % columns * stride.x, -(index / columns) * stride.y);
                index++;
            }
        }
    }
}
