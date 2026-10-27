using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MergeManagerFolderImport
{
    private static List<T> Read<T>(string folder, string filter, bool recursive) where T : UnityEngine.Object
    {
        if (!AssetDatabase.IsValidFolder(folder)) throw new InvalidOperationException("올바른 에셋 폴더를 연결하세요.");
        var paths = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets(filter, new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (recursive || Path.GetDirectoryName(path).Replace('\\', '/') == folder) paths.Add(path);
        }
        paths.Sort(StringComparer.Ordinal);
        var assets = new List<T>();
        foreach (string path in paths)
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) assets.Add(asset);
        }
        return assets;
    }

    public static void ApplyItems(MergegameManager manager, string folder, bool recursive, bool filterSeries)
    {
        Guard(manager);
        var items = Read<MergeItem>(folder, "t:MergeItem", recursive);
        var output = new List<MergeItem>();
        var ids = new HashSet<ItemData>();
        foreach (var item in items)
        {
            if (filterSeries && manager.itemSeries != ItemSeries.None && item.item != null && item.item.series != manager.itemSeries) continue;
            if (item.item == null || (item.prefab == null || item.prefab.GetComponent<MergeItemObject>() == null || !item.prefab.GetComponent<MergeItemObject>().enabled))
                throw new InvalidOperationException(item.name + ": ItemData와 활성 MergeItemObject 프리팹을 연결하세요.");
            if (!ids.Add(item.item)) throw new InvalidOperationException("동일한 ItemData의 MergeItem이 중복됩니다: " + item.item.name);
            output.Add(item);
        }
        if (output.Count == 0) throw new InvalidOperationException("조건에 맞는 MergeItem이 없습니다.");
        Undo.RecordObject(manager, "Import merge items folder");
        manager.mergeItems = output;
        Dirty(manager);
    }

    public static void ApplyRecipes(MergegameManager manager, string folder, bool recursive, bool filterSeries)
    {
        Guard(manager);
        var tiers = new SortedDictionary<int, MergeSeries>();
        foreach (var recipe in Read<ItemRecipeData>(folder, "t:ItemRecipeData", recursive))
        {
            var series = recipe.resultItem != null ? recipe.resultItem.series : recipe.itemSeries;
            if (filterSeries && manager.itemSeries != ItemSeries.None && series != manager.itemSeries) continue;
            if (!MergeRecipeGraphEditing.TryInputs(recipe, out var a, out var b) || a == null || b == null ||
                recipe.resultItem == null || recipe.tier < 0)
                throw new InvalidOperationException(recipe.name + ": 두 재료와 결과 / 티어를 확인하세요.");
            foreach (var previous in tiers.Values)
                foreach (var other in previous.itemRecipeDatas)
                    if (MergeTree.MatchesPair(other, a, b)) throw new InvalidOperationException("재료 조합이 중복됩니다: " + recipe.name);
            if (!tiers.TryGetValue(recipe.tier, out var tier)) tiers.Add(recipe.tier, tier = new MergeSeries { tier = recipe.tier });
            tier.itemRecipeDatas.Add(recipe);
        }
        if (tiers.Count == 0) throw new InvalidOperationException("조건에 맞는 레시피가 없습니다.");
        Undo.RecordObject(manager, "Import merge recipes folder");
        manager.mergeRecipe = new List<MergeSeries>(tiers.Values);
        Dirty(manager);
    }

    private static void Guard(MergegameManager manager)
    {
        if (manager == null || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("플레이를 종료하고 적용할 매니저를 연결하세요.");
    }
    private static void Dirty(MergegameManager manager)
    {
        EditorUtility.SetDirty(manager);
        PrefabUtility.RecordPrefabInstancePropertyModifications(manager);
        if (manager.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
    }
}

[CustomEditor(typeof(MergegameManager))]
public sealed class MergegameManagerEditor : Editor
{
    [SerializeField] private DefaultAsset itemsFolder, recipesFolder;
    [SerializeField] private bool recursive = true, filterSeries = true;
    private string status;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("폴더에서 데이터 주입", EditorStyles.boldLabel);
        itemsFolder = (DefaultAsset)EditorGUILayout.ObjectField("MergeItem 폴더", itemsFolder, typeof(DefaultAsset), false);
        recipesFolder = (DefaultAsset)EditorGUILayout.ObjectField("레시피 폴더", recipesFolder, typeof(DefaultAsset), false);
        recursive = EditorGUILayout.Toggle("하위 폴더 포함", recursive);
        filterSeries = EditorGUILayout.Toggle("매니저의 시리즈만", filterSeries);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("Merge Items 채우기"))
                Run(() => MergeManagerFolderImport.ApplyItems((MergegameManager)target, AssetDatabase.GetAssetPath(itemsFolder), recursive, filterSeries));
            if (GUILayout.Button("Merge Recipes 채우기"))
                Run(() => MergeManagerFolderImport.ApplyRecipes((MergegameManager)target, AssetDatabase.GetAssetPath(recipesFolder), recursive, filterSeries));
        }
        if (!string.IsNullOrEmpty(status)) EditorGUILayout.LabelField(status, EditorStyles.wordWrappedLabel);
    }
    private void Run(Action action)
    {
        try { action(); status = "데이터를 적용했습니다. 기존 투입 재고 / UI는 유지됩니다."; }
        catch (Exception exception) { status = exception.Message; }
    }
}
