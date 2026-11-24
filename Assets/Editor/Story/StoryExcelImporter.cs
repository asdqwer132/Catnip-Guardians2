using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ItemDataExcelTools;
using UnityEditor;
using UnityEngine;

// Editor-only import of localized visual novel lines.
public static class StoryExcelImporter
{
    public sealed class Entry
    {
        public string assetPath;
        public StoryData.DialogueData[] dialogues;
    }

    private sealed class Line
    {
        public string story, speaker, text;
        public string background, leftCharacter, rightCharacter;
        public string backgroundTrigger, leftTrigger, rightTrigger;
        public int number;
        public bool HasSameVisuals(Line other) =>
            background == other.background && leftCharacter == other.leftCharacter && rightCharacter == other.rightCharacter &&
            backgroundTrigger == other.backgroundTrigger && leftTrigger == other.leftTrigger && rightTrigger == other.rightTrigger;
    }

    public static List<Entry> Read(string workbook, string outputFolder)
    {
        outputFolder = AssetFolder(outputFolder);
        var sheets = ItemDataXlsxReader.Read(workbook);
        var languages = new List<language>();
        var translated = new List<Dictionary<string, Line>>();
        foreach (language selected in Enum.GetValues(typeof(language)))
        {
            if (!sheets.TryGetValue(selected.ToString(), out var sheet))
                throw new InvalidDataException("Missing language sheet: " + selected);
            languages.Add(selected);
            translated.Add(ReadSheet(sheet));
        }
        var reference = translated[0];
        if (reference.Count == 0) throw new InvalidDataException("The workbook has no dialogue rows.");
        foreach (var rows in translated)
            if (rows.Count != reference.Count || reference.Keys.Any(key => !rows.ContainsKey(key)))
                throw new InvalidDataException("All language sheets must contain the same storyId / line keys.");

        var entries = new List<Entry>();
        var sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        foreach (var story in reference.Values.GroupBy(row => row.story).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var lines = story.OrderBy(row => row.number).ToArray();
            var dialogues = new List<StoryData.DialogueData>();
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.number != i + 1) throw new InvalidDataException(story.Key + ": line numbers must be consecutive from 1.");
                var translations = new List<StoryData.LocalizedDialogue>();
                for (int l = 0; l < languages.Count; l++)
                {
                    var other = translated[l][Key(line)];
                    if (!line.HasSameVisuals(other))
                        throw new InvalidDataException(story.Key + " / " + line.number + ": visual columns must match across languages.");
                    translations.Add(new StoryData.LocalizedDialogue { languageType = languages[l], speaker = other.speaker, dialogue = other.text });
                }
                string context = story.Key + " / " + line.number;
                dialogues.Add(new StoryData.DialogueData
                {
                    background = LoadSprite(line.background, sprites, context),
                    leftCharacter = LoadSprite(line.leftCharacter, sprites, context),
                    rightCharacter = LoadSprite(line.rightCharacter, sprites, context),
                    backgroundTrigger = line.backgroundTrigger,
                    leftTrigger = line.leftTrigger,
                    rightTrigger = line.rightTrigger,
                    translations = translations.ToArray()
                });
            }
            string path = outputFolder + "/" + story.Key + ".asset";
            var existing = AssetDatabase.LoadMainAssetAtPath(path);
            if (existing != null && !(existing is StoryData)) throw new InvalidDataException("Another asset occupies: " + path);
            entries.Add(new Entry { assetPath = path, dialogues = dialogues.ToArray() });
        }
        if (entries.Select(entry => entry.assetPath).Distinct(StringComparer.OrdinalIgnoreCase).Count() != entries.Count)
            throw new InvalidDataException("Story IDs must not differ only by filename letter case.");
        return entries;
    }

    // Read validates every language and sprite before any asset is changed.
    public static int Import(string workbook, string outputFolder)
    {
        var entries = Read(workbook, outputFolder);
        EnsureFolder(AssetFolder(outputFolder));
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Import story Excel");
        foreach (var entry in entries)
        {
            var data = AssetDatabase.LoadAssetAtPath<StoryData>(entry.assetPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<StoryData>();
                AssetDatabase.CreateAsset(data, entry.assetPath);
                Undo.RegisterCreatedObjectUndo(data, "Create story");
            }
            else Undo.RecordObject(data, "Update story");
            data.dialogues = entry.dialogues;
            EditorUtility.SetDirty(data);
        }
        Undo.CollapseUndoOperations(undoGroup);
        AssetDatabase.SaveAssets();
        return entries.Count;
    }

    private static Dictionary<string, Line> ReadSheet(ItemDataXlsxReader.Sheet sheet)
    {
        var header = sheet.Rows.FirstOrDefault(row => !row.IsEmpty);
        if (header == null) throw new InvalidDataException(sheet.Name + ": empty sheet.");
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in header.Cells)
        {
            string name = cell.Value.Text.Trim();
            if (name.Length > 0 && !columns.TryAdd(name, cell.Key)) throw new InvalidDataException(sheet.Name + ": duplicate column " + name);
        }
        foreach (string name in new[] { "storyId", "line", "background", "leftCharacter", "rightCharacter", "speaker", "dialogue" })
            if (!columns.ContainsKey(name)) throw new InvalidDataException(sheet.Name + ": missing column " + name);
        var result = new Dictionary<string, Line>(StringComparer.Ordinal);
        foreach (var row in sheet.Rows.Where(row => row.Number > header.Number && !row.IsEmpty))
        {
            string ReadCell(string name)
            {
                if (!columns.TryGetValue(name, out int index)) return "";
                var cell = row.At(index);
                if (cell.HasFormula || cell.IsError) throw new InvalidDataException(sheet.Name + " row " + row.Number + ": use literal values, not formulas/errors.");
                return cell.Text;
            }
            string id = ReadCell("storyId").Trim();
            if (string.IsNullOrWhiteSpace(id) || id == "." || id == ".." || id.IndexOfAny("/\\:*?\"<>|".ToCharArray()) >= 0 || id.Any(char.IsControl) || id.EndsWith(".", StringComparison.Ordinal))
                throw new InvalidDataException(sheet.Name + " row " + row.Number + ": invalid storyId filename.");
            if (!int.TryParse(ReadCell("line"), NumberStyles.None, CultureInfo.InvariantCulture, out int number) || number < 1)
                throw new InvalidDataException(sheet.Name + " row " + row.Number + ": invalid line number.");
            var line = new Line
            {
                story = id, number = number, speaker = ReadCell("speaker"), text = ReadCell("dialogue"),
                background = ReadCell("background").Trim(),
                leftCharacter = ReadCell("leftCharacter").Trim(),
                rightCharacter = ReadCell("rightCharacter").Trim(),
                backgroundTrigger = ReadCell("backgroundTrigger").Trim(),
                leftTrigger = ReadCell("leftTrigger").Trim(),
                rightTrigger = ReadCell("rightTrigger").Trim()
            };
            if (string.IsNullOrWhiteSpace(line.text)) throw new InvalidDataException(sheet.Name + " row " + row.Number + ": dialogue is empty.");
            if (!result.TryAdd(Key(line), line)) throw new InvalidDataException(sheet.Name + ": duplicate storyId / line: " + id + " / " + number);
        }
        return result;
    }

    private static string Key(Line line) => line.story + "\n" + line.number.ToString(CultureInfo.InvariantCulture);

    private static Sprite LoadSprite(string reference, Dictionary<string, Sprite> cache, string context)
    {
        if (string.IsNullOrEmpty(reference)) return null;
        if (cache.TryGetValue(reference, out var sprite)) return sprite;

        int separator = reference.IndexOf('#');
        string path = (separator < 0 ? reference : reference.Substring(0, separator)).Trim().Replace('\\', '/');
        string name = separator < 0 ? null : reference.Substring(separator + 1).Trim();
        if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Split('/').Any(part => part == "." || part == ".."))
            throw new InvalidDataException(context + ": use a sprite asset path under Assets: " + reference);

        var matches = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .Where(candidate => name == null || candidate.name == name).Take(2).ToArray();
        if (matches.Length != 1)
            throw new InvalidDataException(context + ": select one imported sprite using Assets/path.png#SpriteName: " + reference);

        cache.Add(reference, matches[0]);
        return matches[0];
    }

    private static string AssetFolder(string path)
    {
        path = (path ?? "").Trim().Replace('\\', '/').TrimEnd('/');
        if (!(path == "Assets" || path.StartsWith("Assets/", StringComparison.Ordinal)) || path.Split('/').Any(part => part == ".." || part == "." || part.Length == 0))
            throw new InvalidDataException("Use a project folder under Assets: " + path);
        return path;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = path.Substring(0, path.LastIndexOf('/'));
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(parent.Length + 1));
    }
}

