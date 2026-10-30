using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 사용자 템플릿을 에디터에서 복제합니다. 재배치 시에도 이 도구가 만든 루트만 교체합니다.
public static class MergeRecipeCanvasBuilder
{
    private sealed class Node
    {
        public ItemData item;
        public ItemRecipeData recipe;
        public int tier;
        public int depth;
        public MergeRecipeNodeUI view;
        public readonly List<Node> inputs = new List<Node>();
    }

    [MenuItem("CONTEXT/MergeRecipeCanvas/Rebuild All Recipe Layouts")]
    private static void BuildAllMenu(MenuCommand command) => BuildAll((MergeRecipeCanvas)command.context);

    [MenuItem("CONTEXT/MergeRecipeSeriesUI/Rebuild Recipe Layout")]
    private static void BuildMenu(MenuCommand command) => Build((MergeRecipeSeriesUI)command.context);

    public static void BuildAll(MergeRecipeCanvas canvas)
    {
        if (Application.isPlaying || canvas == null) return;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Rebuild series recipe layouts");
        foreach (var panel in canvas.series)
            if (panel != null && panel.recipeBook != null) Build(panel);
        Undo.CollapseUndoOperations(group);
        canvas.Refresh();
    }

    public static bool Build(MergeRecipeSeriesUI panel)
    {
        if (Application.isPlaying || panel == null || panel.recipeBook == null) return false;
        if (panel.scrollView == null || panel.scrollView.content == null || panel.nodeTemplate == null || panel.lineTemplate == null)
        {
            Debug.LogWarning("[MergeRecipeCanvas] Assign Scroll View, Node Template and Line Template before rebuilding.", panel);
            return false;
        }

        var nodes = ReadBook(panel.recipeBook);
        var columns = Arrange(nodes);
        Vector2 nodeSize = ((RectTransform)panel.nodeTemplate.transform).rect.size;
        nodeSize = new Vector2(Mathf.Max(1, nodeSize.x), Mathf.Max(1, nodeSize.y));
        Vector2 gap = new Vector2(SafeSpacing(panel.nodeSpacing.x), SafeSpacing(panel.nodeSpacing.y));
        Vector2 padding = new Vector2(SafeSpacing(panel.padding.x), SafeSpacing(panel.padding.y));
        var content = panel.scrollView.content;
        var viewport = panel.scrollView.viewport != null ? panel.scrollView.viewport : (RectTransform)panel.scrollView.transform;

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build merge recipe layout");
        Undo.RegisterCompleteObjectUndo(content, "Resize recipe scroll content");
        // 등록되지 않은 사용자 오브젝트는 이름으로 찾아 삭제하지 않습니다.
        var previousRoot = panel.GeneratedRoot;
        var root = new GameObject("Generated Recipe Layout", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(content, false);
        root.gameObject.layer = content.gameObject.layer;
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(0, 1);

        float width = padding.x * 2 + columns.Count * nodeSize.x + Mathf.Max(0, columns.Count - 1) * gap.x;
        int maximumRows = 0;
        foreach (var column in columns) maximumRows = Mathf.Max(maximumRows, column.Count);
        float height = padding.y * 2 + maximumRows * nodeSize.y + Mathf.Max(0, maximumRows - 1) * gap.y;
        var size = new Vector2(Mathf.Max(width, viewport.rect.width), Mathf.Max(height, viewport.rect.height));
        content.anchorMin = content.anchorMax = content.pivot = root.pivot;
        content.sizeDelta = root.sizeDelta = size;
        content.anchoredPosition = root.anchoredPosition = Vector2.zero;

        var views = new List<MergeRecipeNodeUI>();
        for (int x = 0; x < columns.Count; x++) for (int y = 0; y < columns[x].Count; y++)
        {
            var node = columns[x][y];
            node.view = Clone(panel.nodeTemplate, root);
            node.view.name = node.recipe != null ? node.recipe.name : node.item.name;
            node.view.item = node.item;
            node.view.RefreshIcon();
            var rect = (RectTransform)node.view.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(.5f, .5f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.sizeDelta = nodeSize;
            rect.anchoredPosition = new Vector2(padding.x + nodeSize.x * .5f + x * (nodeSize.x + gap.x),
                -padding.y - nodeSize.y * .5f - y * (nodeSize.y + gap.y));
            views.Add(node.view);
            RecordPrefabOverrides(node.view, node.view.gameObject, rect, node.view.icon);
            EditorUtility.SetDirty(node.view);
        }

        var connections = new List<MergeRecipeConnection>();
        foreach (var node in nodes)
        {
            if (node.recipe == null || !MergeTree.TryGetIngredients(node.recipe, out var required)) continue;
            foreach (var input in node.inputs)
            {
                var line = Clone(panel.lineTemplate, root);
                line.name = input.item.name + " -> " + node.item.name;
                line.transform.SetAsFirstSibling();
                line.Setup((RectTransform)input.view.transform, (RectTransform)node.view.transform, required[input.item]);
                connections.Add(new MergeRecipeConnection { recipe = node.recipe, from = input.view, to = node.view, line = line });
                RecordPrefabOverrides(line, line.gameObject, line.transform, line.lineImage);
                EditorUtility.SetDirty(line);
            }
        }
        // Register the complete hierarchy once so Redo restores references between its children.
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Create recipe layout");
        // Undo restores the previous hierarchy before restoring these references; Redo creates the new one first.
        Undo.RegisterCompleteObjectUndo(panel, "Assign recipe nodes and lines");
        panel.SetEditorLayout(root, views, connections);
        if (previousRoot != null) Undo.DestroyObjectImmediate(previousRoot.gameObject);
        panel.Refresh(false, null);
        if (PrefabUtility.IsPartOfPrefabInstance(content)) PrefabUtility.RecordPrefabInstancePropertyModifications(content);
        if (PrefabUtility.IsPartOfPrefabInstance(panel)) PrefabUtility.RecordPrefabInstancePropertyModifications(panel);
        EditorUtility.SetDirty(panel);
        if (panel.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
        Undo.CollapseUndoOperations(group);
        return true;
    }

    private static float SafeSpacing(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0 : Mathf.Max(0, value);

    private static void RecordPrefabOverrides(params UnityEngine.Object[] objects)
    {
        foreach (var value in objects)
            if (value != null && PrefabUtility.IsPartOfPrefabInstance(value))
                PrefabUtility.RecordPrefabInstancePropertyModifications(value);
    }

    private static T Clone<T>(T template, Transform parent) where T : Component
    {
        var root = PrefabUtility.IsPartOfPrefabAsset(template)
            ? (GameObject)PrefabUtility.InstantiatePrefab(template.gameObject, parent)
            : UnityEngine.Object.Instantiate(template.gameObject, parent, false);
        root.SetActive(true);
        foreach (var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = parent.gameObject.layer;
        return root.GetComponent<T>();
    }

    private static List<Node> ReadBook(MergeRecipeBook book)
    {
        var nodes = new List<Node>();
        var producer = new Dictionary<ItemData, Node>();
        int firstTier = 0;
        if (book.tiers != null) foreach (var tier in book.tiers)
        {
            if (tier?.itemRecipeDatas == null) continue;
            firstTier = Mathf.Min(firstTier, tier.tier);
            foreach (var recipe in tier.itemRecipeDatas)
            {
                if (!MergeTree.TryGetIngredients(recipe, out _)) continue;
                var node = new Node { item = recipe.resultItem, recipe = recipe, tier = tier.tier };
                nodes.Add(node);
                // 같은 결과의 다른 레시피도 별도 노드로 남깁니다. 재료 연결은 북의 첫 결과 노드를 사용합니다.
                if (!producer.ContainsKey(node.item)) producer.Add(node.item, node);
            }
        }
        Node Material(ItemData item)
        {
            if (!producer.TryGetValue(item, out var node))
            {
                node = new Node { item = item, tier = firstTier - 1 };
                producer.Add(item, node);
                nodes.Add(node);
            }
            return node;
        }
        foreach (var node in nodes.ToArray())
        {
            MergeTree.TryGetIngredients(node.recipe, out var required);
            foreach (var item in required.Keys) node.inputs.Add(Material(item));
        }
        if (book.items != null) foreach (var definition in book.items)
            if (definition != null && definition.item != null) Material(definition.item);
        return nodes;
    }

    private static List<List<Node>> Arrange(List<Node> nodes)
    {
        var visiting = new HashSet<Node>();
        var depths = new Dictionary<Node, int>();
        int Depth(Node node)
        {
            if (depths.TryGetValue(node, out int known)) return known;
            if (!visiting.Add(node)) return 0; // Cyclic recipes still produce a finite layout.
            int depth = 0;
            foreach (var input in node.inputs) depth = Mathf.Max(depth, Depth(input) + 1);
            visiting.Remove(node);
            depths[node] = depth;
            return depth;
        }
        var ordered = new SortedDictionary<(int tier, int depth), List<Node>>();
        foreach (var node in nodes)
        {
            node.depth = Depth(node);
            var key = (node.tier, node.depth);
            if (!ordered.TryGetValue(key, out var column)) ordered.Add(key, column = new List<Node>());
            column.Add(node);
        }
        var columns = new List<List<Node>>(ordered.Values);
        var rows = new Dictionary<Node, int>();
        foreach (var column in columns)
            for (int i = 0; i < column.Count; i++) rows[column[i]] = i;
        // Keep tier/depth columns; order each column near the average of its connected nodes.
        for (int pass = 0; pass < 4; pass++) foreach (var column in columns)
        {
            var rank = new Dictionary<Node, float>();
            foreach (var node in column)
            {
                float sum = 0; int count = 0;
                foreach (var input in node.inputs) { sum += rows[input]; count++; }
                foreach (var output in nodes)
                    if (output.inputs.Contains(node)) { sum += rows[output]; count++; }
                rank[node] = count > 0 ? sum / count : rows[node];
            }
            column.Sort((a, b) => { int value = rank[a].CompareTo(rank[b]); return value != 0 ? value : rows[a].CompareTo(rows[b]); });
            for (int i = 0; i < column.Count; i++) rows[column[i]] = i;
        }
        return columns;
    }
}
