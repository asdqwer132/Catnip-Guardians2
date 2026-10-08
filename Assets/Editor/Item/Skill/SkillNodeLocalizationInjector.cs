#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public class SkillNodeLocalizationInjector : EditorWindow
{
    [Header("Localization File")]
    [SerializeField]
    private TextAsset localizationFile;

    [Header("Skill Node Folder")]
    [SerializeField]
    private DefaultAsset skillNodeFolder;

    [Header("Matching")]
    [SerializeField]
    private bool allowAssetNameMatching = true;

    [Header("Import Option")]
    [SerializeField]
    private bool preserveExistingLanguages = true;

    private Vector2 scrollPosition;

    private static readonly FieldInfo LanguageDataMapField =
        typeof(DefaultData).GetField(
            "languageDataMap",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

    [MenuItem("Tools/Skill Tree/Localization Injector")]
    public static void OpenWindow()
    {
        SkillNodeLocalizationInjector window =
            GetWindow<SkillNodeLocalizationInjector>();

        window.titleContent = new GUIContent("Skill Localization");
        window.minSize = new Vector2(480f, 350f);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField(
            "Skill Node Localization Injector",
            EditorStyles.boldLabel
        );

        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Excel 파일을 CSV UTF-8 또는 TSV 형식으로 저장해서 넣어주세요.\n\n" +
            "필수 열:\n" +
            "Key | Language | Name | Description\n\n" +
            "Language 값:\n" +
            "ko 또는 en",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        localizationFile = (TextAsset)EditorGUILayout.ObjectField(
            "CSV / TSV File",
            localizationFile,
            typeof(TextAsset),
            false
        );

        skillNodeFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Skill Node Folder",
            skillNodeFolder,
            typeof(DefaultAsset),
            false
        );

        EditorGUILayout.Space(10);

        allowAssetNameMatching = EditorGUILayout.Toggle(
            new GUIContent(
                "Allow Asset Name Matching",
                "dataId로 찾지 못했을 때 에셋 이름으로도 검색합니다."
            ),
            allowAssetNameMatching
        );

        preserveExistingLanguages = EditorGUILayout.Toggle(
            new GUIContent(
                "Preserve Existing Languages",
                "엑셀에 없는 기존 언어 데이터는 유지합니다."
            ),
            preserveExistingLanguages
        );

        EditorGUILayout.Space(20);

        GUI.enabled = localizationFile != null;

        if (GUILayout.Button("Inject Localization Data", GUILayout.Height(40)))
            InjectLocalization();

        GUI.enabled = true;

        EditorGUILayout.EndScrollView();
    }

    private void InjectLocalization()
    {
        if (localizationFile == null)
        {
            Debug.LogError("Localization CSV/TSV 파일이 지정되지 않았습니다.");
            return;
        }

        string folderPath = GetSkillNodeFolderPath();

        if (string.IsNullOrEmpty(folderPath))
            return;

        ParsedTable table;

        try
        {
            table = ParseTable(localizationFile.text);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Localization 파일을 읽는 중 오류가 발생했습니다.\n{exception}"
            );

            return;
        }

        if (!ValidateHeaders(table, out ColumnIndices columns))
            return;

        Dictionary<string, List<SkillNodeData>> nodeMap =
            BuildSkillNodeMap(folderPath);

        if (nodeMap.Count == 0)
        {
            Debug.LogWarning(
                $"SkillNodeData를 찾지 못했습니다. 폴더: {folderPath}"
            );

            return;
        }

        int processedRowCount = 0;
        int changedNodeCount = 0;
        int missingKeyCount = 0;
        int invalidLanguageCount = 0;
        int ambiguousKeyCount = 0;

        HashSet<SkillNodeData> changedNodes =
            new HashSet<SkillNodeData>();

        HashSet<string> importedLanguagePairs =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int rowIndex = 1; rowIndex < table.Rows.Count; rowIndex++)
            {
                List<string> row = table.Rows[rowIndex];

                string key = GetCell(row, columns.Key).Trim();
                string languageText = GetCell(row, columns.Language).Trim();
                string skillName = GetCell(row, columns.Name).Trim();
                string description = GetCell(row, columns.Description).Trim();

                if (string.IsNullOrWhiteSpace(key))
                    continue;

                processedRowCount++;

                string normalizedKey = NormalizeKey(key);

                if (!nodeMap.TryGetValue(
                    normalizedKey,
                    out List<SkillNodeData> matchedNodes))
                {
                    Debug.LogWarning(
                        $"[Localization] SkillNodeData를 찾지 못했습니다. " +
                        $"Row: {rowIndex + 1}, Key: {key}"
                    );

                    missingKeyCount++;
                    continue;
                }

                if (matchedNodes.Count > 1)
                {
                    Debug.LogError(
                        $"[Localization] 같은 Key를 가진 SkillNodeData가 여러 개입니다. " +
                        $"Row: {rowIndex + 1}, Key: {key}\n" +
                        GetAssetPathList(matchedNodes)
                    );

                    ambiguousKeyCount++;
                    continue;
                }

                if (!TryParseLanguage(
                    languageText,
                    out language parsedLanguage))
                {
                    Debug.LogWarning(
                        $"[Localization] Language 값을 변환할 수 없습니다. " +
                        $"Row: {rowIndex + 1}, Value: {languageText}\n" +
                        $"현재 language enum: {GetLanguageEnumNames()}"
                    );

                    invalidLanguageCount++;
                    continue;
                }

                SkillNodeData skillNode = matchedNodes[0];

                string duplicatePairKey =
                    normalizedKey + "|" + parsedLanguage;

                if (!importedLanguagePairs.Add(duplicatePairKey))
                {
                    Debug.LogWarning(
                        $"[Localization] 같은 Key와 Language가 중복되었습니다. " +
                        $"마지막 행의 값으로 덮어씁니다. " +
                        $"Row: {rowIndex + 1}, Key: {key}, Language: {languageText}"
                    );
                }

                if (!changedNodes.Contains(skillNode))
                {
                    Undo.RecordObject(
                        skillNode,
                        "Inject Skill Node Localization"
                    );

                    if (!preserveExistingLanguages)
                        skillNode.data = Array.Empty<Description>();

                    changedNodes.Add(skillNode);
                    changedNodeCount++;
                }

                SetLocalization(
                    skillNode,
                    parsedLanguage,
                    skillName,
                    description
                );

                ClearLanguageCache(skillNode);

                EditorUtility.SetDirty(skillNode);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string result =
            "스킬 노드 번역 주입 완료\n\n" +
            $"읽은 행: {processedRowCount}\n" +
            $"수정된 노드: {changedNodeCount}\n" +
            $"찾지 못한 Key: {missingKeyCount}\n" +
            $"잘못된 Language: {invalidLanguageCount}\n" +
            $"중복된 노드 Key: {ambiguousKeyCount}";

        Debug.Log(result);

        EditorUtility.DisplayDialog(
            "Localization Injection Complete",
            result,
            "확인"
        );
    }

    private string GetSkillNodeFolderPath()
    {
        if (skillNodeFolder == null)
            return "Assets";

        string folderPath =
            AssetDatabase.GetAssetPath(skillNodeFolder);

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogError(
                $"선택한 경로가 폴더가 아닙니다: {folderPath}"
            );

            return null;
        }

        return folderPath;
    }

    private Dictionary<string, List<SkillNodeData>> BuildSkillNodeMap(
        string folderPath)
    {
        Dictionary<string, List<SkillNodeData>> result =
            new Dictionary<string, List<SkillNodeData>>();

        string[] guids = AssetDatabase.FindAssets(
            "t:SkillNodeData",
            new[] { folderPath }
        );

        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath =
                AssetDatabase.GUIDToAssetPath(guids[i]);

            SkillNodeData skillNode =
                AssetDatabase.LoadAssetAtPath<SkillNodeData>(assetPath);

            if (skillNode == null)
                continue;

            if (!string.IsNullOrWhiteSpace(skillNode.dataId))
            {
                AddNodeToMap(
                    result,
                    NormalizeKey(skillNode.dataId),
                    skillNode
                );
            }

            if (allowAssetNameMatching &&
                !string.IsNullOrWhiteSpace(skillNode.name))
            {
                string assetNameKey =
                    NormalizeKey(skillNode.name);

                string dataIdKey =
                    NormalizeKey(skillNode.dataId);

                if (assetNameKey != dataIdKey)
                {
                    AddNodeToMap(
                        result,
                        assetNameKey,
                        skillNode
                    );
                }
            }
        }

        return result;
    }

    private static void AddNodeToMap(
        Dictionary<string, List<SkillNodeData>> nodeMap,
        string key,
        SkillNodeData node)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        if (!nodeMap.TryGetValue(
            key,
            out List<SkillNodeData> nodes))
        {
            nodes = new List<SkillNodeData>();
            nodeMap.Add(key, nodes);
        }

        if (!nodes.Contains(node))
            nodes.Add(node);
    }

    private static void SetLocalization(
        SkillNodeData skillNode,
        language targetLanguage,
        string skillName,
        string description)
    {
        List<Description> languageDataList =
            new List<Description>();

        if (skillNode.data != null)
        {
            for (int i = 0; i < skillNode.data.Length; i++)
            {
                Description existingData = skillNode.data[i];

                if (existingData != null)
                    languageDataList.Add(existingData);
            }
        }

        Description targetData = null;

        for (int i = 0; i < languageDataList.Count; i++)
        {
            if (EqualityComparer<language>.Default.Equals(
                languageDataList[i].language,
                targetLanguage))
            {
                targetData = languageDataList[i];
                break;
            }
        }

        if (targetData == null)
        {
            targetData = new Description
            {
                language = targetLanguage
            };

            languageDataList.Add(targetData);
        }

        targetData.dataName = skillName;
        targetData.description = description;

        skillNode.data = languageDataList.ToArray();
    }

    private static void ClearLanguageCache(
        DefaultData targetData)
    {
        if (LanguageDataMapField == null)
            return;

        LanguageDataMapField.SetValue(targetData, null);
    }

    private static bool TryParseLanguage(
        string value,
        out language result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (Enum.TryParse(value, true, out result))
            return true;

        string normalizedValue = NormalizeKey(value);

        string[] candidates;

        switch (normalizedValue)
        {
            case "KO":
            case "KR":
            case "KOR":
            case "KOREAN":
            case "KOREA":
            case "한국어":
            case "한글":
                candidates = new[]
                {
                    "KO",
                    "KR",
                    "KOR",
                    "KOREAN",
                    "KOREA",
                    "한국어",
                    "한글"
                };
                break;

            case "EN":
            case "ENG":
            case "ENGLISH":
            case "US":
                candidates = new[]
                {
                    "EN",
                    "ENG",
                    "ENGLISH",
                    "US"
                };
                break;

            default:
                return false;
        }

        Array enumValues = Enum.GetValues(typeof(language));

        for (int i = 0; i < enumValues.Length; i++)
        {
            language enumValue = (language)enumValues.GetValue(i);
            string enumName = NormalizeKey(enumValue.ToString());

            for (int j = 0; j < candidates.Length; j++)
            {
                if (enumName == NormalizeKey(candidates[j]))
                {
                    result = enumValue;
                    return true;
                }
            }
        }

        return false;
    }

    private static string GetLanguageEnumNames()
    {
        return string.Join(", ", Enum.GetNames(typeof(language)));
    }

    private static bool ValidateHeaders(
        ParsedTable table,
        out ColumnIndices columns)
    {
        columns = new ColumnIndices();

        if (table.Rows.Count == 0)
        {
            Debug.LogError("Localization 파일이 비어 있습니다.");
            return false;
        }

        List<string> headers = table.Rows[0];

        columns.Key = FindColumn(headers, "Key");
        columns.Language = FindColumn(headers, "Language");
        columns.Name = FindColumn(headers, "Name");
        columns.Description = FindColumn(headers, "Description");

        List<string> missingHeaders = new List<string>();

        if (columns.Key < 0)
            missingHeaders.Add("Key");

        if (columns.Language < 0)
            missingHeaders.Add("Language");

        if (columns.Name < 0)
            missingHeaders.Add("Name");

        if (columns.Description < 0)
            missingHeaders.Add("Description");

        if (missingHeaders.Count > 0)
        {
            Debug.LogError(
                "필수 열이 없습니다: " +
                string.Join(", ", missingHeaders) +
                "\n\n필요한 열: Key, Language, Name, Description"
            );

            return false;
        }

        return true;
    }

    private static int FindColumn(
        List<string> headers,
        string targetHeader)
    {
        string normalizedTarget =
            NormalizeKey(targetHeader);

        for (int i = 0; i < headers.Count; i++)
        {
            string header = headers[i]
                .Replace("\uFEFF", string.Empty);

            if (NormalizeKey(header) == normalizedTarget)
                return i;
        }

        return -1;
    }

    private static string GetCell(
        List<string> row,
        int index)
    {
        if (row == null)
            return string.Empty;

        if (index < 0 || index >= row.Count)
            return string.Empty;

        return row[index] ?? string.Empty;
    }

    private static string NormalizeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        StringBuilder builder = new StringBuilder();

        string trimmed = value.Trim();

        for (int i = 0; i < trimmed.Length; i++)
        {
            char character = trimmed[i];

            if (char.IsLetterOrDigit(character))
                builder.Append(char.ToUpperInvariant(character));
        }

        return builder.ToString();
    }

    private static string GetAssetPathList(
        List<SkillNodeData> nodes)
    {
        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < nodes.Count; i++)
        {
            builder.Append("- ");
            builder.Append(
                AssetDatabase.GetAssetPath(nodes[i])
            );
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static ParsedTable ParseTable(string text)
    {
        ParsedTable result = new ParsedTable();

        if (string.IsNullOrEmpty(text))
            return result;

        char delimiter = DetectDelimiter(text);

        List<string> currentRow = new List<string>();
        StringBuilder currentCell = new StringBuilder();

        bool insideQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char currentCharacter = text[i];

            if (currentCharacter == '"')
            {
                if (insideQuotes &&
                    i + 1 < text.Length &&
                    text[i + 1] == '"')
                {
                    currentCell.Append('"');
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }

                continue;
            }

            if (currentCharacter == delimiter &&
                !insideQuotes)
            {
                currentRow.Add(currentCell.ToString());
                currentCell.Clear();
                continue;
            }

            bool isNewLine =
                currentCharacter == '\n' ||
                currentCharacter == '\r';

            if (isNewLine && !insideQuotes)
            {
                if (currentCharacter == '\r' &&
                    i + 1 < text.Length &&
                    text[i + 1] == '\n')
                {
                    i++;
                }

                currentRow.Add(currentCell.ToString());
                currentCell.Clear();

                if (!IsEmptyRow(currentRow))
                    result.Rows.Add(currentRow);

                currentRow = new List<string>();
                continue;
            }

            currentCell.Append(currentCharacter);
        }

        currentRow.Add(currentCell.ToString());

        if (!IsEmptyRow(currentRow))
            result.Rows.Add(currentRow);

        return result;
    }

    private static char DetectDelimiter(string text)
    {
        bool insideQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char character = text[i];

            if (character == '"')
                insideQuotes = !insideQuotes;

            if (!insideQuotes && character == '\t')
                return '\t';

            if (!insideQuotes &&
                (character == '\n' || character == '\r'))
            {
                break;
            }
        }

        return ',';
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

    private class ParsedTable
    {
        public readonly List<List<string>> Rows =
            new List<List<string>>();
    }

    private struct ColumnIndices
    {
        public int Key;
        public int Language;
        public int Name;
        public int Description;
    }
}

#endif