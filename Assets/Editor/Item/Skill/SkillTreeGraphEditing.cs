using System.Collections.Generic;
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
        layout.nodeLayout.Add(new SkillNodePlacement { node = node, position = position });
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
        layout.revision++; EditorUtility.SetDirty(layout);
    }
}
