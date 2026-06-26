#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class ItemRecipeCsvImporter : EditorWindow
{
    [Header("CSV")]
    [SerializeField] private string csvPath = "";

    [Header("Assets")]
    [SerializeField] private DefaultAsset itemDataFolder;
    [SerializeField] private DefaultAsset recipeOutputFolder;
    [SerializeField] private string outputFolderPath = "Assets/GameData/Recipes";

    [Header("Options")]
    [SerializeField] private bool overwriteExisting = true;
    [SerializeField] private bool stopOnMissingItem = true;

    private Vector2 scroll;
    private readonly List<string> logs = new List<string>();

    [MenuItem("Tools/GameData/Recipe/CSV Recipe Importer")]
    public static void Open()
    {
        GetWindow<ItemRecipeCsvImporter>("CSV Recipe Importer");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("CSV Recipe Importer", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "CSV 컬럼은 Result Name, Grade, Series, Material 1~4만 사용합니다.\n" +
            "Material 칸에 같은 아이템 이름을 반복해서 적으면 중복 재료로 들어갑니다.",
            MessageType.Info);

        DrawCsvSection();

        EditorGUILayout.Space(8);

        itemDataFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "ItemData Folder",
            itemDataFolder,
            typeof(DefaultAsset),
            false);

        recipeOutputFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Recipe Output Folder",
            recipeOutputFolder,
            typeof(DefaultAsset),
            false);

        outputFolderPath = EditorGUILayout.TextField("Output Folder Path", outputFolderPath);

        overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
        stopOnMissingItem = EditorGUILayout.Toggle("Stop On Missing Item", stopOnMissingItem);

        EditorGUILayout.Space(10);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(csvPath)))
        {
            if (GUILayout.Button("Import Recipes From CSV", GUILayout.Height(34)))
                Import();
        }

        DrawLogs();
    }

    private void DrawCsvSection()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("CSV", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            csvPath = EditorGUILayout.TextField("CSV Path", csvPath);

            if (GUILayout.Button("Select", GUILayout.Width(80)))
            {
                string selected = EditorUtility.OpenFilePanel(
                    "Select Recipe CSV",
                    Application.dataPath,
                    "csv");

                if (!string.IsNullOrEmpty(selected))
                    csvPath = selected;
            }
        }
    }

    private void DrawLogs()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(140));

        for (int i = 0; i < logs.Count; i++)
            EditorGUILayout.LabelField(logs[i], EditorStyles.wordWrappedLabel);

        EditorGUILayout.EndScrollView();
    }

    private void Import()
    {
        logs.Clear();

        if (!File.Exists(csvPath))
        {
            LogError($"CSV file not found: {csvPath}");
            return;
        }

        string itemFolderPath = GetAssetFolderPath(itemDataFolder);
        if (string.IsNullOrEmpty(itemFolderPath) || !AssetDatabase.IsValidFolder(itemFolderPath))
        {
            LogError("ItemData Folder를 지정해줘.");
            return;
        }

        string finalOutputFolderPath = GetAssetFolderPath(recipeOutputFolder);
        if (string.IsNullOrEmpty(finalOutputFolderPath))
            finalOutputFolderPath = outputFolderPath;

        if (!EnsureFolder(finalOutputFolderPath))
        {
            LogError($"Output folder create failed: {finalOutputFolderPath}");
            return;
        }

        Dictionary<string, ItemData> itemMap = BuildItemMap(itemFolderPath);
        if (itemMap.Count == 0)
        {
            LogError($"No ItemData assets found in: {itemFolderPath}");
            return;
        }

        List<RecipeCsvRow> rows;

        try
        {
            rows = SimpleCsvReader.ReadRecipeRows(csvPath);
        }
        catch (Exception ex)
        {
            LogError($"CSV read failed: {ex.Message}");
            return;
        }

        if (rows.Count == 0)
        {
            LogError("No rows found in CSV.");
            return;
        }

        int created = 0;
        int updated = 0;
        int skipped = 0;
        int failed = 0;

        for (int i = 0; i < rows.Count; i++)
        {
            int csvRowNumber = i + 2;
            RecipeCsvRow row = rows[i];

            if (string.IsNullOrWhiteSpace(row.resultName))
            {
                skipped++;
                continue;
            }

            if (!TryFindItem(itemMap, row.resultName, out ItemData resultItem))
            {
                failed++;
                LogError($"Row {csvRowNumber}: Result Item not found: {row.resultName}");

                if (stopOnMissingItem)
                    break;

                continue;
            }

            List<ItemData> materialItems = new List<ItemData>();

            for (int m = 0; m < row.materialNames.Count; m++)
            {
                string materialName = row.materialNames[m];

                if (string.IsNullOrWhiteSpace(materialName))
                    continue;

                if (!TryFindItem(itemMap, materialName, out ItemData materialItem))
                {
                    failed++;
                    LogError($"Row {csvRowNumber}: Material not found: {materialName}");

                    if (stopOnMissingItem)
                        return;

                    continue;
                }

                materialItems.Add(materialItem);
            }

            if (materialItems.Count == 0)
            {
                failed++;
                LogError($"Row {csvRowNumber}: No materials: {row.resultName}");

                if (stopOnMissingItem)
                    break;

                continue;
            }

            ItemGrade grade = ParseEnumOrFallback(row.grade, resultItem.grade);
            ItemSeries series = ParseEnumOrFallback(row.series, resultItem.series);

            string assetName = MakeSafeAssetName("Recipe_" + row.resultName) + ".asset";
            string assetPath = CombineAssetPath(finalOutputFolderPath, assetName);

            ItemRecipeData recipe = overwriteExisting
                ? AssetDatabase.LoadAssetAtPath<ItemRecipeData>(assetPath)
                : null;

            bool isNew = recipe == null;

            if (isNew)
            {
                recipe = CreateInstance<ItemRecipeData>();

                if (!overwriteExisting)
                    assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            }
            else
            {
                Undo.RecordObject(recipe, "Update Item Recipe Data");
            }

            ApplyRecipe(recipe, resultItem, grade, series, materialItems);

            if (isNew)
            {
                AssetDatabase.CreateAsset(recipe, assetPath);
                created++;
            }
            else
            {
                EditorUtility.SetDirty(recipe);
                updated++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Log($"Done. Created: {created}, Updated: {updated}, Skipped: {skipped}, Failed: {failed}");
    }

    private static void ApplyRecipe(
        ItemRecipeData recipe,
        ItemData resultItem,
        ItemGrade grade,
        ItemSeries series,
        List<ItemData> materialItems)
    {
        recipe.resultItem = resultItem;
        recipe.itemGrade = grade;
        recipe.itemSeries = series;

        recipe.materials = new RecipeMaterial[materialItems.Count];

        for (int i = 0; i < materialItems.Count; i++)
        {
            recipe.materials[i] = new RecipeMaterial
            {
                itemData = materialItems[i],
                amount = 1
            };
        }
    }

    private static Dictionary<string, ItemData> BuildItemMap(string itemFolderPath)
    {
        Dictionary<string, ItemData> map = new Dictionary<string, ItemData>();

        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { itemFolderPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);

            if (item == null)
                continue;

            AddItemKey(map, item.name, item);
            AddItemKey(map, item.dataId, item);
        }

        return map;
    }

    private static void AddItemKey(Dictionary<string, ItemData> map, string key, ItemData item)
    {
        string normalized = NormalizeItemKey(key);

        if (string.IsNullOrEmpty(normalized))
            return;

        if (!map.ContainsKey(normalized))
            map.Add(normalized, item);
    }

    private static bool TryFindItem(Dictionary<string, ItemData> itemMap, string itemName, out ItemData item)
    {
        return itemMap.TryGetValue(NormalizeItemKey(itemName), out item);
    }

    private static string NormalizeItemKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        string trimmed = value.Trim().ToLowerInvariant();
        return Regex.Replace(trimmed, @"[\s_\-]+", "");
    }

    private static TEnum ParseEnumOrFallback<TEnum>(string value, TEnum fallback) where TEnum : struct
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        return Enum.TryParse(value.Trim(), true, out TEnum parsed)
            ? parsed
            : fallback;
    }

    private static string GetAssetFolderPath(DefaultAsset folder)
    {
        if (folder == null)
            return "";

        string path = AssetDatabase.GetAssetPath(folder);

        if (string.IsNullOrEmpty(path))
            return "";

        if (AssetDatabase.IsValidFolder(path))
            return path;

        string directory = Path.GetDirectoryName(path);
        return string.IsNullOrEmpty(directory) ? "" : directory.Replace("\\", "/");
    }

    private static bool EnsureFolder(string folderPath)
    {
        folderPath = folderPath.Replace("\\", "/").Trim('/');

        if (AssetDatabase.IsValidFolder(folderPath))
            return true;

        string[] parts = folderPath.Split('/');

        if (parts.Length == 0 || parts[0] != "Assets")
            return false;

        string current = "Assets";

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);

            current = next;
        }

        return AssetDatabase.IsValidFolder(folderPath);
    }

    private static string CombineAssetPath(string folder, string file)
    {
        return folder.TrimEnd('/', '\\') + "/" + file;
    }

    private static string MakeSafeAssetName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Recipe";

        foreach (char c in Path.GetInvalidFileNameChars())
            value = value.Replace(c.ToString(), "_");

        return Regex.Replace(value.Trim(), @"\s+", "_");
    }

    private void Log(string message)
    {
        logs.Add(message);
        Debug.Log(message);
    }

    private void LogError(string message)
    {
        logs.Add("ERROR: " + message);
        Debug.LogError(message);
    }

    private class RecipeCsvRow
    {
        public string resultName;
        public string grade;
        public string series;
        public readonly List<string> materialNames = new List<string>();
    }

    private static class SimpleCsvReader
    {
        public static List<RecipeCsvRow> ReadRecipeRows(string path)
        {
            string text = ReadAllTextSmart(path);
            List<List<string>> matrix = ParseCsv(text);

            if (matrix.Count == 0)
                return new List<RecipeCsvRow>();

            Dictionary<string, int> headerMap = BuildHeaderMap(matrix[0]);

            RequireHeader(headerMap, "Result Name");
            RequireHeader(headerMap, "Grade");
            RequireHeader(headerMap, "Series");

            List<RecipeCsvRow> rows = new List<RecipeCsvRow>();

            for (int r = 1; r < matrix.Count; r++)
            {
                List<string> values = matrix[r];

                if (values.All(string.IsNullOrWhiteSpace))
                    continue;

                RecipeCsvRow row = new RecipeCsvRow
                {
                    resultName = GetCell(values, headerMap, "Result Name"),
                    grade = GetCell(values, headerMap, "Grade"),
                    series = GetCell(values, headerMap, "Series")
                };

                row.materialNames.Add(GetCell(values, headerMap, "Material 1"));
                row.materialNames.Add(GetCell(values, headerMap, "Material 2"));
                row.materialNames.Add(GetCell(values, headerMap, "Material 3"));
                row.materialNames.Add(GetCell(values, headerMap, "Material 4"));

                rows.Add(row);
            }

            return rows;
        }

        private static string ReadAllTextSmart(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);

            try
            {
                UTF8Encoding utf8 = new UTF8Encoding(false, true);
                return utf8.GetString(bytes);
            }
            catch
            {
                return Encoding.Default.GetString(bytes);
            }
        }

        private static List<List<string>> ParseCsv(string text)
        {
            List<List<string>> rows = new List<List<string>>();
            List<string> currentRow = new List<string>();
            StringBuilder currentCell = new StringBuilder();

            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        currentCell.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    continue;
                }

                if (c == ',' && !inQuotes)
                {
                    currentRow.Add(currentCell.ToString());
                    currentCell.Clear();
                    continue;
                }

                if ((c == '\n' || c == '\r') && !inQuotes)
                {
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                        i++;

                    currentRow.Add(currentCell.ToString());
                    currentCell.Clear();

                    if (!IsEmptyRow(currentRow))
                        rows.Add(currentRow);

                    currentRow = new List<string>();
                    continue;
                }

                currentCell.Append(c);
            }

            currentRow.Add(currentCell.ToString());

            if (!IsEmptyRow(currentRow))
                rows.Add(currentRow);

            return rows;
        }

        private static bool IsEmptyRow(List<string> row)
        {
            if (row == null || row.Count == 0)
                return true;

            for (int i = 0; i < row.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(row[i]))
                    return false;
            }

            return true;
        }

        private static Dictionary<string, int> BuildHeaderMap(List<string> headers)
        {
            Dictionary<string, int> map = new Dictionary<string, int>();

            for (int i = 0; i < headers.Count; i++)
            {
                string normalized = NormalizeHeader(headers[i]);

                if (string.IsNullOrEmpty(normalized))
                    continue;

                if (!map.ContainsKey(normalized))
                    map.Add(normalized, i);
            }

            return map;
        }

        private static void RequireHeader(Dictionary<string, int> headerMap, string header)
        {
            if (!headerMap.ContainsKey(NormalizeHeader(header)))
                throw new Exception($"Required header missing: {header}");
        }

        private static string GetCell(List<string> values, Dictionary<string, int> headerMap, string header)
        {
            if (!headerMap.TryGetValue(NormalizeHeader(header), out int index))
                return "";

            if (index < 0 || index >= values.Count)
                return "";

            return values[index]?.Trim() ?? "";
        }

        private static string NormalizeHeader(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            string trimmed = value.Trim().TrimStart('\uFEFF').ToLowerInvariant();
            return Regex.Replace(trimmed, @"[\s_\-,]+", "");
        }
    }
}
#endif