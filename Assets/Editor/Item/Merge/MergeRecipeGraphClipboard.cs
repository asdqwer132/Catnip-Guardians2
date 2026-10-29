using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal static class MergeRecipeGraphClipboard
{
    private const string Prefix = "CatnipMergeNodes:1\n";
    internal static bool CanPaste(string text) => !string.IsNullOrEmpty(text) && text.StartsWith(Prefix, StringComparison.Ordinal);
    [Serializable] private sealed class Payload { public List<Node> nodes = new List<Node>(); }
    [Serializable] private sealed class Node
    {
        public string id, item;
        public Vector2 position;
        public int tier;
        public List<string> ingredients = new List<string>();
        public List<string> sources = new List<string>();
    }
    private static string Id(MergeItem item) => item != null ? GlobalObjectId.GetGlobalObjectIdSlow(item).ToString() : null;
    private static MergeItem Resolve(string id)
        => GlobalObjectId.TryParse(id, out var key) ? GlobalObjectId.GlobalObjectIdentifierToObjectSlow(key) as MergeItem : null;

    internal static string Copy(MergeRecipeGraphAsset graph, IEnumerable<MergeGraphItemNode> selection)
    {
        var payload = new Payload();
        foreach (var node in selection)
        {
            var data = new Node { id = node.id, item = Id(node.item), position = node.position, tier = node.tier };
            for (int i = 0; i < node.ingredients.Count; i++)
            {
                data.ingredients.Add(Id(node.ingredients[i]));
                data.sources.Add(MergeRecipeGraphEditing.InputNode(graph, node, i)?.id);
            }
            payload.nodes.Add(data);
        }
        return Prefix + JsonUtility.ToJson(payload);
    }
    internal static List<MergeGraphItemNode> Paste(MergeRecipeGraphAsset graph, string text)
    {
        var added = new List<MergeGraphItemNode>();
        if (graph == null || !CanPaste(text)) return added;
        Payload payload;
        try { payload = JsonUtility.FromJson<Payload>(text.Substring(Prefix.Length)); }
        catch (ArgumentException) { return added; }
        if (payload?.nodes == null || payload.nodes.Count == 0) return added;
        var remap = new Dictionary<string, MergeGraphItemNode>();
        var originals = new List<Node>();
        foreach (var data in payload.nodes)
        {
            if (data == null || string.IsNullOrEmpty(data.id) || remap.ContainsKey(data.id)) continue;
            var item = Resolve(data.item);
            if (item == null || data.ingredients == null) continue;
            var node = new MergeGraphItemNode
            {
                item = item, tier = data.tier, position = data.position,
                ingredients = data.ingredients.ConvertAll(Resolve), ingredientNodeIds = new List<string>()
            };
            // 새 ID / 독립된 입력 목록을 갖고, 추출 에셋은 공유하지 않습니다. ItemData는 복제하지 않습니다.
            remap.Add(data.id, node); originals.Add(data); added.Add(node);
        }
        if (added.Count == 0) return added;
        var offset = new Vector2(40, 40);
        while (added.Exists(node => graph.items.Exists(existing => Vector2.Distance(existing.position, node.position + offset) < 24)))
            offset += new Vector2(40, 40);
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Paste merge nodes"); Undo.RecordObject(graph, "Paste merge nodes");
        graph.items.AddRange(added);
        for (int n = 0; n < added.Count; n++)
        {
            var node = added[n]; node.position += offset; var data = originals[n];
            for (int i = 0; i < node.ingredients.Count; i++)
            {
                string source = data.sources != null && i < data.sources.Count ? data.sources[i] : null;
                if (source != null && remap.TryGetValue(source, out var copiedSource)) source = copiedSource.id;
                var item = node.ingredients[i];
                // 다른 그래프에 붙여넣을 때 필요한 외부 재료는 기존 에셋으로 빈 노드만 추가합니다.
                if (item != null && !graph.items.Exists(candidate => candidate.item == item))
                    MergeRecipeGraphEditing.AddItem(graph, item, node.position + new Vector2(-380, i * 300));
                node.ingredientNodeIds.Add(source);
            }
        }
        EditorUtility.SetDirty(graph); Undo.CollapseUndoOperations(group);
        return added;
    }
}
