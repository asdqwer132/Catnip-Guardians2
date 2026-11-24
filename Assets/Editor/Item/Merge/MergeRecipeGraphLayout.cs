using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal static class MergeRecipeGraphLayout
{
    internal const float NodeWidth = 240;
    internal static float NodeHeight(MergeGraphItemNode node) => 200 + node.ingredients.Count * 30;
    private static float Safe(float value, float fallback) => float.IsFinite(value) ? Mathf.Max(1, value) : fallback;
    internal static Vector2 BasePitch(MergeRecipeGraphAsset graph)
    {
        float height = graph.items.Count == 0 ? 320 : graph.items.Max(NodeHeight);
        return new Vector2(Mathf.Max(Safe(graph.gridCellSize.x, 400), NodeWidth + Safe(graph.sameCellSpacing.x, 20)),
            Mathf.Max(Safe(graph.gridCellSize.y, 360), height + Safe(graph.sameCellSpacing.y, 20)));
    }
    internal static Vector2 Pitch(MergeRecipeGraphAsset graph)
    {
        int count = graph.items.Count == 0 ? 1 : graph.items.GroupBy(node => node.gridPosition).Max(group => group.Count());
        int cols = Mathf.CeilToInt(Mathf.Sqrt(count)), rows = Mathf.CeilToInt((float)count / cols);
        float height = graph.items.Count == 0 ? 320 : graph.items.Max(NodeHeight);
        var gap = new Vector2(Safe(graph.sameCellSpacing.x, 20), Safe(graph.sameCellSpacing.y, 20));
        var pitch = BasePitch(graph);
        return new Vector2(Mathf.Max(pitch.x, cols * NodeWidth + cols * gap.x), Mathf.Max(pitch.y, rows * height + rows * gap.y));
    }
    internal static Vector2Int Nearest(Vector2 point, Vector2 pitch)
        => new Vector2Int(Mathf.RoundToInt(point.x / pitch.x), Mathf.RoundToInt(point.y / pitch.y));
    public static void SetMode(MergeRecipeGraphAsset graph, MergeGraphPlacementMode mode)
    {
        if (graph.placementMode == mode) return;
        Undo.RecordObject(graph, "Change merge graph placement mode");
        if (mode == MergeGraphPlacementMode.Grid)
        {
            var pitch = BasePitch(graph);
            foreach (var node in graph.items) node.gridPosition = Nearest(node.position, pitch);
        }
        graph.placementMode = mode;
        if (mode == MergeGraphPlacementMode.Grid) Arrange(graph);
        EditorUtility.SetDirty(graph);
    }
    public static void Arrange(MergeRecipeGraphAsset graph)
    {
        if (graph == null || graph.placementMode != MergeGraphPlacementMode.Grid) return;
        Undo.RecordObject(graph, "Place merge nodes in grid cells");
        Vector2 pitch = Pitch(graph);
        float height = graph.items.Count == 0 ? 320 : graph.items.Max(NodeHeight);
        Vector2 gap = new Vector2(Safe(graph.sameCellSpacing.x, 20), Safe(graph.sameCellSpacing.y, 20));
        foreach (var group in graph.items.GroupBy(node => node.gridPosition))
        {
            int cols = Mathf.CeilToInt(Mathf.Sqrt(group.Count())), index = 0;
            foreach (var node in group)
            {
                node.position = Vector2.Scale((Vector2)group.Key, pitch) + new Vector2(index % cols * (NodeWidth + gap.x), index / cols * (height + gap.y));
                index++;
            }
        }
        EditorUtility.SetDirty(graph);
    }
}
