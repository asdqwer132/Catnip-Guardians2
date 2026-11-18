using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class DataExcelManagerWindow : EditorWindow
{
    [Serializable]
    private sealed class WorkbookPanel
    {
        public string file;
        public List<string> disabledSheets = new List<string>();
        [NonSerialized] public List<DataWorkbookImport.Binding> bindings;
        [NonSerialized] public DataWorkbookImport.Plan preview;
        [NonSerialized] public string message;
        [NonSerialized] public string loadedFile;
        [NonSerialized] public long loadedTicks;
    }

    [SerializeField] private WorkbookPanel identify = new WorkbookPanel { file = "Assets/Data/Excel/BasicDataIdentify.xlsx" };
    [SerializeField] private WorkbookPanel displayer = new WorkbookPanel { file = "Assets/Data/Excel/BasicDataDisplayer.xlsx" };
    [SerializeField] private WorkbookPanel extra = new WorkbookPanel { file = "Assets/Data/Excel/ExtraData.xlsx" };
    [SerializeField] private int selected;
    private Vector2 scroll;
    private StoryExcelImporterWindow story;
    private static readonly string[] Tabs = { "아이덴티파이", "디스플레이", "엑스트라", "스토리" };

    [MenuItem("Tools/Data/Excel Manager")]
    public static void Open() => GetWindow<DataExcelManagerWindow>("Data Excel Manager");
    private void OnDisable() { if (story != null) DestroyImmediate(story); }
    private void OnGUI()
    {
        selected = GUILayout.Toolbar(Mathf.Clamp(selected, 0, Tabs.Length - 1), Tabs);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (selected == 3)
            {
                if (story == null) story = CreateInstance<StoryExcelImporterWindow>();
                story.DrawUI();
                return;
            }
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawWorkbook(selected == 0 ? identify : selected == 1 ? displayer : extra, (DataWorkbookKind)selected);
            EditorGUILayout.EndScrollView();
        }
    }

    private static void DrawWorkbook(WorkbookPanel panel, DataWorkbookKind kind)
    {
        if (DataExcelGUI.Workbook(ref panel.file, Tabs[(int)kind] + " 엑셀 선택")) panel.loadedFile = null;
        long ticks = File.Exists(panel.file) ? File.GetLastWriteTimeUtc(panel.file).Ticks : 0;
        if (panel.loadedFile != panel.file || panel.loadedTicks != ticks)
        {
            panel.loadedFile = panel.file; panel.loadedTicks = ticks; panel.preview = null;
            try { panel.bindings = DataWorkbookImport.ReadBindings(panel.file); panel.message = null; }
            catch (Exception error) { panel.bindings = null; panel.message = error.Message; }
        }
        EditorGUILayout.LabelField(kind == DataWorkbookKind.Identify
            ? "ID로 하위 폴더를 검색합니다. 없으면 RootPath/Generated에 생성합니다."
            : "ID가 일치하는 기존 데이터만 갱신합니다. 없는 ID는 생성하지 않습니다.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("빈칸 / -: 기존 값 유지 · Null: 비우기 · 배열: ; 또는 JSON · 에셋: 경로 / ID", EditorStyles.wordWrappedLabel);
        if (kind == DataWorkbookKind.Extra)
            EditorGUILayout.LabelField("상자 ItemBoxData: costs · itemSeries · grade · openClip · gachaItems(JSON: itemData, probability, amounts[count/weight])", EditorStyles.wordWrappedMiniLabel);
        if (panel.bindings != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("적용할 시트 (Code 기준)", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("전체 선택")) { panel.disabledSheets.Clear(); panel.preview = null; }
                if (GUILayout.Button("전체 해제")) { panel.disabledSheets = panel.bindings.Select(binding => binding.Sheet).ToList(); panel.preview = null; }
            }
            foreach (var binding in panel.bindings)
            {
                bool enabled = !panel.disabledSheets.Contains(binding.Sheet);
                bool next = EditorGUILayout.ToggleLeft(binding.Sheet + "  /  " + binding.TypeName, enabled);
                if (next != enabled)
                {
                    if (next) panel.disabledSheets.Remove(binding.Sheet); else panel.disabledSheets.Add(binding.Sheet);
                    panel.preview = null;
                }
                using (new EditorGUI.IndentLevelScope()) EditorGUILayout.LabelField(binding.RootPath, EditorStyles.wordWrappedMiniLabel);
            }
        }
        using (new EditorGUI.DisabledScope(panel.bindings == null || panel.bindings.All(binding => panel.disabledSheets.Contains(binding.Sheet))))
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("검증 / 미리보기", GUILayout.Height(30))) Run(panel, kind, false);
            if (GUILayout.Button(kind == DataWorkbookKind.Identify ? "생성 / 갱신" : "갱신", GUILayout.Height(30))) Run(panel, kind, true);
        }
        if (!string.IsNullOrEmpty(panel.message)) EditorGUILayout.LabelField(panel.message, EditorStyles.wordWrappedLabel);
        if (panel.preview == null) return;
        foreach (string error in panel.preview.Errors) EditorGUILayout.HelpBox(error, MessageType.Error);
        foreach (string warning in panel.preview.Warnings) EditorGUILayout.HelpBox(warning, MessageType.Warning);
        foreach (var entry in panel.preview.Entries)
            EditorGUILayout.LabelField((entry.Existing == null ? "생성  " : "갱신  ") + entry.Id + "\n" + entry.Path, EditorStyles.wordWrappedMiniLabel);
    }

    private static void Run(WorkbookPanel panel, DataWorkbookKind kind, bool apply)
    {
        try
        {
            // Never apply an old preview: re-read both the workbook and the current asset IDs.
            panel.preview = DataWorkbookImport.Read(panel.file, kind, panel.bindings.Where(binding => !panel.disabledSheets.Contains(binding.Sheet)).Select(binding => binding.Sheet));
            if (apply && panel.preview.Errors.Count == 0) DataWorkbookImport.Apply(panel.preview);
            panel.message = (apply && panel.preview.Errors.Count == 0 ? "적용 완료: " : "검증 결과: ") + panel.preview.Summary;
            if (panel.preview.Errors.Count > 0) Debug.LogError("[DataExcel][" + kind + "]\n" + string.Join("\n", panel.preview.Errors));
        }
        catch (Exception error) { panel.message = error.Message; Debug.LogException(error); }
    }
}
