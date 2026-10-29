using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MergeRecipeGraphEditing
{
    private static string Folder(MergeRecipeGraphAsset graph)
    {
        string path = AssetDatabase.GetAssetPath(graph);
        if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("그래프를 먼저 저장하세요.");
        return Path.GetDirectoryName(path).Replace('\\', '/');
    }
    private static string SafeName(string name)
    {
        foreach (char value in "/\\:*?\"<>|") name = name.Replace(value, '_');
        return string.IsNullOrWhiteSpace(name) ? "MergeItem" : name;
    }
    // + 버튼은 껍데기 MergeItem만 만듭니다. ItemData / 프리팹은 사용자가 할당합니다.
    public static MergeItem CreateItem(MergeRecipeGraphAsset graph, Vector2 position)
    {
        var item = ScriptableObject.CreateInstance<MergeItem>();
        item.name = "Merge Item " + (graph.items.Count + 1);
        AssetDatabase.CreateAsset(item, AssetDatabase.GenerateUniqueAssetPath(Folder(graph) + "/" + item.name + ".asset"));
        AddItem(graph, item, position);
        return item;
    }
    public static MergeGraphItemNode AddItem(MergeRecipeGraphAsset graph, MergeItem item, Vector2 position)
    {
        if (item == null) return null;
        EnsureDefaultInputs(graph);
        var existing = graph.items.Find(node => node.item == item);
        if (existing != null) return existing;
        Undo.RecordObject(graph, "Add merge item node");
        var node = new MergeGraphItemNode { item = item, position = position };
        graph.items.Add(node); EditorUtility.SetDirty(graph);
        return node;
    }
    public static int ImportItemData(MergeRecipeGraphAsset graph, IEnumerable<ItemData> items, Vector2 position)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("플레이를 종료한 뒤 가져오세요.");
        string folder = Folder(graph);
        var incoming = new List<ItemData>();
        var unique = new HashSet<ItemData>();
        foreach (var data in items)
        {
            if (data == null || !unique.Add(data)) continue;
            if (!AssetDatabase.Contains(data)) throw new InvalidOperationException("Project에 저장된 ItemData 에셋을 넣으세요.");
            incoming.Add(data);
        }
        if (incoming.Count == 0) return 0;
        Upgrade(graph);
        var existing = new Dictionary<ItemData, MergeItem>();
        var present = new HashSet<ItemData>();
        foreach (var node in graph.items)
        {
            if (node.item != null && node.item.item != null)
            { present.Add(node.item.item); existing[node.item.item] = node.item; }
            // 기존 노드와 연결을 움직이지 않고, 새 묶음을 그 아래에 배치합니다.
            position.y = Mathf.Max(position.y, node.position.y + 180 + node.ingredients.Count * 30);
        }
        // 대량 입력에서도 프로젝트 에셋 검색은 한 번만 합니다.
        foreach (string guid in AssetDatabase.FindAssets("t:MergeItem"))
        {
            var definition = AssetDatabase.LoadAssetAtPath<MergeItem>(AssetDatabase.GUIDToAssetPath(guid));
            if (definition != null && definition.item != null && !existing.ContainsKey(definition.item))
                existing.Add(definition.item, definition);
        }
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Import ItemData as merge nodes");
        int added = 0;
        try
        {
            foreach (var data in incoming)
            {
                if (present.Contains(data)) continue;
                if (!existing.TryGetValue(data, out var definition))
                {
                    definition = CreateDefinition(folder, data);
                    existing.Add(data, definition);
                }
                int rowHeight = 190 + MergeGraphItemNode.DefaultInputCount * 30;
                AddItem(graph, definition, position + new Vector2(added % 5 * 300, added / 5 * rowHeight));
                present.Add(data); added++;
            }
        }
        finally { Undo.CollapseUndoOperations(group); }
        return added;
    }
    public static void AddInput(MergeRecipeGraphAsset graph, MergeGraphItemNode node)
    {
        Undo.RecordObject(graph, "Add merge ingredient input");
        node.ingredients.Add(null); EditorUtility.SetDirty(graph);
    }
    public static void RemoveInput(MergeRecipeGraphAsset graph, MergeGraphItemNode node, int slot)
    {
        Undo.RecordObject(graph, "Remove merge ingredient input");
        node.ingredients.RemoveAt(slot);
        if (slot < node.ingredientNodeIds.Count) node.ingredientNodeIds.RemoveAt(slot);
        EditorUtility.SetDirty(graph);
    }
    internal static MergeGraphItemNode InputNode(MergeRecipeGraphAsset graph, MergeGraphItemNode node, int slot)
    {
        var item = node.ingredients[slot];
        if (item == null) return null;
        string id = slot < node.ingredientNodeIds.Count ? node.ingredientNodeIds[slot] : null;
        return graph.items.Find(candidate => candidate.id == id && candidate.item == item)
            ?? graph.items.Find(candidate => candidate.item == item);
    }
    public static void SetInput(MergeRecipeGraphAsset graph, MergeGraphItemNode node, int slot, MergeItem source, string sourceNodeId = null)
    {
        if (!graph.items.Contains(node) || slot < 0 || slot >= node.ingredients.Count ||
            (source != null && !graph.items.Exists(candidate => candidate.item == source)))
            throw new InvalidOperationException("그래프의 머지 아이템을 재료 입력에 연결하세요.");
        Undo.RecordObject(graph, "Connect merge ingredients");
        node.ingredients[slot] = source;
        while (node.ingredientNodeIds.Count <= slot) node.ingredientNodeIds.Add(null);
        node.ingredientNodeIds[slot] = source != null ? sourceNodeId : null;
        EditorUtility.SetDirty(graph);
    }
    public static void SetTier(MergeRecipeGraphAsset graph, MergeGraphItemNode node, int tier)
    {
        Undo.RecordObject(graph, "Set merge node tier");
        node.tier = Mathf.Max(0, tier); EditorUtility.SetDirty(graph);
    }
    public static void RemoveItem(MergeRecipeGraphAsset graph, MergeGraphItemNode item)
    {
        Undo.RecordObject(graph, "Remove merge item node");
        graph.items.Remove(item);
        bool hasOtherOutput = graph.items.Exists(node => node.item == item.item);
        foreach (var node in graph.items) for (int i = 0; i < node.ingredients.Count; i++)
            if ((!hasOtherOutput && node.ingredients[i] == item.item) ||
                (i < node.ingredientNodeIds.Count && node.ingredientNodeIds[i] == item.id))
            {
                node.ingredients[i] = null;
                if (i < node.ingredientNodeIds.Count) node.ingredientNodeIds[i] = null;
            }
        EditorUtility.SetDirty(graph);
    }
    private static MergeItem Resolve(MergeRecipeGraphAsset graph, ItemData data)
    {
        foreach (var node in graph.items) if (node.item != null && node.item.item == data) return node.item;
        foreach (string guid in AssetDatabase.FindAssets("t:MergeItem"))
        {
            var definition = AssetDatabase.LoadAssetAtPath<MergeItem>(AssetDatabase.GUIDToAssetPath(guid));
            if (definition != null && definition.item == data) return definition;
        }
        return CreateDefinition(Folder(graph), data);
    }
    private static MergeItem CreateDefinition(string folder, ItemData data)
    {
        var item = ScriptableObject.CreateInstance<MergeItem>(); item.name = data.name + " Merge"; item.item = data;
        AssetDatabase.CreateAsset(item, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + SafeName(item.name) + ".asset"));
        return item;
    }
    public static void ImportRecipes(MergeRecipeGraphAsset graph, IEnumerable<ItemRecipeData> recipes)
    {
        var incoming = new List<ItemRecipeData>(recipes);
        foreach (var recipe in incoming)
            if (!MergeTree.TryGetIngredients(recipe, out _)) throw new InvalidOperationException("레시피의 결과와 재료 / 수량을 확인하세요.");
        foreach (var recipe in incoming)
        {
            if (graph.items.Exists(candidate => candidate.generatedRecipe == recipe)) continue;
            MergeTree.TryGetIngredients(recipe, out var materials);
            var sources = new List<MergeItem>();
            foreach (var material in materials)
            {
                var item = Resolve(graph, material.Key);
                AddItem(graph, item, new Vector2(40, 50 + graph.items.Count * 180));
                for (int i = 0; i < material.Value; i++) sources.Add(item);
            }
            var result = Resolve(graph, recipe.resultItem);
            var node = AddItem(graph, result, new Vector2(430, 50 + graph.items.Count * 180));
            Undo.RecordObject(graph, "Import merge recipe node");
            if (node.HasIngredients || node.generatedRecipe != null)
            {
                node = new MergeGraphItemNode { item = result, position = node.position + new Vector2(0, 220) };
                graph.items.Add(node);
            }
            while (sources.Count < MergeGraphItemNode.DefaultInputCount) sources.Add(null);
            node.ingredients = sources; node.tier = recipe.tier; node.generatedRecipe = recipe;
            EditorUtility.SetDirty(graph);
        }
    }
    public static void Upgrade(MergeRecipeGraphAsset graph)
    {
        bool changed = false;
        // 별도 노드가 생겨도 순회가 깨지지 않도록 이전 노드만 복사합니다.
        foreach (var node in graph.items.ToArray())
        {
            if (string.IsNullOrEmpty(node.id)) { node.id = Guid.NewGuid().ToString("N"); changed = true; }
            if (node.item == null && node.LegacyData != null)
            {
                node.item = node.LegacyDefinition != null ? node.LegacyDefinition : Resolve(graph, node.LegacyData);
                node.ClearLegacy(); changed = true;
            }
            if (node.LegacyInputs.Count == 0) continue;
            bool first = true;
            foreach (var pair in node.LegacyInputs)
            {
                if (pair.first == null && pair.second == null && pair.generatedRecipe == null) continue;
                var target = first ? node : new MergeGraphItemNode { item = node.item, position = node.position + new Vector2(0, 220) };
                if (!first) graph.items.Add(target);
                target.ingredients = new List<MergeItem> { pair.first, pair.second };
                target.tier = pair.tier; target.generatedRecipe = pair.generatedRecipe; first = false;
            }
            node.LegacyInputs.Clear(); changed = true;
        }
        if (graph.LegacyRecipes.Count > 0)
        {
            var recipes = new List<ItemRecipeData>();
            foreach (var node in graph.LegacyRecipes) if (node.recipe != null) recipes.Add(node.recipe);
            ImportRecipes(graph, recipes); graph.LegacyRecipes.Clear(); changed = true;
        }
        if (changed) EditorUtility.SetDirty(graph);
        EnsureDefaultInputs(graph);
    }
    private static void EnsureDefaultInputs(MergeRecipeGraphAsset graph)
    {
        if (graph.InputLayoutVersion >= 1) return;
        Undo.RecordObject(graph, "Initialize merge input layout");
        // 이전 빈 노드만 한 번 보완합니다. 이후 사용자가 0개까지 줄여도 다시 늘리지 않습니다.
        foreach (var node in graph.items)
            if (node.ingredients.Count == 0)
                node.ingredients = new List<MergeItem>(new MergeItem[MergeGraphItemNode.DefaultInputCount]);
        graph.InputLayoutVersion = 1;
        EditorUtility.SetDirty(graph);
    }
    public static List<string> Validate(MergeRecipeGraphAsset graph)
    {
        Upgrade(graph);
        var errors = new List<string>();
        var definitions = new HashSet<MergeItem>();
        foreach (var node in graph.items)
        {
            if (node.item == null || node.item.item == null) { errors.Add("레시피 추출 전에 각 MergeItem에 기존 ItemData를 할당하세요."); continue; }
            definitions.Add(node.item);
            if (node.tier < 0) errors.Add(node.item.name + ": 티어는 0 이상이어야 합니다.");
        }
        foreach (var node in graph.items)
        {
            if (!node.HasIngredients) continue;
            foreach (var source in node.ingredients)
            {
                if (source == null) continue;
                if (source.item == null || !definitions.Contains(source))
                { errors.Add("연결한 재료는 ItemData가 할당된 그래프 노드여야 합니다."); continue; }
            }
        }
        return errors;
    }
    // 1단계: 연결된 각 결과 노드로부터 개별 레시피를 추출합니다. 레시피북은 수정하지 않습니다.
    public static List<ItemRecipeData> ExportRecipes(MergeRecipeGraphAsset graph)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("플레이를 종료한 뒤 생성하세요.");
        var errors = Validate(graph);
        if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
        if (graph.items.Count == 0) throw new InvalidOperationException("머지 아이템 노드를 추가하세요.");
        Undo.RecordObject(graph, "Extract merge recipes");
        string folder = Folder(graph) + "/" + SafeName(graph.name) + " Recipes";
        var output = new List<ItemRecipeData>();
        var usedRecipes = new HashSet<ItemRecipeData>();
        foreach (var node in graph.items)
        {
            if (!node.HasIngredients) continue;
            var recipe = node.generatedRecipe;
            // 복제 노드가 같은 추출 에셋을 가리켜도 서로 덮어쓰지 않도록 별도 레시피를 만듭니다.
            if (recipe == null || !usedRecipes.Add(recipe))
            {
                if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(Folder(graph), Path.GetFileName(folder));
                recipe = ScriptableObject.CreateInstance<ItemRecipeData>(); recipe.name = node.item.name + " Recipe";
                AssetDatabase.CreateAsset(recipe, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + SafeName(recipe.name) + ".asset"));
                node.generatedRecipe = recipe;
                usedRecipes.Add(recipe);
            }
            Undo.RecordObject(recipe, "Extract merge recipe");
            recipe.resultItem = node.item.item; recipe.tier = node.tier;
            recipe.itemGrade = recipe.resultItem.grade; recipe.itemSeries = recipe.resultItem.series;
            var counts = new Dictionary<ItemData, int>();
            foreach (var source in node.ingredients)
            {
                if (source == null) continue;
                counts.TryGetValue(source.item, out int amount); counts[source.item] = amount + 1;
            }
            var materials = new List<RecipeMaterial>();
            foreach (var entry in counts) materials.Add(new RecipeMaterial { itemData = entry.Key, amount = entry.Value });
            recipe.materials = materials.ToArray();
            EditorUtility.SetDirty(recipe); AssetDatabase.SaveAssetIfDirty(recipe); output.Add(recipe);
        }
        EditorUtility.SetDirty(graph); Save(graph);
        return output;
    }
    // 2단계: 추출한 레시피를 결과 노드의 티어별로 묶어 레시피북을 만듭니다.
    public static MergeRecipeBook Export(MergeRecipeGraphAsset graph)
    {
        var recipes = ExportRecipes(graph);
        Undo.RecordObject(graph, "Generate merge recipe book");
        if (graph.recipeBook == null)
        {
            graph.recipeBook = ScriptableObject.CreateInstance<MergeRecipeBook>();
            AssetDatabase.CreateAsset(graph.recipeBook, AssetDatabase.GenerateUniqueAssetPath(Folder(graph) + "/" + SafeName(graph.name) + " Book.asset"));
        }
        var tiers = new SortedDictionary<int, MergeSeries>();
        foreach (var recipe in recipes)
        {
            if (!tiers.TryGetValue(recipe.tier, out var tier)) tiers.Add(recipe.tier, tier = new MergeSeries { tier = recipe.tier });
            tier.itemRecipeDatas.Add(recipe);
        }
        Undo.RecordObject(graph.recipeBook, "Group merge recipes by tier");
        graph.recipeBook.items = new List<MergeItem>();
        foreach (var node in graph.items) if (!graph.recipeBook.items.Contains(node.item)) graph.recipeBook.items.Add(node.item);
        graph.recipeBook.tiers = new List<MergeSeries>(tiers.Values);
        EditorUtility.SetDirty(graph.recipeBook); EditorUtility.SetDirty(graph);
        AssetDatabase.SaveAssetIfDirty(graph.recipeBook); Save(graph);
        return graph.recipeBook;
    }
    public static void Apply(MergeRecipeGraphAsset graph, MergegameManager manager)
    {
        if (manager == null) throw new InvalidOperationException("머지 매니저를 연결하세요.");
        var book = Export(graph); Undo.RecordObject(manager, "Connect merge recipe book"); manager.recipeBook = book;
        EditorUtility.SetDirty(manager); PrefabUtility.RecordPrefabInstancePropertyModifications(manager);
        if (manager.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
    }
    public static void ImportBook(MergeRecipeGraphAsset graph, MergeRecipeBook book)
    {
        if (book == null) throw new InvalidOperationException("레시피북을 연결하세요.");
        var recipes = new List<ItemRecipeData>();
        foreach (var tier in book.tiers) if (tier?.itemRecipeDatas != null) recipes.AddRange(tier.itemRecipeDatas);
        foreach (var recipe in recipes) if (!MergeTree.TryGetIngredients(recipe, out _)) throw new InvalidOperationException("레시피의 결과와 재료 / 수량을 확인하세요.");
        Undo.RecordObject(graph, "Import merge recipe book");
        var old = graph.items;
        graph.items = new List<MergeGraphItemNode>(); graph.recipeBook = book; graph.LegacyRecipes.Clear();
        foreach (var item in book.items) if (item != null)
        {
            var node = AddItem(graph, item, new Vector2(40, graph.items.Count * 180));
            var previous = old.Find(value => value.item == item);
            if (previous != null) { node.position = previous.position; node.tier = previous.tier; }
        }
        ImportRecipes(graph, recipes);
        foreach (var node in graph.items)
        {
            var previous = old.Find(value => value.item == node.item && value.generatedRecipe == node.generatedRecipe);
            if (previous != null) node.position = previous.position;
        }
        EditorUtility.SetDirty(graph);
    }
    public static void Import(MergeRecipeGraphAsset graph, MergegameManager manager)
    {
        if (manager == null || manager.recipeBook == null) throw new InvalidOperationException("매니저의 레시피북을 연결하세요.");
        ImportBook(graph, manager.recipeBook);
    }
    public static void Arrange(MergeRecipeGraphAsset graph) => MergeRecipeGraphLayout.Arrange(graph);

    public static void Save(MergeRecipeGraphAsset graph)
    {
        Upgrade(graph); AssetDatabase.SaveAssetIfDirty(graph);
        foreach (var node in graph.items) if (node.item != null)
        {
            AssetDatabase.SaveAssetIfDirty(node.item);
            if (node.item.item != null) AssetDatabase.SaveAssetIfDirty(node.item.item);
        }
    }
}
