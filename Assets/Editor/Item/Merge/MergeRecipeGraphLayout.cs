using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 티어 → 연결 깊이의 열은 유지하고, 열 안의 순서만 바꾸어 연결선 교차를 줄입니다.
internal static class MergeRecipeGraphLayout
{
    private sealed class Edge
    {
        public MergeGraphItemNode source, target;
        public int slot;
        public Vector2 Start => source.position + new Vector2(240, 59);
        public Vector2 End => target.position + new Vector2(0, 122 + slot * 30);
    }
    private readonly struct Score : IComparable<Score>
    {
        private readonly int crossings, overlaps;
        private readonly double span;
        public Score(int crossings, int overlaps, double span)
        { this.crossings = crossings; this.overlaps = overlaps; this.span = span; }
        public int CompareTo(Score other)
        {
            int value = crossings.CompareTo(other.crossings);
            if (value == 0) value = overlaps.CompareTo(other.overlaps);
            return value != 0 ? value : span.CompareTo(other.span);
        }
    }
    private static float Height(MergeGraphItemNode node) => 140 + (node.ingredients?.Count ?? 0) * 30;
    private static float Gap(float value, float fallback) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Max(0, value);

    public static void Arrange(MergeRecipeGraphAsset graph)
    {
        if (graph == null || graph.items == null || graph.items.Count == 0) return;
        var depths = ConnectionDepths(graph);
        var tiers = new SortedDictionary<int, SortedDictionary<int, List<MergeGraphItemNode>>>();
        foreach (var node in graph.items)
        {
            if (node == null) continue;
            if (!tiers.TryGetValue(node.tier, out var tier))
                tiers.Add(node.tier, tier = new SortedDictionary<int, List<MergeGraphItemNode>>());
            if (!tier.TryGetValue(depths[node], out var rows)) tier.Add(depths[node], rows = new List<MergeGraphItemNode>());
            rows.Add(node);
        }
        var columns = new List<List<MergeGraphItemNode>>();
        var columnOf = new Dictionary<MergeGraphItemNode, int>();
        foreach (var tier in tiers.Values) foreach (var rows in tier.Values)
        {
            foreach (var node in rows) columnOf[node] = columns.Count;
            columns.Add(rows);
        }
        var edges = new List<Edge>();
        foreach (var node in graph.items)
            if (node?.ingredients != null) for (int i = 0; i < node.ingredients.Count; i++)
            {
                var source = MergeRecipeGraphEditing.InputNode(graph, node, i);
                if (source != null) edges.Add(new Edge { source = source, target = node, slot = i });
            }

        var gap = new Vector2(Gap(graph.nodeSpacing.x, 140), Gap(graph.nodeSpacing.y, 45));
        void PlaceColumn(int column)
        {
            float y = 50;
            foreach (var node in columns[column])
            { node.position = new Vector2(40 + column * (240 + gap.x), y); y += Height(node) + gap.y; }
        }
        void PlaceAll() { for (int i = 0; i < columns.Count; i++) PlaceColumn(i); }
        List<MergeGraphItemNode>[] Snapshot() => columns.ConvertAll(rows => new List<MergeGraphItemNode>(rows)).ToArray();

        Undo.RecordObject(graph, "Arrange merge nodes and reduce connection crossings");
        PlaceAll();
        Score bestScore = Evaluate(edges, graph.items);
        var best = Snapshot();
        void KeepBest()
        {
            var score = Evaluate(edges, graph.items);
            if (score.CompareTo(bestScore) >= 0) return;
            bestScore = score; best = Snapshot();
        }
        // 좌우로 번갈아 순회하며 입력 포트 순서까지 고려해 연결된 높이에 가깝게 정렬합니다.
        // Crossing reduction is a heuristic: fixed tiers and cycles can prevent a crossing-free layout.
        for (int pass = 0; pass < 8; pass++)
        {
            bool forward = pass % 2 == 0;
            for (int step = 0; step < columns.Count; step++)
            {
                int column = forward ? step : columns.Count - 1 - step;
                if (columns[column].Count < 2) continue;
                var ranks = new Dictionary<MergeGraphItemNode, float>();
                var order = new Dictionary<MergeGraphItemNode, int>();
                for (int i = 0; i < columns[column].Count; i++)
                {
                    var node = columns[column][i]; double sum = 0; int count = 0;
                    foreach (var edge in edges)
                    {
                        MergeGraphItemNode neighbor; float desired;
                        if (edge.target == node) { neighbor = edge.source; desired = edge.Start.y - (122 + edge.slot * 30); }
                        else if (edge.source == node) { neighbor = edge.target; desired = edge.End.y - 59; }
                        else continue;
                        if (forward ? columnOf[neighbor] >= column : columnOf[neighbor] <= column) continue;
                        sum += desired; count++;
                    }
                    ranks[node] = count > 0 ? (float)(sum / count) : node.position.y;
                    order[node] = i;
                }
                columns[column].Sort((a, b) =>
                {
                    int comparison = ranks[a].CompareTo(ranks[b]);
                    return comparison != 0 ? comparison : order[a].CompareTo(order[b]);
                });
                bool changed = false;
                for (int i = 0; i < columns[column].Count; i++)
                    if (order[columns[column][i]] != i) { changed = true; break; }
                if (changed) { PlaceColumn(column); KeepBest(); }
            }
        }
        for (int i = 0; i < columns.Count; i++) columns[i] = best[i];
        PlaceAll();
        // 평균 높이만으로 풀리지 않는 꼬임은 이웃한 두 노드를 교환해 비교합니다.
        for (int pass = 0; pass < 2; pass++)
        {
            bool improved = false;
            for (int c = 0; c < columns.Count; c++)
                for (int i = 0; i + 1 < columns[c].Count; i++)
                {
                    var rows = columns[c]; var a = rows[i]; rows[i] = rows[i + 1]; rows[i + 1] = a; PlaceColumn(c);
                    var score = Evaluate(edges, graph.items);
                    if (score.CompareTo(bestScore) < 0) { bestScore = score; improved = true; }
                    else { rows[i + 1] = rows[i]; rows[i] = a; PlaceColumn(c); }
                }
            if (!improved) break;
        }
        EditorUtility.SetDirty(graph);
    }

