#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace ItemDataExcelTools
{
    public sealed class ItemDataExcelImporter : EditorWindow
    {
        [SerializeField] private string workbookPath = "";
        [SerializeField] private string outputFolder = "Assets/GameData/Items";
        [SerializeField] private string iconFolder = "Assets/Art/Icons/Items";
        [SerializeField] private bool findMissingIcons = true;
        [SerializeField] private bool replaceDescriptions;
        [SerializeField] private bool showEnums;
        private Vector2 scroll;
        private Plan preview;
        private string status = "엑셀 파일을 선택하고 검증해 주세요.";

        private static readonly string[] ItemHeaders =
        {
            "enabled", "assetName", "assetPath", "dataNumbering", "dataId", "dataType",
            "requireUnlock", "grade", "category", "series", "weight", "cooldown", "icon", "effectDatas"
        };
        private static readonly string[] DescriptionHeaders = { "dataId", "language", "dataName", "description" };
        private static readonly FieldInfo LanguageMapField = typeof(DefaultData).GetField(
            "languageDataMap", BindingFlags.Instance | BindingFlags.NonPublic);

        [MenuItem("Tools/GameData/ItemData Excel Importer")]
        public static void Open()
        {
            var window = GetWindow<ItemDataExcelImporter>("ItemData Excel");
            window.minSize = new Vector2(740, 460);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("ItemData 엑셀 가져오기", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Items의 dataId로 기존 ItemData를 찾아 갱신하고, 없으면 새 .asset을 생성합니다. " +
                "Descriptions는 같은 dataId의 언어별 이름과 설명입니다. 엑셀을 저장한 뒤 가져오세요.", MessageType.Info);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            workbookPath = EditorGUILayout.TextField("엑셀 파일 (.xlsx)", workbookPath);
            if (GUILayout.Button("선택", GUILayout.Width(60)))
            {
                string selected = EditorUtility.OpenFilePanel("ItemData 엑셀 선택", "", "xlsx");
                if (!string.IsNullOrEmpty(selected)) workbookPath = selected;
                preview = null;
            }
            EditorGUILayout.EndHorizontal();
            outputFolder = EditorGUILayout.TextField("새 에셋 생성 폴더", outputFolder);
            findMissingIcons = EditorGUILayout.Toggle("없는 아이콘을 ID로 검색", findMissingIcons);
            if (findMissingIcons) iconFolder = EditorGUILayout.TextField("아이콘 검색 폴더", iconFolder);
            replaceDescriptions = EditorGUILayout.Toggle("언어 데이터 전체 교체", replaceDescriptions);
            if (EditorGUI.EndChangeCheck()) preview = null;

            EditorGUILayout.LabelField(replaceDescriptions
                ? "언어 전체 교체: 엑셀에 없는 언어는 제거됩니다. 언어 행이 없으면 빈 배열이 됩니다."
                : "언어 병합: 엑셀에 있는 언어만 갱신하고 나머지 언어는 유지합니다.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("icon / effectDatas: 빈칸은 유지, - 는 비우기. assetName은 새 에셋에만 사용합니다.",
                EditorStyles.wordWrappedLabel);

            showEnums = EditorGUILayout.Foldout(showEnums, "프로젝트 enum 값과 효과 입력 형식", true);
            if (showEnums)
            {
                EditorGUILayout.LabelField("DataType", string.Join(", ", Enum.GetNames(typeof(DataType))), EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField("language", string.Join(", ", Enum.GetNames(typeof(language))), EditorStyles.wordWrappedLabel);
                bool effectAssets = typeof(UObject).IsAssignableFrom(typeof(ItemEffectData));
                EditorGUILayout.LabelField("effectDatas", effectAssets
                    ? "ItemEffectData 에셋 경로를 ; 로 구분합니다."
                    : "ItemEffectData가 일반 직렬화 클래스이므로 JSON 배열을 입력합니다.", EditorStyles.wordWrappedLabel);
                if (GUILayout.Button("프로젝트 enum 목록 복사 (CodeList 시트 A2에 붙여넣기)"))
                    CopyEnumList();
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("검증 및 미리보기", GUILayout.Height(30))) Validate();
                if (GUILayout.Button("엑셀 적용 (생성 / 갱신)", GUILayout.Height(30))) Import();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(status, EditorStyles.wordWrappedLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (preview != null)
            {
                foreach (string error in preview.Errors) EditorGUILayout.HelpBox(error, MessageType.Error);
                foreach (string warning in preview.Warnings) EditorGUILayout.HelpBox(warning, MessageType.Warning);
                foreach (ItemPlan item in preview.Items)
                    EditorGUILayout.LabelField((item.Existing == null ? "생성  " : "갱신  ") + item.Id,
                        item.Path, EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.EndScrollView();
        }

        private void Validate()
        {
            try
            {
                preview = BuildPlan();
                status = Summary(preview);
            }
            catch (Exception exception)
            {
                preview = new Plan();
                preview.Errors.Add(exception.Message);
                status = "검증 실패. 에셋을 변경하지 않았습니다.";
                Debug.LogException(exception);
            }
        }

        private void Import()
        {
            // Re-read both workbook and AssetDatabase so a stale preview is never applied.
            Validate();
            if (preview == null || preview.Errors.Count > 0 || preview.Items.Count == 0) return;
            try
            {
                Apply(preview);
                status = "적용 완료. " + Summary(preview);
                Debug.Log("[ItemData Excel] " + status);
            }
            catch (Exception exception)
            {
                status = "적용 중 오류가 발생해 기존 데이터 복원과 신규 에셋 삭제를 시도했습니다. Console을 확인하세요.";
                Debug.LogException(exception);
            }
        }

        private Plan BuildPlan()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Play Mode를 종료한 뒤 가져오세요.");
            if (!File.Exists(workbookPath)) throw new FileNotFoundException("엑셀 파일을 찾을 수 없습니다.", workbookPath);
            if (!string.Equals(Path.GetExtension(workbookPath), ".xlsx", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(".xlsx 파일을 선택하세요. .xls / CSV는 지원하지 않습니다.");

            string folder = ValidateAssetPath(outputFolder, false);
            Dictionary<string, ItemDataXlsxReader.Sheet> sheets = ItemDataXlsxReader.Read(workbookPath);
            var items = new Table(RequireSheet(sheets, "Items"), ItemHeaders);
            var descriptions = new Table(RequireSheet(sheets, "Descriptions"), DescriptionHeaders);
            var result = new Plan();
            Dictionary<string, List<ItemData>> existingById = IndexItems();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var disabledIds = new HashSet<string>(StringComparer.Ordinal);
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var resolver = new AssetResolver();
            List<Sprite> icons = null;

            foreach (ItemDataXlsxReader.Row row in items.DataRows)
            {
                try
                {
                    bool enabled = ReadBool(items.Value(row, "enabled"), true);
                    if (!enabled)
                    {
                        string disabledId = row.At(items.Columns["dataId"]).Text.Trim();
                        if (disabledId.Length > 0) disabledIds.Add(disabledId);
                        result.Skipped++;
                        continue;
                    }
                    items.CheckValues(row);
                    string id = items.Value(row, "dataId").Trim();
                    if (id.Length == 0) throw new InvalidDataException("dataId는 필수입니다.");
                    if (!ids.Add(id)) throw new InvalidDataException("엑셀에 같은 dataId가 중복되었습니다: " + id);
                    string explicitPath = items.Value(row, "assetPath").Trim();
                    string assetName = items.Value(row, "assetName").Trim();
                    List<ItemData> matches;
                    existingById.TryGetValue(id, out matches);
                    if (matches != null && matches.Count > 1)
                        throw new InvalidDataException("프로젝트에 같은 dataId가 중복되었습니다: " + id + "\n" +
                            string.Join("\n", matches.Select(AssetDatabase.GetAssetPath).ToArray()));
                    ItemData existing = matches == null || matches.Count == 0 ? null : matches[0];
                    string path;
                    if (explicitPath.Length > 0)
                    {
                        path = ValidateAssetPath(explicitPath, true);
                        UObject main = AssetDatabase.LoadMainAssetAtPath(path);
                        if (main != null && !(main is ItemData))
                            throw new InvalidDataException("assetPath에 ItemData가 아닌 에셋이 있습니다: " + path);
                        ItemData atPath = main as ItemData;
                        if (existing != null && existing != atPath)
                            throw new InvalidDataException("dataId의 기존 에셋과 assetPath가 다릅니다. 기존 경로: " +
                                AssetDatabase.GetAssetPath(existing));
                        if (atPath != null && !string.IsNullOrEmpty(atPath.dataId) && atPath.dataId != id)
                            throw new InvalidDataException("assetPath의 기존 dataId가 다릅니다: " + atPath.dataId);
                        existing = atPath;
                    }
                    else if (existing != null) path = ValidateAssetPath(AssetDatabase.GetAssetPath(existing), true);
                    else
                    {
                        if (assetName.Length == 0) assetName = id;
                        if (assetName.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                            assetName = assetName.Substring(0, assetName.Length - 6);
                        ValidateSegment(assetName);
                        path = ValidateAssetPath(folder + "/" + assetName + ".asset", true);
                        if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                            throw new InvalidDataException("새 에셋 생성 경로에 다른 에셋이 있습니다. assetName을 바꾸세요: " + path);
                    }
                    if (existing != null && !AssetDatabase.IsMainAsset(existing))
                        throw new InvalidDataException("ItemData 서브 에셋은 수정할 수 없습니다: " + path);
                    if (existing != null && !AssetDatabase.IsOpenForEdit(existing))
                        throw new InvalidDataException("에셋을 편집할 수 없습니다. 버전 관리 잠금을 확인하세요: " + path);
                    if (existing == null && (File.Exists(ProjectFile(path)) || AssetDatabase.AssetPathToGUID(path).Length > 0))
                        throw new InvalidDataException("생성 경로에 기존 파일이 있습니다: " + path);
                    if (!paths.Add(path)) throw new InvalidDataException("여러 행이 같은 에셋 경로를 사용합니다: " + path);

                    var item = new ItemPlan { Row = row.Number, Id = id, Path = path, Existing = existing };
                    string dataType = items.Value(row, "dataType");
                    if (string.IsNullOrWhiteSpace(dataType) && existing == null)
                        throw new InvalidDataException("새 에셋의 dataType은 필수입니다. 에디터의 프로젝트 enum 목록을 확인하세요.");
                    item.Type = ReadEnum(dataType, existing == null ? default(DataType) : existing.dataType);
                    item.Numbering = items.Value(row, "dataNumbering");
                    item.RequireUnlock = ReadBool(items.Value(row, "requireUnlock"), existing != null && existing.requireUnlock);
                    item.Grade = ReadEnum(items.Value(row, "grade"), existing == null ? ItemGrade.Common : existing.grade);
                    item.Category = ReadEnum(items.Value(row, "category"), existing == null ? ItemCategory.None : existing.category);
                    item.Series = ReadEnum(items.Value(row, "series"), existing == null ? ItemSeries.None : existing.series);
                    item.Weight = ReadFloat(items.Value(row, "weight"), existing == null ? 1f : existing.weight);
                    item.Cooldown = ReadFloat(items.Value(row, "cooldown"), existing == null ? 0.5f : existing.cooldown);

                    string icon = items.Value(row, "icon").Trim();
                    item.Icon = existing == null ? null : existing.icon;
                    if (icon == "-") item.Icon = null;
                    else if (icon.Length > 0) item.Icon = (Sprite)resolver.Find(icon, typeof(Sprite));
                    else if (item.Icon == null && findMissingIcons)
                    {
                        if (icons == null) icons = LoadIconCandidates(result);
                        string key = NormalizeKey(id);
                        Sprite[] found = icons.Where(s => NormalizeKey(s.name) == key).ToArray();
                        if (found.Length > 1)
                            throw new InvalidDataException("ID와 일치하는 아이콘이 여러 개입니다. icon에 경로#Sprite이름을 입력하세요: " + id);
                        if (found.Length == 1) item.Icon = found[0];
                        else result.Warnings.Add("Items " + row.Number + "행: ID와 일치하는 아이콘이 없습니다: " + id);
                    }
                    item.Effects = ReadEffects(items.Value(row, "effectDatas"), existing, resolver);
                    result.Items.Add(item);
                }
                catch (Exception exception) { result.Errors.Add("Items " + row.Number + "행: " + exception.Message); }
            }

            var plansById = result.Items.ToDictionary(p => p.Id, StringComparer.Ordinal);
            var languagesSeen = new HashSet<string>(StringComparer.Ordinal);
            foreach (ItemDataXlsxReader.Row row in descriptions.DataRows)
            {
                try
                {
                    string id = descriptions.Value(row, "dataId").Trim();
                    if (disabledIds.Contains(id) && !plansById.ContainsKey(id)) continue;
                    descriptions.CheckValues(row);
                    ItemPlan item;
                    if (id.Length == 0 || !plansById.TryGetValue(id, out item))
                        throw new InvalidDataException("활성 Items 행의 dataId를 입력하세요: " + id);
                    string languageText = descriptions.Value(row, "language").Trim();
                    if (languageText.Length == 0) throw new InvalidDataException("language는 필수입니다.");
                    language lan = ReadEnum(languageText, default(language));
                    string key = id + "\0" + Convert.ToInt64(lan, CultureInfo.InvariantCulture);
                    if (!languagesSeen.Add(key)) throw new InvalidDataException("같은 아이템의 language가 중복되었습니다: " + id + "/" + lan);
                    item.Descriptions.Add(new Description
                    {
                        language = lan,
                        dataName = descriptions.Value(row, "dataName"),
                        description = descriptions.Value(row, "description")
                    });
                }
                catch (Exception exception) { result.Errors.Add("Descriptions " + row.Number + "행: " + exception.Message); }
            }
            foreach (ItemPlan item in result.Items)
            {
                item.FinalDescriptions = MergeDescriptions(item, replaceDescriptions);
                if (item.FinalDescriptions.Length == 0)
                    result.Warnings.Add("Items " + item.Row + "행: 언어별 이름과 설명이 없습니다: " + item.Id);
            }
            if (result.Items.Count == 0 && result.Errors.Count == 0)
                result.Warnings.Add("가져올 활성 행이 없습니다. Items의 enabled를 TRUE로 바꾸세요.");
            return result;
        }

        private List<Sprite> LoadIconCandidates(Plan result)
        {
            string folder = iconFolder.Trim().Replace('\\', '/').TrimEnd('/');
            if (!AssetDatabase.IsValidFolder(folder))
            {
                result.Warnings.Add("아이콘 검색 폴더가 없습니다: " + folder);
                return new List<Sprite>();
            }
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
                .Concat(AssetDatabase.FindAssets("t:Sprite", new[] { folder })).Distinct().ToArray();
            return guids.Select(AssetDatabase.GUIDToAssetPath)
                .SelectMany(AssetDatabase.LoadAllAssetsAtPath).OfType<Sprite>().Distinct().ToList();
        }

        private static ItemEffectData[] ReadEffects(string text, ItemData existing, AssetResolver resolver)
        {
            text = text.Trim();
            if (text.Length == 0) return existing == null ? new ItemEffectData[0] : existing.effectDatas;
            if (text == "-" || text == "[]") return new ItemEffectData[0];
            if (typeof(UObject).IsAssignableFrom(typeof(ItemEffectData)))
            {
                string[] locators = text.Split(new[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var effects = new ItemEffectData[locators.Length];
                for (int i = 0; i < locators.Length; i++)
                    effects[i] = (ItemEffectData)(object)resolver.Find(locators[i].Trim(), typeof(ItemEffectData));
                return effects;
            }
            if (!text.StartsWith("[", StringComparison.Ordinal) || !text.EndsWith("]", StringComparison.Ordinal))
                throw new InvalidDataException("ItemEffectData가 일반 클래스입니다. effectDatas에 JSON 배열을 입력하세요.");
            var parsed = JsonUtility.FromJson<InlineEffectsJson>("{\"effectDatas\":" + text + "}");
            if (parsed == null || parsed.effectDatas == null || parsed.effectDatas.Any(e => (object)e == null))
                throw new InvalidDataException("effectDatas JSON 배열을 읽을 수 없거나 null 요소가 있습니다.");
            return parsed.effectDatas;
        }

        private static Description[] MergeDescriptions(ItemPlan item, bool replace)
        {
            var result = new List<Description>();
            var positions = new Dictionary<language, int>();
            if (!replace && item.Existing != null && item.Existing.data != null)
                foreach (Description description in item.Existing.data)
                {
                    if (description == null) continue;
                    Description copy = CopyDescription(description);
                    int index;
                    if (positions.TryGetValue(copy.language, out index)) result[index] = copy;
                    else { positions.Add(copy.language, result.Count); result.Add(copy); }
                }
            foreach (Description description in item.Descriptions)
            {
                int index;
                if (positions.TryGetValue(description.language, out index)) result[index] = CopyDescription(description);
                else { positions.Add(description.language, result.Count); result.Add(CopyDescription(description)); }
            }
            return result.ToArray();
        }

        private static Description CopyDescription(Description description)
        {
            return new Description { language = description.language, dataName = description.dataName, description = description.description };
        }

        private static void Apply(Plan plan)
        {
            if (plan.Errors.Count != 0) throw new InvalidOperationException("검증 오류를 먼저 해결하세요.");
            var createdPaths = new List<string>();
            var backups = new Dictionary<ItemData, string>();
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Import ItemData Excel");
            try
            {
                foreach (ItemPlan item in plan.Items)
                {
                    ItemData asset = item.Existing;
                    bool isNew = asset == null;
                    if (isNew)
                    {
                        EnsureFolder(item.Path.Substring(0, item.Path.LastIndexOf('/')));
                        asset = ScriptableObject.CreateInstance<ItemData>();
                        asset.name = Path.GetFileNameWithoutExtension(item.Path);
                    }
                    else
                    {
                        backups.Add(asset, EditorJsonUtility.ToJson(asset));
                        Undo.RecordObject(asset, "Import ItemData Excel");
                    }
                    asset.dataNumbering = item.Numbering;
                    asset.dataId = item.Id;
                    asset.dataType = item.Type;
                    asset.requireUnlock = item.RequireUnlock;
                    asset.grade = item.Grade;
                    asset.category = item.Category;
                    asset.series = item.Series;
                    asset.weight = item.Weight;
                    asset.cooldown = item.Cooldown;
                    asset.icon = item.Icon;
                    asset.effectDatas = item.Effects;
                    asset.data = item.FinalDescriptions;
                    InvalidateLanguageMap(asset);
                    if (isNew)
                    {
                        // Do not call DefaultData.Rebuild(): it would replace the Excel ID with asset.name.
                        createdPaths.Add(item.Path);
                        AssetDatabase.CreateAsset(asset, item.Path);
                        if (AssetDatabase.LoadMainAssetAtPath(item.Path) != asset)
                            throw new IOException("에셋 생성에 실패했습니다: " + item.Path);
                        Undo.RegisterCreatedObjectUndo(asset, "Import ItemData Excel");
                    }
                    EditorUtility.SetDirty(asset);
                }
                Undo.FlushUndoRecordObjects();
                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(undoGroup);
            }
            catch
            {
                // Runtime/I/O failures are rolled back where Unity permits. User validation errors never reach Apply.
                try { Undo.RevertAllDownToGroup(undoGroup); }
                catch (Exception rollbackError) { Debug.LogException(rollbackError); }
                foreach (KeyValuePair<ItemData, string> backup in backups)
                {
                    if (backup.Key == null) continue;
                    try
                    {
                        EditorJsonUtility.FromJsonOverwrite(backup.Value, backup.Key);
                        InvalidateLanguageMap(backup.Key);
                        EditorUtility.SetDirty(backup.Key);
                    }
                    catch (Exception rollbackError) { Debug.LogException(rollbackError); }
                }
                foreach (string path in createdPaths)
                    if (AssetDatabase.LoadMainAssetAtPath(path) != null || File.Exists(ProjectFile(path)))
                        if (!AssetDatabase.DeleteAsset(path)) Debug.LogError("신규 에셋 정리 실패: " + path);
                AssetDatabase.SaveAssets();
                throw;
            }
        }

        private static void InvalidateLanguageMap(ItemData item)
        {
            if (LanguageMapField != null) LanguageMapField.SetValue(item, null);
        }

        private static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = "Assets";
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    if (AssetDatabase.LoadMainAssetAtPath(next) != null || File.Exists(ProjectFile(next)))
                        throw new IOException("폴더 경로에 파일이 있습니다: " + next);
                    string guid = AssetDatabase.CreateFolder(current, segments[i]);
                    if (string.IsNullOrEmpty(guid)) throw new IOException("폴더 생성 실패: " + next);
                }
                current = next;
            }
        }

        private static Dictionary<string, List<ItemData>> IndexItems()
        {
            var result = new Dictionary<string, List<ItemData>>(StringComparer.Ordinal);
            var seen = new HashSet<ItemData>();
            foreach (string guid in AssetDatabase.FindAssets("t:ItemData"))
                foreach (ItemData item in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)).OfType<ItemData>())
                {
                    if (!seen.Add(item) || string.IsNullOrEmpty(item.dataId)) continue;
                    List<ItemData> values;
                    if (!result.TryGetValue(item.dataId, out values))
                    {
                        values = new List<ItemData>();
                        result.Add(item.dataId, values);
                    }
                    values.Add(item);
                }
            return result;
        }

        private static ItemDataXlsxReader.Sheet RequireSheet(Dictionary<string, ItemDataXlsxReader.Sheet> sheets, string name)
        {
            ItemDataXlsxReader.Sheet sheet;
            if (!sheets.TryGetValue(name, out sheet)) throw new InvalidDataException("필수 시트가 없습니다: " + name);
            return sheet;
        }

        private static bool ReadBool(string text, bool fallback)
        {
            text = text.Trim();
            if (text.Length == 0) return fallback;
            if (text == "1" || text.Equals("TRUE", StringComparison.OrdinalIgnoreCase)) return true;
            if (text == "0" || text.Equals("FALSE", StringComparison.OrdinalIgnoreCase)) return false;
            throw new InvalidDataException("논리값은 TRUE / FALSE 또는 1 / 0을 입력하세요: " + text);
        }

        private static float ReadFloat(string text, float fallback)
        {
            text = text.Trim();
            if (text.Length == 0) return fallback;
            float value;
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || float.IsNaN(value) || float.IsInfinity(value))
                throw new InvalidDataException("유한한 숫자를 입력하세요. 소수점은 . 을 사용합니다: " + text);
            return value;
        }

        private static T ReadEnum<T>(string text, T fallback) where T : struct
        {
            text = text.Trim();
            if (text.Length == 0) return fallback;
            T value;
            if (!typeof(T).IsEnum || !Enum.TryParse<T>(text, true, out value) || !Enum.IsDefined(typeof(T), value))
                throw new InvalidDataException(typeof(T).Name + "에 없는 값입니다: " + text + " (사용 가능: " +
                    string.Join(", ", Enum.GetNames(typeof(T))) + ")");
            return value;
        }

        private static string ValidateAssetPath(string text, bool file)
        {
            string path = text.Trim().Replace('\\', '/').TrimEnd('/');
            if (path != "Assets" && !path.StartsWith("Assets/", StringComparison.Ordinal))
                throw new InvalidDataException("에셋 생성·수정 경로는 Assets 아래여야 합니다: " + path);
            foreach (string segment in path.Split('/')) ValidateSegment(segment);
            if (file && !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("assetPath는 .asset으로 끝나야 합니다: " + path);
            return path;
        }

        private static void ValidateSegment(string segment)
        {
            if (segment.Length == 0 || segment == "." || segment == ".." || segment.EndsWith(".", StringComparison.Ordinal) ||
                segment.EndsWith(" ", StringComparison.Ordinal) || Regex.IsMatch(segment, "[<>:\"|?*\\\\/\\x00-\\x1F]"))
                throw new InvalidDataException("파일명·폴더명에 사용할 수 없는 값입니다: " + segment);
            string stem = segment.Split('.')[0];
            if (Regex.IsMatch(stem, "^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])$", RegexOptions.IgnoreCase))
                throw new InvalidDataException("예약된 파일명입니다: " + segment);
        }

        private static string ProjectFile(string path)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, path);
        }

        private static string NormalizeKey(string value)
        {
            return Regex.Replace(value.Trim().ToLowerInvariant(), @"[\s_\-]+", "");
        }

        private static string Summary(Plan plan)
        {
            int created = plan.Items.Count(i => i.Existing == null);
            return "생성 " + created + " / 갱신 " + (plan.Items.Count - created) + " / 제외 " + plan.Skipped +
                " / 오류 " + plan.Errors.Count + " / 경고 " + plan.Warnings.Count;
        }

        private static void CopyEnumList()
        {
            var text = new StringBuilder();
            foreach (Type type in new[] { typeof(ItemGrade), typeof(ItemCategory), typeof(ItemSeries), typeof(DataType), typeof(language) })
                foreach (string name in Enum.GetNames(type))
                    text.Append(type.Name).Append('\t').Append(name).Append('\t')
                        .Append(Convert.ToInt64(Enum.Parse(type, name), CultureInfo.InvariantCulture)).Append('\t').AppendLine();
            EditorGUIUtility.systemCopyBuffer = text.ToString();
        }

        [Serializable]
        private sealed class InlineEffectsJson { public ItemEffectData[] effectDatas = null; }

        private sealed class Plan
        {
            public readonly List<ItemPlan> Items = new List<ItemPlan>();
            public readonly List<string> Errors = new List<string>();
            public readonly List<string> Warnings = new List<string>();
            public int Skipped;
        }

        private sealed class ItemPlan
        {
            public int Row;
            public string Id, Path, Numbering;
            public ItemData Existing;
            public DataType Type;
            public bool RequireUnlock;
            public ItemGrade Grade;
            public ItemCategory Category;
            public ItemSeries Series;
            public float Weight, Cooldown;
            public Sprite Icon;
            public ItemEffectData[] Effects;
            public Description[] FinalDescriptions;
            public readonly List<Description> Descriptions = new List<Description>();
        }

        private sealed class Table
        {
            public readonly Dictionary<string, int> Columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            public readonly List<ItemDataXlsxReader.Row> DataRows;
            private readonly string name;

            public Table(ItemDataXlsxReader.Sheet sheet, string[] required)
            {
                name = sheet.Name;
                ItemDataXlsxReader.Row header = sheet.Rows.FirstOrDefault(r => r.Number == 1);
                if (header == null) throw new InvalidDataException(name + ": 1행에 헤더가 필요합니다.");
                foreach (KeyValuePair<int, ItemDataXlsxReader.Cell> cell in header.Cells)
                {
                    string text = cell.Value.Text.Trim();
                    if (text.Length == 0) continue;
                    if (cell.Value.HasFormula || cell.Value.IsError || Columns.ContainsKey(text))
                        throw new InvalidDataException(name + ": 헤더가 올바르지 않거나 중복되었습니다: " + text);
                    Columns.Add(text, cell.Key);
                }
                foreach (string key in required)
                    if (!Columns.ContainsKey(key)) throw new InvalidDataException(name + ": 필수 헤더가 없습니다: " + key);
                DataRows = sheet.Rows.Where(r => r.Number > 1 && !r.IsEmpty && !r.At(0).Text.TrimStart().StartsWith("#", StringComparison.Ordinal)).ToList();
            }

            public string Value(ItemDataXlsxReader.Row row, string key)
            {
                ItemDataXlsxReader.Cell cell = row.At(Columns[key]);
                if (cell.HasFormula) throw new InvalidDataException(key + ": 수식을 값으로 붙여넣은 뒤 가져오세요.");
                if (cell.IsError) throw new InvalidDataException(key + ": 엑셀 오류 값입니다: " + cell.Text);
                return cell.Text;
            }

            public void CheckValues(ItemDataXlsxReader.Row row)
            {
                foreach (string key in Columns.Keys) Value(row, key);
            }
        }

        private sealed class AssetResolver
        {
            private readonly Dictionary<string, UObject> cache = new Dictionary<string, UObject>(StringComparer.Ordinal);

            public UObject Find(string locator, Type type)
            {
                string key = type.FullName + "\0" + locator;
                UObject cached;
                if (cache.TryGetValue(key, out cached)) return cached;
                int hash = locator.LastIndexOf('#');
                string path = hash < 0 ? locator : locator.Substring(0, hash);
                string subName = hash < 0 ? "" : locator.Substring(hash + 1);
                if (hash >= 0 && subName.Length == 0) throw new InvalidDataException("# 뒤에 서브 에셋 이름이 필요합니다: " + locator);
                if (path.StartsWith("guid:", StringComparison.OrdinalIgnoreCase))
                    path = AssetDatabase.GUIDToAssetPath(path.Substring(5));
                path = path.Replace('\\', '/');
                if (!path.StartsWith("Assets/", StringComparison.Ordinal) && !path.StartsWith("Packages/", StringComparison.Ordinal))
                    throw new InvalidDataException("에셋 경로 또는 guid:GUID를 입력하세요: " + locator);
                UObject[] matches = AssetDatabase.LoadAllAssetsAtPath(path)
                    .Where(a => type.IsInstanceOfType(a) && (subName.Length == 0 || a.name == subName)).ToArray();
                if (matches.Length == 0) throw new InvalidDataException(type.Name + " 에셋을 찾을 수 없습니다: " + locator);
                if (matches.Length > 1) throw new InvalidDataException("에셋이 여러 개입니다. 경로#서브에셋이름을 입력하세요: " + locator);
                cache.Add(key, matches[0]);
                return matches[0];
            }
        }
    }
}
#endif
