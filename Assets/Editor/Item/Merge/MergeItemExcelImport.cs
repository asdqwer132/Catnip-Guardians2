using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ItemDataExcelTools;
using UnityEditor;
using UnityEngine;

public sealed class MergeItemExcelPlan
{
    public sealed class Entry
    {
        public int row;
        public ItemData item;
        public GameObject prefab;
        public float radius;
        public string path;
        public MergeItem existing;
    }
    public readonly List<Entry> entries = new List<Entry>();
    public readonly List<string> errors = new List<string>();
    public string outputFolder;
}

public static class MergeItemExcelImport
{
    public static MergeItemExcelPlan Read(string file, string sheetName, string itemFolder, string prefabFolder, string outputFolder)
    {
        var plan = new MergeItemExcelPlan();
        try
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("플레이를 종료한 뒤 가져오세요.");
            if (!string.Equals(Path.GetExtension(file), ".xlsx", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(".xlsx 파일을 선택하세요.");
            plan.outputFolder = AssetPath(outputFolder, false);
            if (!AssetDatabase.IsValidFolder(itemFolder) || !AssetDatabase.IsValidFolder(prefabFolder))
                throw new InvalidDataException("ItemData / 프리팹 검색 폴더를 확인하세요.");
            var sheets = ItemDataXlsxReader.Read(file);
            if (!sheets.TryGetValue(sheetName, out var sheet)) throw new InvalidDataException("시트가 없습니다: " + sheetName);
            var header = sheet.Rows.Find(row => row.Number == 1);
            if (header == null) throw new InvalidDataException("첫 행에 컬럼 이름이 필요합니다.");
            var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in header.Cells)
            {
                string key = pair.Value.Text.Trim();
                if (key.Length == 0) continue;
                if (pair.Value.HasFormula || pair.Value.IsError || columns.ContainsKey(key))
                    throw new InvalidDataException("중복되거나 잘못된 컬럼: " + key);
                columns.Add(key, pair.Key);
            }
            foreach (string key in new[] { "itemData", "prefab", "radius" })
                if (!columns.ContainsKey(key)) throw new InvalidDataException("필수 컬럼이 없습니다: " + key);
            var existing = new Dictionary<ItemData, MergeItem>();
            if (AssetDatabase.IsValidFolder(plan.outputFolder))
                foreach (string guid in AssetDatabase.FindAssets("t:MergeItem", new[] { plan.outputFolder }))
                {
                    var definition = AssetDatabase.LoadAssetAtPath<MergeItem>(AssetDatabase.GUIDToAssetPath(guid));
                    if (definition == null || definition.item == null) continue;
                    if (existing.ContainsKey(definition.item)) throw new InvalidDataException("출력 폴더에 같은 ItemData의 MergeItem이 여러 개 있습니다: " + definition.item.name);
                    existing.Add(definition.item, definition);
                }
            var usedItems = new HashSet<ItemData>();
            var usedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in sheet.Rows)
            {
                if (row.Number <= 1 || row.IsEmpty || row.At(0).Text.TrimStart().StartsWith("#", StringComparison.Ordinal)) continue;
                try
                {
                    string enabled = Cell(row, columns, "enabled");
                    if (enabled.Equals("false", StringComparison.OrdinalIgnoreCase) || enabled == "0") continue;
                    if (enabled.Length > 0 && !enabled.Equals("true", StringComparison.OrdinalIgnoreCase) && enabled != "1")
                        throw new InvalidDataException("enabled는 TRUE / FALSE 또는 1 / 0입니다.");
                    var entry = new MergeItemExcelPlan.Entry { row = row.Number };
                    entry.item = Resolve<ItemData>(Cell(row, columns, "itemData"), itemFolder, "t:ItemData");
                    entry.prefab = Resolve<GameObject>(Cell(row, columns, "prefab"), prefabFolder, "t:Prefab");
                    if (!PrefabUtility.IsPartOfPrefabAsset(entry.prefab) || entry.prefab.GetComponent<MergeItemObject>() == null || !entry.prefab.GetComponent<MergeItemObject>().enabled)
                        throw new InvalidDataException("프리팹 루트에 활성 MergeItemObject가 필요합니다.");
                    if (!float.TryParse(Cell(row, columns, "radius"), NumberStyles.Float, CultureInfo.InvariantCulture, out entry.radius) ||
                        float.IsNaN(entry.radius) || float.IsInfinity(entry.radius) || entry.radius < .01f || entry.radius > 1000f)
                        throw new InvalidDataException("radius는 0.01 ~ 1000 범위의 숫자입니다.");
                    existing.TryGetValue(entry.item, out entry.existing);
                    string path = Cell(row, columns, "assetPath");
                    string name = Cell(row, columns, "assetName");
                    if (path.Length > 0)
                    {
                        entry.path = AssetPath(path, true);
                        if (!entry.path.StartsWith(plan.outputFolder + "/", StringComparison.Ordinal))
                            throw new InvalidDataException("assetPath는 출력 폴더 안에 있어야 합니다.");
                        if (entry.existing != null && AssetDatabase.GetAssetPath(entry.existing) != entry.path)
                            throw new InvalidDataException("이미 등록된 MergeItem 경로와 다릅니다: " + AssetDatabase.GetAssetPath(entry.existing));
                    }
                    else entry.path = entry.existing != null ? AssetDatabase.GetAssetPath(entry.existing) :
                        plan.outputFolder + "/" + SafeName(name.Length == 0 ? entry.item.name + "_Merge" : name) + ".asset";
                    var occupied = AssetDatabase.LoadMainAssetAtPath(entry.path);
                    if (occupied != null && occupied != entry.existing)
                        throw new InvalidDataException("다른 에셋이 같은 경로에 있습니다: " + entry.path);
                    if (!usedItems.Add(entry.item)) throw new InvalidDataException("같은 ItemData 행이 중복됩니다: " + entry.item.name);
                    if (!usedPaths.Add(entry.path)) throw new InvalidDataException("출력 경로가 중복됩니다: " + entry.path);
                    plan.entries.Add(entry);
                }
                catch (Exception exception) { plan.errors.Add(row.Number + "행: " + exception.Message); }
            }
            if (plan.entries.Count == 0 && plan.errors.Count == 0) plan.errors.Add("가져올 행이 없습니다.");
        }
        catch (Exception exception) { plan.errors.Add(exception.Message); }
        return plan;
    }

    private static string Cell(ItemDataXlsxReader.Row row, Dictionary<string, int> columns, string key)
    {
        if (!columns.TryGetValue(key, out int index)) return "";
        var cell = row.At(index);
        if (cell.HasFormula || cell.IsError) throw new InvalidDataException(key + ": 수식 / 오류 셀을 값으로 바꿔 주세요.");
        return cell.Text.Trim();
    }

    private static T Resolve<T>(string locator, string folder, string filter) where T : UnityEngine.Object
    {
        if (string.IsNullOrWhiteSpace(locator)) throw new InvalidDataException(typeof(T).Name + " 값이 비어 있습니다.");
        string path = locator.Replace('\\', '/');
        if (path.StartsWith("guid:", StringComparison.OrdinalIgnoreCase)) path = AssetDatabase.GUIDToAssetPath(path.Substring(5));
        if (path.StartsWith("Assets/", StringComparison.Ordinal) || path.StartsWith("Packages/", StringComparison.Ordinal))
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            throw new InvalidDataException(typeof(T).Name + " 에셋을 찾을 수 없습니다: " + locator);
        }
        T match = null;
        foreach (string guid in AssetDatabase.FindAssets(filter, new[] { folder }))
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset == null) continue;
            bool found = string.Equals(asset.name, locator, StringComparison.OrdinalIgnoreCase) ||
                (asset is ItemData item && string.Equals(item.dataId, locator, StringComparison.Ordinal));
            if (!found) continue;
            if (match != null && match != asset) throw new InvalidDataException("이름 / ID가 중복됩니다. 에셋 경로나 guid:GUID를 입력하세요: " + locator);
            match = asset;
        }
        if (match == null) throw new InvalidDataException(typeof(T).Name + " 에셋을 찾을 수 없습니다: " + locator);
        return match;
    }

    private static string AssetPath(string value, bool file)
    {
        string path = value.Replace('\\', '/').TrimEnd('/');
        if (!file && path == "Assets") return path;
        if (!path.StartsWith("Assets/", StringComparison.Ordinal)) throw new InvalidDataException("Assets 내부 경로를 입력하세요.");
        foreach (string part in path.Split('/'))
            if (part == ".." || part == "." || part.Length == 0) throw new InvalidDataException("잘못된 에셋 경로: " + value);
        if (file && !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("MergeItem 출력 파일은 .asset이어야 합니다.");
        return path;
    }

    private static string SafeName(string name)
    {
        foreach (char value in "/\\:*?\"<>|") name = name.Replace(value, '_');
        return name.Trim();
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }

    public static void Apply(MergeItemExcelPlan plan)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("플레이를 종료한 뒤 가져오세요.");
        if (plan == null || plan.errors.Count > 0 || plan.entries.Count == 0) throw new InvalidOperationException("검증에 통과한 엑셀 데이터가 필요합니다.");
        var originals = new Dictionary<MergeItem, string>();
        var created = new List<string>();
        var changed = new List<MergeItem>();
        try
        {
            foreach (var entry in plan.entries)
            {
                var definition = entry.existing;
                if (definition == null)
                {
                    EnsureFolder(Path.GetDirectoryName(entry.path).Replace('\\', '/'));
                    if (AssetDatabase.LoadMainAssetAtPath(entry.path) != null) throw new InvalidOperationException("출력 경로가 변경되었습니다. 다시 검증하세요: " + entry.path);
                    definition = ScriptableObject.CreateInstance<MergeItem>();
                    definition.name = Path.GetFileNameWithoutExtension(entry.path);
                    AssetDatabase.CreateAsset(definition, entry.path);
                    created.Add(entry.path);
                }
                else
                {
                    originals.Add(definition, EditorJsonUtility.ToJson(definition));
                    Undo.RecordObject(definition, "Import merge item Excel");
                }
                definition.item = entry.item;
                definition.prefab = entry.prefab;
                definition.radius = entry.radius;
                EditorUtility.SetDirty(definition);
                changed.Add(definition);
            }
            foreach (var definition in changed) AssetDatabase.SaveAssetIfDirty(definition);
        }
        catch
        {
            foreach (var pair in originals)
            { EditorJsonUtility.FromJsonOverwrite(pair.Value, pair.Key); EditorUtility.SetDirty(pair.Key); AssetDatabase.SaveAssetIfDirty(pair.Key); }
            foreach (string path in created) AssetDatabase.DeleteAsset(path);
            throw;
        }
    }
}