    private static Score Evaluate(List<Edge> edges, List<MergeGraphItemNode> nodes)
    {
        int crossings = 0, overlaps = 0; double span = 0;
        for (int i = 0; i < edges.Count; i++)
        {
            var edge = edges[i]; Vector2 a = edge.Start, b = edge.End;
            span += Math.Abs(a.y - b.y);
            for (int j = 0; j < i; j++)
                if (Intersects(a, b, edges[j].Start, edges[j].End)) crossings++;
            // 긴 연결이 중간 카드를 가로지르는 배치도 피합니다.
            foreach (var node in nodes)
            {
                if (node == null || node == edge.source || node == edge.target) continue;
                var rect = new Rect(node.position, new Vector2(240, Height(node)));
                if (rect.Contains(a) || rect.Contains(b) ||
                    Intersects(a, b, rect.min, new Vector2(rect.xMax, rect.yMin)) ||
                    Intersects(a, b, new Vector2(rect.xMax, rect.yMin), rect.max) ||
                    Intersects(a, b, rect.max, new Vector2(rect.xMin, rect.yMax)) ||
                    Intersects(a, b, new Vector2(rect.xMin, rect.yMax), rect.min)) overlaps++;
            }
        }
        return new Score(crossings, overlaps, span);
    }
    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
    private static bool Intersects(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        Vector2 ab = b - a, cd = d - c; float denominator = Cross(ab, cd);
        if (Mathf.Abs(denominator) < .0001f) return false;
        float t = Cross(c - a, cd) / denominator, u = Cross(c - a, ab) / denominator;
        // 같은 출력을 공유하는 선의 시작점은 교차로 세지 않습니다.
        return t > .0001f && t < .9999f && u > .0001f && u < .9999f;
    }
    private static Dictionary<MergeGraphItemNode, int> ConnectionDepths(MergeRecipeGraphAsset graph)
    {
        var nodes = graph.items;
        // 캔버스와 동일한 노드 ID를 사용합니다. 예전 연결은 같은 아이템의 첫 출력으로 해석합니다.
        var inputs = new Dictionary<MergeGraphItemNode, List<MergeGraphItemNode>>();
        foreach (var node in nodes)
        {
            if (node == null || inputs.ContainsKey(node)) continue;
            var sources = new List<MergeGraphItemNode>();
            if (node.ingredients != null)
                for (int i = 0; i < node.ingredients.Count; i++)
                {
                    var source = MergeRecipeGraphEditing.InputNode(graph, node, i);
                    if (source != null && !sources.Contains(source)) sources.Add(source);
                }
            inputs.Add(node, sources);
        }

        // 순환 연결은 같은 묶음으로 압축합니다. 연결을 지우지 않고도 유한한 깊이를 계산할 수 있습니다.
        var indices = new Dictionary<MergeGraphItemNode, int>();
        var lows = new Dictionary<MergeGraphItemNode, int>();
        var stack = new Stack<MergeGraphItemNode>();
        var onStack = new HashSet<MergeGraphItemNode>();
        var components = new Dictionary<MergeGraphItemNode, int>();
        var groups = new List<List<MergeGraphItemNode>>();
        void Visit(MergeGraphItemNode node)
        {
            indices[node] = lows[node] = indices.Count;
            stack.Push(node); onStack.Add(node);
            foreach (var source in inputs[node])
            {
                if (!indices.ContainsKey(source)) { Visit(source); lows[node] = Math.Min(lows[node], lows[source]); }
                else if (onStack.Contains(source)) lows[node] = Math.Min(lows[node], indices[source]);
            }
            if (lows[node] != indices[node]) return;
            var group = new List<MergeGraphItemNode>();
            MergeGraphItemNode member;
            do
            {
                member = stack.Pop(); onStack.Remove(member); components[member] = groups.Count; group.Add(member);
            } while (member != node);
            groups.Add(group);
        }
        foreach (var node in nodes) if (node != null && !indices.ContainsKey(node)) Visit(node);

        var groupDepths = new int[groups.Count];
        var depths = new Dictionary<MergeGraphItemNode, int>();
        // Visit은 재료 쪽 묶음을 먼저 완성하므로, 각 묶음은 이미 계산된 재료 깊이를 사용할 수 있습니다.
        for (int i = 0; i < groups.Count; i++)
        {
            foreach (var node in groups[i])
                foreach (var source in inputs[node])
                    if (components[source] != i) groupDepths[i] = Math.Max(groupDepths[i], groupDepths[components[source]] + 1);
            foreach (var node in groups[i]) depths[node] = groupDepths[i];
        }
        return depths;
    }
}
