#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public partial class SkillTreeNodePlacer
{
    public bool IsApplyingLayout { get; private set; }
    private Dictionary<SkillNodeData, SkillNodeUI> Views()
    {
        var result = new Dictionary<SkillNodeData, SkillNodeUI>();
        if (nodeArea != null) foreach (var view in nodeArea.GetComponentsInChildren<SkillNodeUI>(true))
            if (view.SkillNodeData != null && !result.ContainsKey(view.SkillNodeData)) result.Add(view.SkillNodeData, view);
        return result;
    }
    private static bool Different(Vector2 a, Vector2 b) => (a - b).sqrMagnitude > .0001f;
    private Vector2 Position(SkillNodeUI view)
        => (Vector2)nodeArea.InverseTransformPoint(view.transform.position) - nodeArea.rect.center;
    private void Dirty(UnityEngine.Object target)
    {
        EditorUtility.SetDirty(target);
        if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }
    private bool CanEdit => !EditorApplication.isPlayingOrWillChangePlaymode && !EditorUtility.IsPersistent(this);

    [ContextMenu("Remember Node Positions")]
    public void CaptureNodePositions()
    {
        if (!CanEdit || nodeArea == null) return;
        var views = Views();
        bool changed = false;
        foreach (var pair in views)
        {
            if (nodeDatas == null || !nodeDatas.Contains(pair.Key)) continue;
            Vector2 position = Position(pair.Value);
            var saved = nodeLayout.Find(entry => entry != null && entry.node == pair.Key);
            if (saved != null && !Different(saved.position, position)) continue;
            if (!changed) { Undo.RegisterCompleteObjectUndo(this, "Remember skill node positions"); changed = true; }
            if (saved == null) nodeLayout.Add(new SkillNodePlacement { node = pair.Key, position = position });
            else saved.position = position;
        }
        if (changed) Dirty(this);
        RefreshConnections();
    }

    public void ApplySavedLayout(bool recordUndo = true)
    {
        if (!CanEdit || nodeArea == null) return;
        IsApplyingLayout = true;
        try
        {
            foreach (var pair in Views())
            {
                var saved = nodeLayout.Find(entry => entry != null && entry.node == pair.Key);
                if (saved == null || !Different(Position(pair.Value), saved.position)) continue;
                if (recordUndo) Undo.RegisterCompleteObjectUndo(pair.Value.transform, "Position skill node");
                pair.Value.transform.position = nodeArea.TransformPoint(nodeArea.rect.center + saved.position);
                Dirty(pair.Value.transform);
            }
            RefreshConnections();
        }
        finally { IsApplyingLayout = false; }
    }

    [ContextMenu("Apply Initial Grid Positions")]
    public void ApplyGridPositions()
    {
        if (!CanEdit || nodeDatas == null) return;
        Undo.RegisterCompleteObjectUndo(this, "Apply initial skill grid");
        nodeLayout.Clear();
        for (int i = 0; i < nodeDatas.Count; i++)
            if (nodeDatas[i] != null) nodeLayout.Add(new SkillNodePlacement { node = nodeDatas[i], position = GridPosition(i) });
        Dirty(this); ApplySavedLayout();
    }
    private Vector2 GridPosition(int index)
    {
        var grid = nodePositions != null && index < nodePositions.Count ? nodePositions[index] : new Vector2Int(index, 0);
        return startPosition + new Vector2(grid.x * spacing.x, grid.y * spacing.y);
    }

    [ContextMenu("Generate Skill Tree")]
    public void Generate()
    {
        if (!CanEdit || nodeArea == null || lineArea == null || nodePrefab == null ||
            horizontalLinePrefab == null || verticalLinePrefab == null || nodeDatas == null) return;
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Generate skill tree");
        if (autoRememberPositions) CaptureNodePositions();
        var views = Views();
        for (int i = 0; i < nodeDatas.Count; i++)
        {
            var data = nodeDatas[i];
            if (data == null || views.ContainsKey(data)) continue;
            var view = PrefabUtility.IsPartOfPrefabAsset(nodePrefab)
                ? (SkillNodeUI)PrefabUtility.InstantiatePrefab(nodePrefab, nodeArea)
                : Instantiate(nodePrefab, nodeArea, false);
            if (useReadableName) view.name = "Node_" + NodeName(data);
            var rect = (RectTransform)view.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.localScale = Vector3.one;
            var saved = nodeLayout.Find(entry => entry != null && entry.node == data);
            rect.anchoredPosition = saved != null ? saved.position : GridPosition(i);
            view.SetData(data, Array.Empty<GameObject>());
            Dirty(view); Dirty(rect);
            Undo.RegisterCreatedObjectUndo(view.gameObject, "Create skill node");
            views.Add(data, view);
        }
        CaptureNodePositions();
        RebuildConnections();
        // Only nodes removed from the configured list are removed; existing configured nodes keep their overrides.
        foreach (var pair in views)
            if (!nodeDatas.Contains(pair.Key)) Undo.DestroyObjectImmediate(pair.Value.gameObject);
        Undo.CollapseUndoOperations(group);
    }

    [ContextMenu("Rebuild Connections")]
    public void RebuildConnections()
    {
        if (!CanEdit || nodeArea == null || lineArea == null || horizontalLinePrefab == null || verticalLinePrefab == null) return;
        var views = Views();
        var previous = Connections(views);
        var existing = new Dictionary<(SkillNodeUI, SkillNodeUI), SkillTreeConnectionUI>();
        foreach (var connection in lineArea.GetComponentsInChildren<SkillTreeConnectionUI>(true))
            if (connection.from != null && connection.to != null && !existing.ContainsKey((connection.from, connection.to)))
                existing.Add((connection.from, connection.to), connection);
        var lines = new Dictionary<SkillNodeData, List<GameObject>>();
        foreach (var pair in views) lines.Add(pair.Key, new List<GameObject>());
        foreach (var pair in views)
        {
            if (nodeDatas == null || !nodeDatas.Contains(pair.Key) || pair.Key.requiredSkills == null) continue;
            var parents = new HashSet<SkillNodeData>();
            foreach (var parent in pair.Key.requiredSkills)
            {
                if (parent == null || !parents.Add(parent) || !views.TryGetValue(parent, out var source) || !nodeDatas.Contains(parent)) continue;
                if (existing.TryGetValue((source, pair.Value), out var retained))
                {
                    previous.Remove(retained.gameObject);
                    retained.bendMode = bendMode; retained.nodeHalfSize = nodeHalfSize; retained.RefreshGeometry();
                    Dirty(retained); lines[parent].Add(retained.gameObject);
                    continue;
                }
                var root = new GameObject("Connection_" + NodeName(parent) + "_To_" + NodeName(pair.Key), typeof(RectTransform));
                root.layer = lineArea.gameObject.layer;
                var rect = (RectTransform)root.transform; rect.SetParent(lineArea, false);
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                var connection = root.AddComponent<SkillTreeConnectionUI>();
                connection.from = source; connection.to = pair.Value; connection.bendMode = bendMode; connection.nodeHalfSize = nodeHalfSize;
                connection.horizontalLine = Segment(horizontalLinePrefab, rect);
                connection.verticalLine = Segment(verticalLinePrefab, rect);
                connection.SetUnlocked(false); connection.RefreshGeometry();
                Dirty(connection.horizontalLine.transform); Dirty(connection.verticalLine.transform);
                Undo.RegisterCreatedObjectUndo(root, "Create skill connection");
                lines[parent].Add(root);
            }
        }
        foreach (var pair in views)
        {
            Undo.RegisterCompleteObjectUndo(pair.Value, "Assign skill connections");
            pair.Value.SetData(pair.Key, lines[pair.Key].ToArray()); Dirty(pair.Value);
        }
        foreach (var old in previous) if (old != null) Undo.DestroyObjectImmediate(old);
        if (gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

    private SkillTreeLineUI Segment(RectTransform prefab, RectTransform parent)
    {
        var rect = PrefabUtility.IsPartOfPrefabAsset(prefab)
            ? (RectTransform)PrefabUtility.InstantiatePrefab(prefab, parent)
            : Instantiate(prefab, parent, false);
        foreach (var child in rect.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = parent.gameObject.layer;
        var style = rect.GetComponent<SkillTreeLineUI>();
        // Existing custom templates remain usable. Their current artwork is the locked-state fallback.
        if (style == null) style = rect.gameObject.AddComponent<SkillTreeLineUI>();
        return style;
    }

    private HashSet<GameObject> Connections(Dictionary<SkillNodeData, SkillNodeUI> views)
    {
        var result = new HashSet<GameObject>();
        if (lineArea == null) return result;
        foreach (var view in views.Values)
            if (view.Lines != null) foreach (var line in view.Lines)
                if (line != null && line.transform.IsChildOf(lineArea)) result.Add(line);
        foreach (var connection in lineArea.GetComponentsInChildren<SkillTreeConnectionUI>(true)) result.Add(connection.gameObject);
        return result;
    }

    [ContextMenu("Clear Generated Objects")]
    public void ClearGeneratedObjects()
    {
        if (!CanEdit) return;
        if (autoRememberPositions) CaptureNodePositions();
        var views = Views();
        foreach (var line in Connections(views)) if (line != null) Undo.DestroyObjectImmediate(line);
        foreach (var view in views.Values) if (view != null) Undo.DestroyObjectImmediate(view.gameObject);
    }

    public void RefreshConnections()
    {
        if (lineArea == null) return;
        foreach (var connection in lineArea.GetComponentsInChildren<SkillTreeConnectionUI>(true))
        { connection.bendMode = bendMode; connection.nodeHalfSize = nodeHalfSize; connection.RefreshGeometry(); }
    }
    private static string NodeName(SkillNodeData data)
    {
        string name = data.GetDataName(language.english);
        return !string.IsNullOrEmpty(name) ? name : !string.IsNullOrEmpty(data.dataId) ? data.dataId : data.name;
    }
}
#endif
