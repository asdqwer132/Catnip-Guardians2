using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ItemDataExcelTools;
using UnityEditor;
using UnityEngine;

public sealed class SkillExcelManager : IDataExcelModule
{
    private string file = "Assets/Data/Excel/SkillData.xlsx", message = "";
    private Vector2 scroll;
    public string Name => "Skills";
    public void Dispose() { }
    public void DrawGUI()
    {
        file = EditorGUILayout.TextField("Workbook", file);
        if (GUILayout.Button("Choose .xlsx"))
        { string path = EditorUtility.OpenFilePanel("Skills", "", "xlsx"); if (!string.IsNullOrEmpty(path)) file = path; }
        if (GUILayout.Button("Validate / Preview")) Run(false);
        if (GUILayout.Button("Apply Skills")) Run(true);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField(message, EditorStyles.wordWrappedLabel); EditorGUILayout.EndScrollView();
    }
    private void Run(bool apply)
    {
        try
        {
            var plan = SkillExcelImport.Read(file);
            message = string.Join("\n", plan.Select(row => row.path + " | " + row.id + " | Level " + row.level));
            if (apply) SkillExcelImport.Apply(plan);
            message = (apply ? "Applied: " : "Validated: ") + plan.Count + "\n" + message;
        }
        catch (Exception error) { message = error.Message; }
    }
}

