using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ItemDataExcelTools;
using UnityEditor;
using UnityEngine;

// Editor-only XLSX import. StoryData and the runtime story player remain unchanged.
public static class StoryExcelImporter
{
    public sealed class Entry
    {
        public string assetPath;
        public Sprite[] sprites;
        public StoryData.DialogueData[] dialogues;
    }

    private sealed class Line
    {
        public string story, folder, speaker, text;
        public int number;
        public bool nextImage;
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
        foreach (var story in reference.Values.GroupBy(row => row.story).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var lines = story.OrderBy(row => row.number).ToArray();
            var sprites = LoadSprites(lines[0].folder);
            int imageIndex = 0;
            var dialogues = new List<StoryData.DialogueData>();
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.number != i + 1) throw new InvalidDataException(story.Key + ": line numbers must be consecutive from 1.");
                if (line.folder != lines[0].folder) throw new InvalidDataException(story.Key + ": use one spriteFolder per story.");
                if (i == 0 && line.nextImage) throw new InvalidDataException(story.Key + ": first line must have nextImage=FALSE (starts at image 0).");
                if (line.nextImage) imageIndex++;
                if (imageIndex >= sprites.Length) throw new InvalidDataException(story.Key + ": nextImage exceeds the available sprites.");
                var translations = new List<StoryData.LocalizedDialogue>();
                for (int l = 0; l < languages.Count; l++)
                {
                    var other = translated[l][Key(line)];
                    if (other.folder != line.folder || other.nextImage != line.nextImage)
                        throw new InvalidDataException(story.Key + " / " + line.number + ": spriteFolder and nextImage must match across languages.");
                    translations.Add(new StoryData.LocalizedDialogue { languageType = languages[l], speaker = other.speaker, dialogue = other.text });
                }
                dialogues.Add(new StoryData.DialogueData { nextImage = line.nextImage, translations = translations.ToArray() });
            }
            string path = outputFolder + "/" + story.Key + ".asset";
            var existing = AssetDatabase.LoadMainAssetAtPath(path);
            if (existing != null && !(existing is StoryData)) throw new InvalidDataException("Another asset occupies: " + path);
            entries.Add(new Entry { assetPath = path, sprites = sprites, dialogues = dialogues.ToArray() });
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
            data.sprites = entry.sprites;
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
        foreach (string name in new[] { "storyId", "line", "spriteFolder", "nextImage", "speaker", "dialogue" })
            if (!columns.ContainsKey(name)) throw new InvalidDataException(sheet.Name + ": missing column " + name);
        var result = new Dictionary<string, Line>(StringComparer.Ordinal);
        foreach (var row in sheet.Rows.Where(row => row.Number > header.Number && !row.IsEmpty))
        {
            string ReadCell(string name)
            {
                var cell = row.At(columns[name]);
                if (cell.HasFormula || cell.IsError) throw new InvalidDataException(sheet.Name + " row " + row.Number + ": use literal values, not formulas/errors.");
                return cell.Text;
            }
            string id = ReadCell("storyId").Trim();
            if (string.IsNullOrWhiteSpace(id) || id == "." || id == ".." || id.IndexOfAny("/\\:*?\"<>|".ToCharArray()) >= 0 || id.Any(char.IsControl) || id.EndsWith(".", StringComparison.Ordinal))
                throw new InvalidDataException(sheet.Name + " row " + row.Number + ": invalid storyId filename.");
            if (!int.TryParse(ReadCell("line"), NumberStyles.None, CultureInfo.InvariantCulture, out int number) || number < 1)
                throw new InvalidDataException(sheet.Name + " row " + row.Number + ": invalid line number.");
            string flag = ReadCell("nextImage").Trim();
            bool next;
            if (flag == "1") next = true;
            else if (flag == "0") next = false;
            else if (!bool.TryParse(flag, out next)) throw new InvalidDataException(sheet.Name + " row " + row.Number + ": nextImage must be TRUE/FALSE or 1/0.");
            var line = new Line { story = id, number = number, folder = AssetFolder(ReadCell("spriteFolder")), nextImage = next, speaker = ReadCell("speaker"), text = ReadCell("dialogue") };
            if (string.IsNullOrWhiteSpace(line.text)) throw new InvalidDataException(sheet.Name + " row " + row.Number + ": dialogue is empty.");
            if (!result.TryAdd(Key(line), line)) throw new InvalidDataException(sheet.Name + ": duplicate storyId / line: " + id + " / " + number);
        }
        return result;
    }

    private static string Key(Line line) => line.story + "\n" + line.number.ToString(CultureInfo.InvariantCulture);

    private static Sprite[] LoadSprites(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder)) throw new InvalidDataException("Sprite folder does not exist: " + folder);
        var paths = AssetDatabase.FindAssets("t:Sprite", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath)
            .Distinct().OrderBy(path => path, Comparer<string>.Create(EditorUtility.NaturalCompare));
        var sprites = paths.SelectMany(path => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(sprite => sprite.name, Comparer<string>.Create(EditorUtility.NaturalCompare))).ToArray();
        if (sprites.Length == 0) throw new InvalidDataException("No imported sprites in: " + folder);
        return sprites;
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

