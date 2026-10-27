using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 에셋 변경과 매니저 적용을 화면 코드에서 분리해 실제 게임 레시피 판정까지 검사합니다.
public static class MergeRecipeGraphEditing
{
    private static string Folder(MergeRecipeGraphAsset graph)
    {
        string path = AssetDatabase.GetAssetPath(graph);
        if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("그래프를 먼저 에셋으로 저장하세요.");
        return Path.GetDirectoryName(path).Replace('\\', '/');
    }

    private static string FileName(string name)
    {
        foreach (char value in "/\\:*?\"<>|") name = name.Replace(value, '_');
        return string.IsNullOrWhiteSpace(name) ? "Merge Item" : name;
    }

    private static GameObject CreatePrefab(MergeRecipeGraphAsset graph, string name)
    {
        if (graph.defaultPrefab == null) return null;
        if (!PrefabUtility.IsPartOfPrefabAsset(graph.defaultPrefab))
            throw new InvalidOperationException("기본 프리팹에는 Project의 프리팹 에셋을 연결하세요.");
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(graph.defaultPrefab, preview);
            string path = AssetDatabase.GenerateUniqueAssetPath(Folder(graph) + "/" + FileName(name) + ".prefab");
            return PrefabUtility.SaveAsPrefabAsset(instance, path);
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }

