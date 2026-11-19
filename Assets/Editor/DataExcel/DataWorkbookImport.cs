using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using ItemDataExcelTools;
using UnityEditor;
using UnityEngine;

public enum DataWorkbookKind { Identify, Displayer, Extra }

// All three workbooks share Code routing, recursive ID lookup and one validated apply path.
public static class DataWorkbookImport
{
    public sealed class Binding
    {
        public string Sheet, TypeName, RootPath;
    }

    public sealed class Entry
    {
        public string Id, Path, BeforeJson;
        public Type DataClass;
        public DefaultData Existing;
        public readonly Dictionary<FieldInfo, object> Values = new Dictionary<FieldInfo, object>();
        public readonly Dictionary<language, Dictionary<string, string>> Descriptions = new Dictionary<language, Dictionary<string, string>>();
    }

    public sealed class Plan
    {
        public DataWorkbookKind Kind;
        public readonly List<Entry> Entries = new List<Entry>();
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Warnings = new List<string>();
        public int Missing, Rows;
        public int Created => Entries.Count(entry => entry.Existing == null);
        public string Summary => $"대상 {Entries.Count}개 · 생성 {Created} · 갱신 {Entries.Count - Created} · 대상 없음 {Missing} · 오류 {Errors.Count}";
    }

    private static readonly FieldInfo LanguageCache = typeof(DefaultData).GetField("languageDataMap", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly string[] IdentifyFields = { "dataId", "dataNumbering", "dataType", "icon", "grade", "category", "rarity" };
    private static readonly string[] DisplayFields = { "dataId", "language", "dataName", "description", "detailDescription" };
    private static readonly Dictionary<string, string> ExtraAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "mapPath", "skillMap" }, { "requiredNodeIds", "requiredSkills" }, { "rewardPaths", "rewards" }
    };

    public static List<Binding> ReadBindings(string file) => Bindings(ItemDataXlsxReader.Read(file));

    private static List<Binding> Bindings(Dictionary<string, ItemDataXlsxReader.Sheet> sheets)
    {
        if (!sheets.TryGetValue("Code", out var code)) throw new InvalidDataException("Code 시트가 없습니다.");
        var table = new Table(code, "Sheet", "DataType", "RootPath");
        var result = new List<Binding>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in table.Rows)
        {
            string name = table.Get(row, "Sheet");
            if (name.StartsWith("#", StringComparison.Ordinal)) continue;
            if (name.Length == 0 || !names.Add(name)) throw new InvalidDataException("Code " + row.Number + "행: 비어 있거나 중복된 Sheet입니다: " + name);
            result.Add(new Binding { Sheet = name, TypeName = table.Get(row, "DataType"), RootPath = table.Get(row, "RootPath") });
        }
        return result;
    }

    public static Plan Read(string file, DataWorkbookKind kind, IEnumerable<string> selectedSheets)
    {
        var plan = new Plan { Kind = kind };
        try
        {
            var sheets = ItemDataXlsxReader.Read(file);
            var bindings = Bindings(sheets);
            var selected = new HashSet<string>(selectedSheets ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            if (selected.Count == 0) throw new InvalidDataException("적용할 시트를 선택하세요.");
            foreach (string name in selected)
                if (!bindings.Any(binding => string.Equals(binding.Sheet, name, StringComparison.OrdinalIgnoreCase)))
                    plan.Errors.Add("Code에 등록되지 않은 시트: " + name);
            foreach (var binding in bindings.Where(binding => selected.Contains(binding.Sheet)))
            {
                try { ReadSheet(plan, binding, sheets); }
                catch (Exception error) { plan.Errors.Add(binding.Sheet + ": " + error.Message); }
            }
            ValidateSkillLinks(plan);
        }
        catch (Exception error) { plan.Errors.Add(error.Message); }
        return plan;
    }

    private static void ReadSheet(Plan plan, Binding binding, Dictionary<string, ItemDataXlsxReader.Sheet> sheets)
    {
        Type type = ResolveType(binding.TypeName);
        string root = AssetFolder(binding.RootPath);
        if (!sheets.TryGetValue(binding.Sheet, out var sheet)) throw new InvalidDataException("Code에서 지정한 시트가 없습니다.");
        var table = new Table(sheet, plan.Kind == DataWorkbookKind.Displayer ? new[] { "dataId", "language" } : new[] { "dataId" });
        var columns = new Dictionary<string, FieldInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (string header in table.Headers)
        {
            if (header.Equals("enabled", StringComparison.OrdinalIgnoreCase)) continue;
            if (plan.Kind == DataWorkbookKind.Displayer)
            {
                if (!DisplayFields.Contains(header, StringComparer.OrdinalIgnoreCase)) throw new InvalidDataException("알 수 없는 표시 컬럼: " + header);
            }
            else
            {
                if (plan.Kind == DataWorkbookKind.Identify && !IdentifyFields.Contains(header, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidDataException("아이덴티파이에서 지원하지 않는 컬럼: " + header);
                string fieldName = plan.Kind == DataWorkbookKind.Extra && ExtraAliases.TryGetValue(header, out string alias) ? alias : header;
                var field = DataWorkbookValue.Field(type, fieldName);
                if (field == null || field.Name == "data") throw new InvalidDataException(type.Name + "에 주입 가능한 필드가 없습니다: " + header);
                if (columns.Values.Contains(field)) throw new InvalidDataException("같은 필드를 두 번 지정했습니다: " + header);
                columns.Add(header, field);
            }
        }

        var assets = Index(root);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in table.Rows)
        {
            try
            {
                string id = table.Get(row, "dataId");
                if (id.StartsWith("#", StringComparison.Ordinal)) continue;
                string enabled = table.Get(row, "enabled");
                if (enabled.Length > 0 && !(bool)DataWorkbookValue.Parse(typeof(bool), enabled)) continue;
                if (DataWorkbookValue.IsKeep(id) || DataWorkbookValue.IsClear(id)) throw new InvalidDataException("조회할 dataId를 명시하세요.");
                string rowKey = plan.Kind == DataWorkbookKind.Displayer ? id + "\n" + table.Get(row, "language").ToLowerInvariant() : id;
                if (!seen.Add(rowKey)) throw new InvalidDataException("중복 ID 또는 ID/언어입니다: " + id);
                plan.Rows++;
                assets.TryGetValue(id, out var matches);
                var typed = matches?.Where(asset => asset.GetType() == type).ToArray() ?? Array.Empty<DefaultData>();
                if (typed.Length > 1) throw new InvalidDataException("같은 ID의 에셋이 여러 개입니다: " + string.Join(", ", typed.Select(AssetDatabase.GetAssetPath)));
                if (typed.Length == 0 && matches != null)
                    throw new InvalidDataException("ID " + id + "는 " + string.Join(", ", matches.Select(asset => asset.GetType().Name).Distinct()) + "입니다. Code의 DataType=" + type.Name + "과 다릅니다.");
                var existing = typed.SingleOrDefault();
                if (existing == null && plan.Kind != DataWorkbookKind.Identify)
                {
                    plan.Missing++;
                    plan.Warnings.Add(binding.Sheet + " " + row.Number + "행: 대상 없음, 갱신 생략: " + id);
                    continue;
                }
                string path = existing != null ? AssetDatabase.GetAssetPath(existing) : root + "/Generated/" + FileName(id) + ".asset";
                var previous = plan.Entries.Find(entry => entry.Path == path);
                if (previous != null && (plan.Kind != DataWorkbookKind.Displayer || previous.Id != id))
                    throw new InvalidDataException("다른 행과 주입 대상이 겹칩니다: " + path);
                if (existing == null && (AssetDatabase.LoadMainAssetAtPath(path) != null || File.Exists(ProjectFile(path))))
                    throw new InvalidDataException("Generated 경로가 이미 사용 중입니다: " + path);
                var entry = previous ?? new Entry { Id = id, Path = path, Existing = existing, DataClass = type, BeforeJson = existing != null ? EditorJsonUtility.ToJson(existing) : null };
                if (plan.Kind == DataWorkbookKind.Displayer) ReadDescription(entry, table, row);
                else
                {
                    foreach (var column in columns)
                    {
                        if (column.Value.Name == "dataId" && plan.Kind == DataWorkbookKind.Extra) continue;
                        string text = table.Get(row, column.Key, column.Value.FieldType != typeof(string));
                        if (DataWorkbookValue.IsKeep(text)) continue; // Blank and - preserve authored values and constructor defaults.
                        object value = column.Value.Name == "dataId" ? id : DataWorkbookValue.Parse(column.Value.FieldType, text);
                        DataWorkbookValue.Validate(column.Value, value);
                        entry.Values.Add(column.Value, value);
                    }
                }
                if (type == typeof(ItemBoxData)) ValidateBoxRewards(entry);
                if (previous == null && (entry.Values.Count > 0 || entry.Descriptions.Count > 0)) plan.Entries.Add(entry);
            }
            catch (Exception error) { plan.Errors.Add(binding.Sheet + " " + row.Number + "행: " + error.Message); }
        }
    }

    private static void ValidateBoxRewards(Entry entry)
    {
        foreach (var value in entry.Values)
        {
            if (value.Key.Name != "gachaItems" || value.Value == null) continue;
            foreach (var reward in (GachaItemInfo[])value.Value)
            {
                if (reward == null || reward.itemData == null) throw new InvalidDataException("상자 보상에는 itemData 참조가 필요합니다.");
                if (reward.amounts == null || reward.amounts.Length == 0) continue; // 빈 수량 목록은 1개 지급.
                if (!reward.amounts.Any(a => a != null && a.count > 0 && a.weight > 0f))
                    throw new InvalidDataException("상자 보상의 amounts에는 count 1 이상, weight 0 초과인 항목이 필요합니다.");
            }
        }
    }

    private static void ReadDescription(Entry entry, Table table, ItemDataXlsxReader.Row row)
    {
        string languageText = table.Get(row, "language");
        if (DataWorkbookValue.IsKeep(languageText) || DataWorkbookValue.IsClear(languageText)) throw new InvalidDataException("language가 비어 있습니다.");
        var selected = (language)DataWorkbookValue.Parse(typeof(language), languageText);
        var patch = new Dictionary<string, string>();
        foreach (string name in DisplayFields.Skip(2))
        {
            string text = table.Get(row, name, false);
            if (!DataWorkbookValue.IsKeep(text)) patch.Add(name, DataWorkbookValue.IsClear(text) ? "" : text);
        }
        if (patch.Count == 0) return;
        if (entry.Descriptions.ContainsKey(selected)) throw new InvalidDataException("중복된 언어입니다: " + entry.Id + "/" + selected);
        entry.Descriptions.Add(selected, patch);
    }

    private static Dictionary<string, List<DefaultData>> Index(string root)
    {
        var result = new Dictionary<string, List<DefaultData>>(StringComparer.Ordinal);
        if (!AssetDatabase.IsValidFolder(root)) return result;
        foreach (string guid in AssetDatabase.FindAssets("t:DefaultData", new[] { root }))
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)).OfType<DefaultData>())
            {
                if (string.IsNullOrEmpty(asset.dataId)) continue;
                if (!result.TryGetValue(asset.dataId, out var values)) result.Add(asset.dataId, values = new List<DefaultData>());
                if (!values.Contains(asset)) values.Add(asset);
            }
        return result;
    }

    public static Type ResolveType(string name)
    {
        string key = Normalize(name);
        var types = TypeCache.GetTypesDerivedFrom<DefaultData>().Concat(new[] { typeof(DefaultData) })
            .Where(type => !type.IsAbstract && (Normalize(type.Name) == key || Normalize(type.FullName) == key)).ToArray();
        if (types.Length != 1) throw new InvalidDataException("DefaultData 클래스명을 확인하세요: " + name);
        return types[0];
    }
    private static string Normalize(string value) => new string((value ?? "").Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();
    private static string FileName(string id)
    {
        string name = new string(id.Select(c => char.IsControl(c) || "/\\:*?\"<>|".Contains(c) ? '_' : c).ToArray()).Trim(' ', '.');
        if (name.Length == 0) throw new InvalidDataException("파일명으로 사용할 수 없는 ID입니다: " + id);
        return name;
    }
    public static string AssetFolder(string value)
    {
        string path = (value ?? "").Trim().Replace('\\', '/').TrimEnd('/');
        if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Split('/').Any(part => part.Length == 0 || part == "." || part == ".." || part.IndexOfAny(new[] { ':', '*', '?', '"', '<', '>', '|' }) >= 0))
            throw new InvalidDataException("RootPath는 Assets 아래의 폴더여야 합니다: " + value);
        return path;
    }
    private static string ProjectFile(string path) => System.IO.Path.Combine(Directory.GetParent(Application.dataPath).FullName, path);

    // Extra skill links still use the provided IDs and can be checked without touching graph assets.
    private static void ValidateSkillLinks(Plan plan)
    {
        var changed = plan.Entries.Where(entry => typeof(SkillNodeData).IsAssignableFrom(entry.DataClass)).ToArray();
        var visiting = new HashSet<SkillNodeData>();
        var visited = new HashSet<SkillNodeData>();
        SkillNodeData[] Parents(SkillNodeData node)
        {
            var entry = changed.FirstOrDefault(candidate => candidate.Existing == node);
            var field = DataWorkbookValue.Field(typeof(SkillNodeData), "requiredSkills");
            return entry != null && entry.Values.TryGetValue(field, out var value) ? ((List<SkillNodeData>)value).ToArray() : node.requiredSkills?.ToArray() ?? Array.Empty<SkillNodeData>();
        }
        void Visit(SkillNodeData node)
        {
            if (node == null || visited.Contains(node)) return;
            if (!visiting.Add(node)) throw new InvalidDataException("스킬 선행 조건에 순환이 있습니다: " + node.dataId);
            foreach (var parent in Parents(node)) Visit(parent);
            visiting.Remove(node); visited.Add(node);
        }
        try { foreach (var entry in changed) if (entry.Existing is SkillNodeData node) Visit(node); }
        catch (Exception error) { plan.Errors.Add(error.Message); }
    }

    public static void Apply(Plan plan)
    {
        if (plan.Errors.Count > 0) throw new InvalidOperationException("검증 오류가 있는 엑셀은 적용할 수 없습니다.");
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("플레이 모드를 종료하세요.");
        foreach (var entry in plan.Entries)
        {
            if (entry.Existing == null)
            {
                if (plan.Kind != DataWorkbookKind.Identify) throw new InvalidOperationException("아이덴티파이만 새 에셋을 생성할 수 있습니다.");
                if (AssetDatabase.LoadMainAssetAtPath(entry.Path) != null || File.Exists(ProjectFile(entry.Path))) throw new IOException("새 에셋 경로가 변경되었습니다. 다시 검증하세요: " + entry.Path);
            }
            else if (EditorJsonUtility.ToJson(entry.Existing) != entry.BeforeJson) throw new InvalidOperationException("검증 후 데이터가 변경되었습니다. 다시 검증하세요: " + entry.Id);
        }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Import " + plan.Kind + " workbook");
        var backups = new Dictionary<DefaultData, string>();
        var created = new List<string>();
        var folders = new List<string>();
        try
        {
            foreach (var entry in plan.Entries)
            {
                var asset = entry.Existing;
                if (asset == null)
                {
                    EnsureFolder(System.IO.Path.GetDirectoryName(entry.Path).Replace('\\', '/'), folders);
                    asset = (DefaultData)ScriptableObject.CreateInstance(entry.DataClass);
                    asset.name = System.IO.Path.GetFileNameWithoutExtension(entry.Path);
                }
                else { backups.Add(asset, entry.BeforeJson); Undo.RecordObject(asset, "Import Excel data"); }
                foreach (var pair in entry.Values) pair.Key.SetValue(asset, pair.Value);
                if (entry.Descriptions.Count > 0) asset.data = MergeDescriptions(asset.data, entry.Descriptions);
                LanguageCache.SetValue(asset, null);
                if (entry.Existing == null)
                {
                    created.Add(entry.Path);
                    AssetDatabase.CreateAsset(asset, entry.Path);
                    if (AssetDatabase.LoadMainAssetAtPath(entry.Path) != asset) throw new IOException("에셋 생성에 실패했습니다: " + entry.Path);
                    Undo.RegisterCreatedObjectUndo(asset, "Create Excel data");
                }
                EditorUtility.SetDirty(asset);
            }
            Undo.FlushUndoRecordObjects();
            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(group);
            Debug.Log("[DataExcel][" + plan.Kind + "] " + plan.Summary);
        }
        catch
        {
            Undo.RevertAllDownToGroup(group);
            foreach (var backup in backups)
                if (backup.Key != null) { EditorJsonUtility.FromJsonOverwrite(backup.Value, backup.Key); LanguageCache.SetValue(backup.Key, null); EditorUtility.SetDirty(backup.Key); }
            foreach (string path in created) AssetDatabase.DeleteAsset(path);
            foreach (string folder in folders.AsEnumerable().Reverse())
                if (AssetDatabase.IsValidFolder(folder) && Directory.GetFileSystemEntries(ProjectFile(folder)).Length == 0) AssetDatabase.DeleteAsset(folder);
            AssetDatabase.SaveAssets();
            throw;
        }
    }

    private static Description[] MergeDescriptions(Description[] current, Dictionary<language, Dictionary<string, string>> patches)
    {
        var result = new List<Description>();
        if (current != null) foreach (var value in current)
            if (value != null) result.Add(new Description { language = value.language, dataName = value.dataName, description = value.description, detailDescription = value.detailDescription });
        foreach (var pair in patches)
        {
            var value = result.Find(description => description.language == pair.Key);
            if (value == null) { value = new Description { language = pair.Key }; result.Add(value); }
            foreach (var patch in pair.Value) typeof(Description).GetField(patch.Key).SetValue(value, patch.Value);
        }
        return result.ToArray();
    }

    private static void EnsureFolder(string path, List<string> created)
    {
        string folder = "Assets";
        foreach (string part in path.Split('/').Skip(1))
        {
            string next = folder + "/" + part;
            if (!AssetDatabase.IsValidFolder(next))
            {
                if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(folder, part))) throw new IOException("폴더 생성 실패: " + next);
                created.Add(next);
            }
            folder = next;
        }
    }

    private sealed class Table
    {
        private readonly Dictionary<string, int> columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public readonly List<ItemDataXlsxReader.Row> Rows;
        public IEnumerable<string> Headers => columns.Keys;
        public Table(ItemDataXlsxReader.Sheet sheet, params string[] required)
        {
            var header = sheet.Rows.Find(row => row.Number == 1) ?? throw new InvalidDataException("첫 행에 컬럼명이 필요합니다.");
            foreach (var pair in header.Cells)
            {
                string name = ReadCell(pair.Value).Trim();
                if (name.Length == 0) continue;
                if (columns.ContainsKey(name)) throw new InvalidDataException("중복 컬럼: " + name);
                columns.Add(name, pair.Key);
            }
            foreach (string name in required) if (!columns.ContainsKey(name)) throw new InvalidDataException("필수 컬럼 없음: " + name);
            Rows = sheet.Rows.Where(row => row.Number > 1 && !row.IsEmpty).ToList();
        }
        public string Get(ItemDataXlsxReader.Row row, string name, bool trim = true)
        {
            if (!columns.TryGetValue(name, out int column)) return "";
            string text = ReadCell(row.At(column));
            return trim ? text.Trim() : text;
        }
        private static string ReadCell(ItemDataXlsxReader.Cell cell)
        {
            if (cell.IsError || cell.HasFormula && string.IsNullOrWhiteSpace(cell.Text)) throw new InvalidDataException("엑셀 오류 또는 저장된 결과가 없는 수식입니다. Excel에서 재계산 후 저장하세요.");
            return cell.Text;
        }
    }
}
