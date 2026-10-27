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
    private static GameObject CreatePrefab(MergeRecipeGraphAsset graph, string name)
    {
        if (graph.defaultPrefab == null) return null;
        if (!PrefabUtility.IsPartOfPrefabAsset(graph.defaultPrefab)) throw new InvalidOperationException("Project의 프리팹을 템플릿으로 연결하세요.");
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(graph.defaultPrefab, preview);
            return PrefabUtility.SaveAsPrefabAsset(instance, AssetDatabase.GenerateUniqueAssetPath(Folder(graph) + "/" + SafeName(name) + ".prefab"));
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }
    private static MergeItem CreateDefinition(MergeRecipeGraphAsset graph, ItemData data)
    {
        var item = ScriptableObject.CreateInstance<MergeItem>();
        item.name = data.name + " Merge";
        item.item = data;
        item.prefab = CreatePrefab(graph, item.name);
        AssetDatabase.CreateAsset(item, AssetDatabase.GenerateUniqueAssetPath(Folder(graph) + "/" + SafeName(item.name) + ".asset"));
        return item;
    }
    public static MergeItem CreateItem(MergeRecipeGraphAsset graph, Vector2 position)
    {
        var data = ScriptableObject.CreateInstance<ItemData>();
        data.name = "Merge Item " + (graph.items.Count + 1);
        data.series = graph.defaultSeries;
        AssetDatabase.CreateAsset(data, AssetDatabase.GenerateUniqueAssetPath(Folder(graph) + "/" + data.name + " Data.asset"));
        var item = CreateDefinition(graph, data);
        AddItem(graph, item, position);
        return item;
    }
    public static MergeGraphItemNode AddItem(MergeRecipeGraphAsset graph, MergeItem item, Vector2 position)
    {
        if (item == null) return null;
        var existing = graph.items.Find(node => node.item == item);
        if (existing != null) return existing;
        Undo.RecordObject(graph, "Add merge item node");
        var node = new MergeGraphItemNode { item = item, position = position };
        graph.items.Add(node);
        EditorUtility.SetDirty(graph);
        return node;
    }
    public static MergeGraphInputPair AddCombination(MergeRecipeGraphAsset graph, MergeGraphItemNode result)
    {
        Undo.RecordObject(graph, "Add merge combination");
        var pair = new MergeGraphInputPair();
        result.inputs.Add(pair);
        EditorUtility.SetDirty(graph);
        return pair;
    }
    public static void SetInput(MergeRecipeGraphAsset graph, MergeGraphInputPair pair, int slot, MergeItem source)
    {
        if (slot < 0 || slot > 1 || (source != null && !graph.items.Exists(node => node.item == source)))
            throw new InvalidOperationException("그래프의 머지 아이템을 A / B에 연결하세요.");
        Undo.RecordObject(graph, "Connect merge items");
        if (slot == 0) pair.first = source; else pair.second = source;
        EditorUtility.SetDirty(graph);
    }
    public static void RemoveItem(MergeRecipeGraphAsset graph, MergeGraphItemNode item)
    {
        Undo.RecordObject(graph, "Remove merge item node");
        foreach (var node in graph.items) foreach (var pair in node.inputs)
        {
            if (pair.first == item.item) pair.first = null;
            if (pair.second == item.item) pair.second = null;
        }
        graph.items.Remove(item);
        EditorUtility.SetDirty(graph);
    }
    public static void RemoveCombination(MergeRecipeGraphAsset graph, MergeGraphItemNode node, MergeGraphInputPair pair)
    {
        Undo.RecordObject(graph, "Remove merge combination");
        node.inputs.Remove(pair);
        if (node.inputs.Count == 0) node.inputs.Add(new MergeGraphInputPair());
        EditorUtility.SetDirty(graph);
    }

    public static bool TryInputs(ItemRecipeData recipe, out ItemData first, out ItemData second)
    {
        first = second = null;
        if (recipe == null) return false;
        if (recipe.materials == null) return true;
        int count = 0;
        foreach (var material in recipe.materials)
        {
            int amount = material != null ? material.amount : 1;
            if (material?.itemData == null && amount == 0) amount = 1;
            if (amount < 1 || amount > 2 - count) return false;
            for (int i = 0; i < amount; i++)
                if (count++ == 0) first = material?.itemData; else second = material?.itemData;
        }
        return true;
    }
    private static MergeItem Resolve(MergeRecipeGraphAsset graph, ItemData data)
    {
        if (data == null) return null;
        foreach (var node in graph.items) if (node.item != null && node.item.item == data) return node.item;
        foreach (string guid in AssetDatabase.FindAssets("t:MergeItem"))
        {
            var definition = AssetDatabase.LoadAssetAtPath<MergeItem>(AssetDatabase.GUIDToAssetPath(guid));
            if (definition != null && definition.item == data) return definition;
        }
        return CreateDefinition(graph, data);
    }
    public static void ImportRecipes(MergeRecipeGraphAsset graph, IEnumerable<ItemRecipeData> recipes)
    {
        var incoming = new List<ItemRecipeData>(recipes);
        foreach (var recipe in incoming)
            if (!TryInputs(recipe, out _, out _)) throw new InvalidOperationException("두 재료를 사용하는 레시피만 불러올 수 있습니다.");
        foreach (var recipe in incoming)
        {
            if (recipe.resultItem == null) continue;
            TryInputs(recipe, out var a, out var b);
            var first = Resolve(graph, a); var second = Resolve(graph, b); var result = Resolve(graph, recipe.resultItem);
            if (first != null) AddItem(graph, first, new Vector2(40, 50 + graph.items.Count * 180));
            if (second != null) AddItem(graph, second, new Vector2(40, 50 + graph.items.Count * 180));
            var node = AddItem(graph, result, new Vector2(430, 50 + graph.items.Count * 180));
            if (node.inputs.Exists(pair => pair.generatedRecipe == recipe)) continue;
            Undo.RecordObject(graph, "Import merge combination");
            var pair = node.inputs.Find(candidate => candidate.IsEmpty && candidate.generatedRecipe == null);
            if (pair == null) { pair = new MergeGraphInputPair(); node.inputs.Add(pair); }
            pair.first = first; pair.second = second; pair.tier = recipe.tier; pair.generatedRecipe = recipe;
            EditorUtility.SetDirty(graph);
        }
    }
    public static void Upgrade(MergeRecipeGraphAsset graph)
    {
        bool changed = false;
        foreach (var node in graph.items)
        {
            if (node.item == null && node.LegacyData != null)
            {
                node.item = node.LegacyDefinition != null ? node.LegacyDefinition : Resolve(graph, node.LegacyData);
                node.ClearLegacy(); changed = true;
            }
            if (node.inputs == null || node.inputs.Count == 0)
            { node.inputs = new List<MergeGraphInputPair> { new MergeGraphInputPair() }; changed = true; }
        }
        if (graph.LegacyRecipes.Count > 0)
        {
            var recipes = new List<ItemRecipeData>();
            foreach (var node in graph.LegacyRecipes) if (node.recipe != null && node.recipe.resultItem != null) recipes.Add(node.recipe);
            ImportRecipes(graph, recipes);
            graph.LegacyRecipes.RemoveAll(node => node.recipe == null || node.recipe.resultItem != null);
            changed = true;
        }
        if (changed) EditorUtility.SetDirty(graph);
    }

    public static List<string> Validate(MergeRecipeGraphAsset graph)
    {
        Upgrade(graph);
        var errors = new List<string>();
        var definitions = new HashSet<MergeItem>();
        var data = new HashSet<ItemData>();
        foreach (var node in graph.items)
        {
            if (node.item == null || node.item.item == null) { errors.Add("머지 아이템과 ItemData를 연결하세요."); continue; }
            if (!definitions.Add(node.item) || !data.Add(node.item.item)) errors.Add(node.item.name + ": 아이템 노드가 중복됩니다.");
        }
        var used = new List<MergeGraphInputPair>();
        var generated = new HashSet<ItemRecipeData>();
        foreach (var node in graph.items) foreach (var pair in node.inputs)
        {
            if (pair.IsEmpty) continue;
            if (pair.first == null || pair.second == null) { errors.Add(node.item != null ? node.item.name + ": 재료 A · B를 모두 연결하세요." : "재료 A · B를 모두 연결하세요."); continue; }
            if (!definitions.Contains(pair.first) || !definitions.Contains(pair.second)) errors.Add("연결된 재료 노드가 그래프에 없습니다.");
            if (pair.tier < 0) errors.Add("티어는 0 이상이어야 합니다.");
            foreach (var other in used)
                if ((other.first.item == pair.first.item && other.second.item == pair.second.item) ||
                    (other.first.item == pair.second.item && other.second.item == pair.first.item))
                { errors.Add("동일한 재료 조합이 중복됩니다."); break; }
            used.Add(pair);
            if (pair.generatedRecipe != null && !generated.Add(pair.generatedRecipe)) errors.Add("같은 레시피 에셋이 여러 조합에 연결되어 있습니다.");
        }
        if (graph.LegacyRecipes.Count > 0) errors.Add("이전 레시피 에셋의 결과 아이템을 먼저 지정하세요.");
        return errors;
    }
    public static MergeRecipeBook Export(MergeRecipeGraphAsset graph)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("플레이를 종료한 뒤 생성하세요.");
        var errors = Validate(graph);
        if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
        if (graph.items.Count == 0) throw new InvalidOperationException("머지 아이템 노드를 추가하세요.");
        Undo.RecordObject(graph, "Generate merge recipe book");
        if (graph.recipeBook == null)
        {
            graph.recipeBook = ScriptableObject.CreateInstance<MergeRecipeBook>();
            AssetDatabase.CreateAsset(graph.recipeBook, AssetDatabase.GenerateUniqueAssetPath(Folder(graph) + "/" + SafeName(graph.name) + " Book.asset"));
        }
        string folder = Folder(graph) + "/" + SafeName(graph.name) + " Recipes";
        var tiers = new SortedDictionary<int, MergeSeries>();
        foreach (var node in graph.items)
        for (int index = 0; index < node.inputs.Count; index++)
        {
            var pair = node.inputs[index];
            if (pair.IsEmpty) continue;
            var recipe = pair.generatedRecipe;
            if (recipe == null)
            {
                if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(Folder(graph), Path.GetFileName(folder));
                recipe = ScriptableObject.CreateInstance<ItemRecipeData>();
                recipe.name = node.item.name + " Recipe " + (index + 1);
                AssetDatabase.CreateAsset(recipe, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + SafeName(recipe.name) + ".asset"));
                pair.generatedRecipe = recipe;
            }
            Undo.RecordObject(recipe, "Generate merge recipe");
            recipe.resultItem = node.item.item;
            recipe.itemGrade = recipe.resultItem.grade; recipe.itemSeries = recipe.resultItem.series; recipe.tier = pair.tier;
            recipe.materials = pair.first.item == pair.second.item
                ? new[] { new RecipeMaterial { itemData = pair.first.item, amount = 2 } }
                : new[] { new RecipeMaterial { itemData = pair.first.item, amount = 1 }, new RecipeMaterial { itemData = pair.second.item, amount = 1 } };
            EditorUtility.SetDirty(recipe);
            AssetDatabase.SaveAssetIfDirty(recipe);
            if (!tiers.TryGetValue(pair.tier, out var tier)) tiers.Add(pair.tier, tier = new MergeSeries { tier = pair.tier });
            tier.itemRecipeDatas.Add(recipe);
        }
        Undo.RecordObject(graph.recipeBook, "Update merge recipe book");
        graph.recipeBook.items = new List<MergeItem>();
        foreach (var node in graph.items) graph.recipeBook.items.Add(node.item);
        graph.recipeBook.tiers = new List<MergeSeries>(tiers.Values);
        EditorUtility.SetDirty(graph.recipeBook); EditorUtility.SetDirty(graph);
        AssetDatabase.SaveAssetIfDirty(graph.recipeBook);
        Save(graph);
        return graph.recipeBook;
    }
    public static void Apply(MergeRecipeGraphAsset graph, MergegameManager manager)
    {
        if (manager == null) throw new InvalidOperationException("머지 매니저를 연결하세요.");
        var book = Export(graph);
        Undo.RecordObject(manager, "Connect merge recipe book");
        manager.recipeBook = book;
        EditorUtility.SetDirty(manager);
        PrefabUtility.RecordPrefabInstancePropertyModifications(manager);
        if (manager.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
    }
    public static void ImportBook(MergeRecipeGraphAsset graph, MergeRecipeBook book)
    {
        if (book == null) throw new InvalidOperationException("레시피북을 연결하세요.");
        Undo.RecordObject(graph, "Import merge recipe book");
        var positions = new Dictionary<MergeItem, Vector2>();
        foreach (var node in graph.items) if (node.item != null) positions[node.item] = node.position;
        graph.items = new List<MergeGraphItemNode>(); graph.recipeBook = book;
        foreach (var item in book.items) if (item != null)
            AddItem(graph, item, positions.TryGetValue(item, out var position) ? position : new Vector2(40, graph.items.Count * 180));
        var recipes = new List<ItemRecipeData>();
        foreach (var tier in book.tiers) if (tier?.itemRecipeDatas != null) recipes.AddRange(tier.itemRecipeDatas);
        ImportRecipes(graph, recipes);
        EditorUtility.SetDirty(graph);
    }
    public static void Import(MergeRecipeGraphAsset graph, MergegameManager manager)
    {
        if (manager == null || manager.recipeBook == null) throw new InvalidOperationException("매니저의 레시피북을 연결하세요.");
        ImportBook(graph, manager.recipeBook);
    }
    public static void Arrange(MergeRecipeGraphAsset graph)
    {
        Undo.RecordObject(graph, "Arrange merge items");
        var levels = new Dictionary<MergeItem, int>();
        foreach (var node in graph.items) if (node.item != null) levels[node.item] = 0;
        for (int pass = 0; pass < graph.items.Count; pass++)
        {
            bool changed = false;
            foreach (var node in graph.items)
            {
                if (node.item == null) continue;
                int level = levels[node.item];
                foreach (var pair in node.inputs)
                    if (!pair.IsEmpty && pair.first != null && pair.second != null && levels.ContainsKey(pair.first) && levels.ContainsKey(pair.second))
                        level = Math.Max(level, Math.Min(graph.items.Count, Math.Max(levels[pair.first], levels[pair.second]) + 1));
                if (level != levels[node.item]) { levels[node.item] = level; changed = true; }
            }
            if (!changed) break;
        }
        var rows = new Dictionary<int, int>();
        foreach (var node in graph.items)
        {
            int level = node.item != null ? levels[node.item] : 0;
            rows.TryGetValue(level, out int row);
            node.position = new Vector2(40 + level * 380, 50 + row * 230);
            rows[level] = row + 1;
        }
        EditorUtility.SetDirty(graph);
    }
    public static void Save(MergeRecipeGraphAsset graph)
    {
        Upgrade(graph);
        AssetDatabase.SaveAssetIfDirty(graph);
        foreach (var node in graph.items) if (node.item != null)
        {
            AssetDatabase.SaveAssetIfDirty(node.item);
            if (node.item.item != null) AssetDatabase.SaveAssetIfDirty(node.item.item);
        }
    }
}