    public static MergeItem CreateItem(MergeRecipeGraphAsset graph, Vector2 position)
    {
        string folder = Folder(graph);
        var item = ScriptableObject.CreateInstance<ItemData>();
        item.name = "Merge Item " + (graph.items.Count + 1);
        item.series = graph.defaultSeries;
        AssetDatabase.CreateAsset(item, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + item.name + " Data.asset"));
        var definition = ScriptableObject.CreateInstance<MergeItem>();
        definition.name = item.name;
        definition.item = item;
        definition.prefab = CreatePrefab(graph, item.name);
        AssetDatabase.CreateAsset(definition, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + item.name + ".asset"));
        AddItem(graph, definition, position);
        return definition;
    }

    public static void AddItem(MergeRecipeGraphAsset graph, MergeItem item, Vector2 position)
    {
        if (item == null || graph.items.Exists(node => node.item == item)) return;
        Undo.RecordObject(graph, "Add merge item node");
        graph.items.Add(new MergeGraphItemNode { item = item, position = position });
        EditorUtility.SetDirty(graph);
    }

    public static ItemRecipeData CreateRecipe(MergeRecipeGraphAsset graph, Vector2 position)
    {
        var recipe = ScriptableObject.CreateInstance<ItemRecipeData>();
        recipe.name = "Merge Recipe " + (graph.recipes.Count + 1);
        recipe.itemSeries = graph.defaultSeries;
        recipe.materials = new[] { new RecipeMaterial { amount = 1 }, new RecipeMaterial { amount = 1 } };
        AssetDatabase.CreateAsset(recipe, AssetDatabase.GenerateUniqueAssetPath(Folder(graph) + "/" + recipe.name + ".asset"));
        AddRecipe(graph, recipe, position);
        return recipe;
    }

    public static void AddRecipe(MergeRecipeGraphAsset graph, ItemRecipeData recipe, Vector2 position)
    {
        if (recipe == null || graph.recipes.Exists(node => node.recipe == recipe)) return;
        if (!TryInputs(recipe, out _, out _))
            throw new InvalidOperationException("두 개의 재료를 사용하는 레시피만 머지 그래프에 추가할 수 있습니다.");
        Undo.RecordObject(graph, "Add merge recipe node");
        graph.recipes.Add(new MergeGraphRecipeNode { recipe = recipe, position = position });
        EditorUtility.SetDirty(graph);
    }

    // 빈 포트는 null로 남깁니다. 같은 재료를 두 포트에 연결하면 실제 레시피는 수량 2로 저장합니다.
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

    public static void SetInput(ItemRecipeData recipe, int slot, ItemData item)
    {
        if (slot < 0 || slot > 1 || !TryInputs(recipe, out var first, out var second))
            throw new InvalidOperationException("레시피의 재료 포트를 확인하세요.");
        Undo.RecordObject(recipe, "Connect merge recipe ingredient");
        if (slot == 0) first = item; else second = item;
        recipe.materials = first != null && first == second
            ? new[] { new RecipeMaterial { itemData = first, amount = 2 } }
            : new[] { new RecipeMaterial { itemData = first, amount = 1 }, new RecipeMaterial { itemData = second, amount = 1 } };
        EditorUtility.SetDirty(recipe);
    }

    public static void SetResult(ItemRecipeData recipe, ItemData item)
    {
        if (recipe == null) throw new InvalidOperationException("레시피 에셋을 먼저 연결하세요.");
        Undo.RecordObject(recipe, "Connect merge recipe result");
        recipe.resultItem = item;
        if (item != null) { recipe.itemGrade = item.grade; recipe.itemSeries = item.series; }
        EditorUtility.SetDirty(recipe);
    }

    public static MergeItem Definition(MergeRecipeGraphAsset graph, ItemData item)
    {
        if (item == null) return null;
        foreach (var node in graph.items) if (node.item != null && node.item.item == item) return node.item;
        return null;
    }

    // 레시피를 역으로 읽어 재료 / 결과 노드까지 복원합니다. 원본 레시피는 변경하지 않습니다.
    public static void ImportRecipes(MergeRecipeGraphAsset graph, IEnumerable<ItemRecipeData> recipes)
    {
        var incoming = new List<ItemRecipeData>(recipes);
        foreach (var recipe in incoming)
            if (!TryInputs(recipe, out _, out _))
                throw new InvalidOperationException((recipe != null ? recipe.name : "레시피 없음") + ": 머지는 재료 두 개를 사용하는 레시피만 지원합니다.");
        var available = new List<MergeItem>();
        foreach (string guid in AssetDatabase.FindAssets("t:MergeItem"))
        {
            var definition = AssetDatabase.LoadAssetAtPath<MergeItem>(AssetDatabase.GUIDToAssetPath(guid));
            if (definition != null) available.Add(definition);
        }
        foreach (var recipe in incoming)
        {
            TryInputs(recipe, out var first, out var second);
            foreach (var data in new[] { first, second, recipe.resultItem })
            {
                if (data == null || Definition(graph, data) != null) continue;
                var definition = available.Find(item => item.item == data);
                if (definition == null)
                {
                    definition = ScriptableObject.CreateInstance<MergeItem>();
                    definition.name = data.name + " Merge";
                    definition.item = data;
                    definition.prefab = CreatePrefab(graph, definition.name);
                    AssetDatabase.CreateAsset(definition, AssetDatabase.GenerateUniqueAssetPath(Folder(graph) + "/" + FileName(definition.name) + ".asset"));
                    available.Add(definition);
                }
                AddItem(graph, definition, new Vector2(data == recipe.resultItem ? 580 : 40, 50 + graph.items.Count * 160));
            }
            AddRecipe(graph, recipe, new Vector2(310, 50 + graph.recipes.Count * 180));
        }
    }

    public static List<string> Validate(MergeRecipeGraphAsset graph)
    {
        var errors = new List<string>();
        var definitions = new HashSet<ItemData>();
        foreach (var node in graph.items)
        {
            if (node.item == null || node.item.item == null) { errors.Add("아이템 노드에 MergeItem / ItemData가 필요합니다."); continue; }
            if (!definitions.Add(node.item.item)) errors.Add(node.item.name + ": 같은 ItemData를 사용하는 노드가 중복됩니다.");
            if (node.item.prefab == null || node.item.prefab.GetComponent<MergeItemObject>() == null ||
                !node.item.prefab.GetComponent<MergeItemObject>().enabled)
                errors.Add(node.item.name + ": 활성 MergeItemObject가 있는 프리팹을 연결하세요.");
        }
        var complete = new List<ItemRecipeData>();
        foreach (var node in graph.recipes)
        {
            var recipe = node.recipe;
            if (!TryInputs(recipe, out var a, out var b) || a == null || b == null || recipe.resultItem == null)
            { errors.Add((recipe != null ? recipe.name : "레시피") + ": 재료 A · B와 결과를 모두 연결하세요."); continue; }
            if (!definitions.Contains(a) || !definitions.Contains(b) || !definitions.Contains(recipe.resultItem))
                errors.Add(recipe.name + ": 재료 / 결과의 머지 아이템 노드가 그래프에 없습니다.");
            if (recipe.tier < 0) errors.Add(recipe.name + ": 티어는 0 이상이어야 합니다.");
            foreach (var previous in complete)
                if (MergeTree.MatchesPair(previous, a, b)) { errors.Add(recipe.name + ": 동일한 재료 조합이 중복됩니다."); break; }
            complete.Add(recipe);
        }
        return errors;
    }

    public static void Apply(MergeRecipeGraphAsset graph, MergegameManager manager)
    {
        if (manager == null) throw new InvalidOperationException("적용할 머지 매니저를 연결하세요.");
        if (EditorApplication.isPlaying) throw new InvalidOperationException("플레이를 종료한 뒤 적용하세요.");
        var errors = Validate(graph);
        if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
        Undo.RecordObject(manager, "Apply merge recipe graph");
        var items = new List<MergeItem>();
        foreach (var node in graph.items) items.Add(node.item);
        var tiers = new SortedDictionary<int, MergeSeries>();
        foreach (var node in graph.recipes)
        {
            int tier = node.recipe.tier;
            if (!tiers.TryGetValue(tier, out var series)) tiers.Add(tier, series = new MergeSeries { tier = tier });
            series.itemRecipeDatas.Add(node.recipe);
        }
        // 참조 소스나 런타임 복사 없이 각 매니저의 설정 자체를 저장합니다. 투입 재고 / UI는 유지합니다.
        manager.mergeItems = items;
        manager.mergeRecipe = new List<MergeSeries>(tiers.Values);
        EditorUtility.SetDirty(manager);
        PrefabUtility.RecordPrefabInstancePropertyModifications(manager);
        if (manager.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
    }

    public static void Import(MergeRecipeGraphAsset graph, MergegameManager manager)
    {
        if (manager == null) throw new InvalidOperationException("불러올 머지 매니저를 연결하세요.");
        foreach (var tier in manager.mergeRecipe)
            if (tier?.itemRecipeDatas != null) foreach (var recipe in tier.itemRecipeDatas)
                if (recipe != null && !TryInputs(recipe, out _, out _))
                    throw new InvalidOperationException(recipe.name + ": 머지는 재료 두 개를 사용하는 레시피만 지원합니다.");
        Undo.RecordObject(graph, "Import merge manager graph");
        graph.defaultSeries = manager.itemSeries;
        var oldItems = graph.items;
        var oldRecipes = graph.recipes;
        graph.items = new List<MergeGraphItemNode>();
        graph.recipes = new List<MergeGraphRecipeNode>();
        var definitions = new List<MergeItem>(manager.mergeItems);
        foreach (var material in manager.mergeMaterials)
            if (material?.mergeItem != null && !definitions.Contains(material.mergeItem)) definitions.Add(material.mergeItem);
        foreach (var item in definitions)
        {
            if (item == null || graph.items.Exists(node => node.item == item)) continue;
            var old = oldItems.Find(node => node.item == item);
            graph.items.Add(new MergeGraphItemNode { item = item, position = old != null ? old.position : new Vector2(40, graph.items.Count * 160) });
        }
        foreach (var tier in manager.mergeRecipe)
        {
            if (tier?.itemRecipeDatas == null) continue;
            foreach (var recipe in tier.itemRecipeDatas)
            {
                if (recipe == null || graph.recipes.Exists(node => node.recipe == recipe)) continue;
                var old = oldRecipes.Find(node => node.recipe == recipe);
                graph.recipes.Add(new MergeGraphRecipeNode { recipe = recipe, position = old != null ? old.position : new Vector2(320, graph.recipes.Count * 180) });
            }
        }
        var imported = new List<ItemRecipeData>();
        foreach (var node in graph.recipes) imported.Add(node.recipe);
        ImportRecipes(graph, imported);
        EditorUtility.SetDirty(graph);
    }

    public static void Arrange(MergeRecipeGraphAsset graph)
    {
        Undo.RecordObject(graph, "Arrange merge recipe graph");
        // 실제 티어 값이 0, 10, 42처럼 떨어져 있어도 편집 화면에는 빈 열을 만들지 않습니다.
        var ranks = new SortedDictionary<int, int>();
        foreach (var node in graph.recipes)
            if (node.recipe != null) ranks[Math.Max(0, node.recipe.tier)] = 0;
        var tierKeys = new List<int>(ranks.Keys);
        for (int i = 0; i < tierKeys.Count; i++) ranks[tierKeys[i]] = i;
        var columns = new Dictionary<int, int>();
        foreach (var node in graph.items)
        {
            int tier = 0;
            foreach (var recipe in graph.recipes)
                if (recipe.recipe != null && node.item != null && recipe.recipe.resultItem == node.item.item)
                    tier = Math.Max(tier, ranks[Math.Max(0, recipe.recipe.tier)] + 1);
            columns.TryGetValue(tier * 2, out int row);
            node.position = new Vector2(40 + tier * 540, 50 + row * 180);
            columns[tier * 2] = row + 1;
        }
        foreach (var node in graph.recipes)
        {
            int tier = node.recipe != null ? ranks[Math.Max(0, node.recipe.tier)] : 0;
            columns.TryGetValue(tier * 2 + 1, out int row);
            node.position = new Vector2(310 + tier * 540, 50 + row * 180);
            columns[tier * 2 + 1] = row + 1;
        }
        EditorUtility.SetDirty(graph);
    }

    public static void Save(MergeRecipeGraphAsset graph)
    {
        AssetDatabase.SaveAssetIfDirty(graph);
        foreach (var node in graph.items)
            if (node.item != null)
            {
                AssetDatabase.SaveAssetIfDirty(node.item);
                if (node.item.item != null) AssetDatabase.SaveAssetIfDirty(node.item.item);
            }
        foreach (var node in graph.recipes) if (node.recipe != null) AssetDatabase.SaveAssetIfDirty(node.recipe);
    }
}