public static class SkillExcelImport
{
    public sealed class Entry
    {
        public string path, id;
        public SkillMapData map;
        public SkillNodeData existing;
        public int level;
        public bool setIcon, setUnlockedIcon;
        public Sprite icon, unlockedIcon;
        public Cost[] costs;
        public string[] required;
        public SkillRewardData[] rewards;
        public List<Description> descriptions = new List<Description>();
    }
    private static List<Dictionary<string, string>> Rows(ItemDataXlsxReader.Sheet sheet)
    {
        var result = new List<Dictionary<string, string>>();
        if (sheet.Rows.Count == 0) return result;
        var headers = sheet.Rows[0].Cells.ToDictionary(pair => pair.Key, pair => pair.Value.Text.Trim());
        foreach (var row in sheet.Rows.Skip(1))
        {
            if (row.IsEmpty) continue;
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var column in headers)
            {
                var cell = row.At(column.Key);
                if (cell.HasFormula || cell.IsError) throw new InvalidDataException(sheet.Name + " row " + row.Number + ": use literal values, not formulas/errors.");
                if (!string.IsNullOrEmpty(column.Value)) data.Add(column.Value, cell.Text.Trim());
            }
            result.Add(data);
        }
        return result;
    }
    private static string Get(Dictionary<string, string> row, string name) => row.TryGetValue(name, out string value) ? value : "";
    private static string[] Split(string value) => value == "-" ? Array.Empty<string>() : value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
    private static T EnumValue<T>(string value) where T : struct
    {
        if (!Enum.TryParse<T>(value, true, out var parsed) || !Enum.IsDefined(typeof(T), parsed)) throw new InvalidDataException("Invalid " + typeof(T).Name + ": " + value);
        return parsed;
    }
    private static Sprite SpriteAt(string value)
    {
        if (value == "-") return null;
        var parts = value.Split('#');
        var sprites = AssetDatabase.LoadAllAssetsAtPath(parts[0]).OfType<Sprite>().Where(s => parts.Length == 1 || s.name == parts[1]).ToArray();
        if (sprites.Length != 1) throw new InvalidDataException("Sprite must resolve uniquely (path#spriteName): " + value);
        return sprites[0];
    }
    public static List<Entry> Read(string workbook)
    {
        var sheets = ItemDataXlsxReader.Read(workbook);
        if (!sheets.TryGetValue("Skills", out var sheet)) throw new InvalidDataException("Missing Skills sheet.");
        var plan = new List<Entry>(); var paths = new HashSet<string>(); var keys = new HashSet<string>();
        foreach (var row in Rows(sheet))
        {
            if (Get(row, "enabled").Length > 0 && !new[] { "true", "false", "0", "1" }.Contains(Get(row, "enabled").ToLowerInvariant())) throw new InvalidDataException("Invalid enabled value.");
            if (Get(row, "enabled").Equals("false", StringComparison.OrdinalIgnoreCase) || Get(row, "enabled") == "0") continue;
            var e = new Entry { path = Get(row, "assetPath").Replace('\\', '/'), id = Get(row, "dataId") };
            if (!e.path.StartsWith("Assets/", StringComparison.Ordinal) || !e.path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) || e.path.Count(c => c == '/') < 2 || e.path.Split('/').Any(p => p == ".." || p == ".") || string.IsNullOrEmpty(e.id)) throw new InvalidDataException("Each enabled skill needs an Assets/...asset path and dataId.");
            e.map = AssetDatabase.LoadAssetAtPath<SkillMapData>(Get(row, "mapPath"));
            if (e.map == null || string.IsNullOrEmpty(e.map.dataId)) throw new InvalidDataException("Missing map or map ID: " + e.path);
            if (!int.TryParse(Get(row, "level"), NumberStyles.Integer, CultureInfo.InvariantCulture, out e.level) || e.level < 1) throw new InvalidDataException("Level must be >= 1: " + e.path);
            if (!paths.Add(e.path) || !keys.Add(e.map.dataId + "\n" + e.id)) throw new InvalidDataException("Duplicate skill path or map/node ID: " + e.path);
            e.existing = AssetDatabase.LoadAssetAtPath<SkillNodeData>(e.path);
            if (AssetDatabase.LoadMainAssetAtPath(e.path) != null && e.existing == null) throw new InvalidDataException("Target path is not a SkillNodeData: " + e.path);
            string ownFolder = Path.GetDirectoryName(e.path).Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(ownFolder))
            {
                bool duplicate = AssetDatabase.FindAssets("t:SkillNodeData", new[] { ownFolder })
                    .Select(guid => AssetDatabase.LoadAssetAtPath<SkillNodeData>(AssetDatabase.GUIDToAssetPath(guid)))
                    .Any(node => node != e.existing && node.skillMap == e.map && node.dataId == e.id);
                if (duplicate) throw new InvalidDataException("An existing node already uses this map/node ID: " + e.id);
            }
            string icon = Get(row, "icon"), unlocked = Get(row, "unlockedIcon");
            e.setIcon = icon.Length > 0; e.setUnlockedIcon = unlocked.Length > 0;
            if (e.setIcon) e.icon = SpriteAt(icon);
            if (e.setUnlockedIcon) e.unlockedIcon = SpriteAt(unlocked);
            string costs = Get(row, "costs");
            if (costs.Length > 0) e.costs = Split(costs).Select(token =>
            {
                var pair = token.Split(':');
                if (pair.Length != 2 || !int.TryParse(pair[1], out int amount) || amount < 0) throw new InvalidDataException("Invalid cost: " + token);
                return new Cost(EnumValue<CurrencyType>(pair[0]), amount);
            }).ToArray();
            string required = Get(row, "requiredNodeIds"), rewards = Get(row, "rewardPaths");
            if (required.Length > 0) e.required = Split(required);
            if (rewards.Length > 0) e.rewards = Split(rewards).Select(path => AssetDatabase.LoadAssetAtPath<SkillRewardData>(path) ?? throw new InvalidDataException("Missing reward: " + path)).ToArray();
            plan.Add(e);
        }
        foreach (var e in plan) if (e.required != null) foreach (string id in e.required)
            if (!plan.Any(p => p.map == e.map && p.id == id) && ResolveExisting(e, id) == null) throw new InvalidDataException("Missing prerequisite: " + e.id + " -> " + id);
        var visiting = new HashSet<Entry>(); var visited = new HashSet<Entry>();
        void CheckCycle(Entry e)
        {
            if (visited.Contains(e)) return;
            if (!visiting.Add(e)) throw new InvalidDataException("Cyclic prerequisites: " + e.id);
            var ids = e.required ?? (e.existing != null && e.existing.requiredSkills != null
                ? e.existing.requiredSkills.Where(node => node != null).Select(node => node.dataId).ToArray() : Array.Empty<string>());
            foreach (string id in ids)
            {
                if (id == e.id) throw new InvalidDataException("A skill cannot require itself: " + e.id);
                var parent = plan.Find(p => p.map == e.map && p.id == id);
                if (parent != null) CheckCycle(parent);
            }
            visiting.Remove(e); visited.Add(e);
        }
        foreach (var entry in plan) CheckCycle(entry);
        if (sheets.TryGetValue("Descriptions", out var descriptions)) foreach (var row in Rows(descriptions))
        {
            var e = plan.Find(p => p.id == Get(row, "dataId") && p.map.dataId == Get(row, "mapId"));
            if (e == null) continue;
            var lang = EnumValue<language>(Get(row, "language"));
            if (e.descriptions.Any(d => d.language == lang)) throw new InvalidDataException("Duplicate description language: " + e.id);
            e.descriptions.Add(new Description { language = lang, dataName = Get(row, "dataName"), description = Get(row, "description"), detailDescription = Get(row, "detailDescription") });
        }
        return plan;
    }
    private static SkillNodeData ResolveExisting(Entry entry, string id)
    {
        var matches = AssetDatabase.FindAssets("t:SkillNodeData", new[] { Path.GetDirectoryName(entry.path).Replace('\\', '/') })
            .Select(guid => AssetDatabase.LoadAssetAtPath<SkillNodeData>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(node => node.skillMap == entry.map && node.dataId == id).ToArray();
        if (matches.Length > 1) throw new InvalidDataException("Ambiguous prerequisite ID: " + id);
        return matches.SingleOrDefault();
    }
    public static void Apply(List<Entry> plan)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Import skills from Excel");
        foreach (var e in plan)
        {
            if (e.existing == null)
            {
                string folder = Path.GetDirectoryName(e.path).Replace('\\', '/');
                string current = "Assets";
                foreach (string part in folder.Substring(7).Split('/'))
                { if (!AssetDatabase.IsValidFolder(current + "/" + part)) AssetDatabase.CreateFolder(current, part); current += "/" + part; }
                e.existing = ScriptableObject.CreateInstance<SkillNodeData>(); AssetDatabase.CreateAsset(e.existing, e.path); Undo.RegisterCreatedObjectUndo(e.existing, "Create skill");
            }
        }
        foreach (var e in plan)
        {
            var node = e.existing; Undo.RecordObject(node, "Import skill"); node.dataId = e.id; node.skillMap = e.map; node.level = e.level;
            if (e.setIcon) node.icon = e.icon; if (e.setUnlockedIcon) node.unlockedIcon = e.unlockedIcon;
            if (e.costs != null) node.costs = e.costs;
            if (e.rewards != null) node.rewards = e.rewards.ToList();
            if (e.required != null) node.requiredSkills = e.required.Select(id => plan.Find(p => p.map == e.map && p.id == id)?.existing ?? ResolveExisting(e, id)).ToList();
            var descriptions = node.data != null ? node.data.ToList() : new List<Description>();
            foreach (var description in e.descriptions) { descriptions.RemoveAll(d => d != null && d.language == description.language); descriptions.Add(description); }
            node.data = descriptions.ToArray();
            typeof(DefaultData).GetField("languageDataMap", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(node, null);
            EditorUtility.SetDirty(node);
        }
        AssetDatabase.SaveAssets(); Undo.CollapseUndoOperations(group);
    }
}
