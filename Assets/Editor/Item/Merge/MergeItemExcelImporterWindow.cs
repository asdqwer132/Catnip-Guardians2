using UnityEditor;
using UnityEngine;

public sealed class MergeItemExcelImporterWindow : EditorWindow
{
    [SerializeField] private string workbook = "";
    [SerializeField] private string sheet = "MergeItems";
    [SerializeField] private DefaultAsset itemFolder, prefabFolder, outputFolder;
    [SerializeField] private string newOutputFolder = "Assets/Data/MergeItems";
    private MergeItemExcelPlan preview;
    private Vector2 scroll;
    private string status;

    [MenuItem("Tools/Item/Merge/Merge Item Excel Importer")]
    public static void Open() => GetWindow<MergeItemExcelImporterWindow>("MergeItem Excel");

    private string Folder(DefaultAsset asset) => asset != null ? AssetDatabase.GetAssetPath(asset) : "Assets";
    private MergeItemExcelPlan Read() => MergeItemExcelImport.Read(workbook, sheet, Folder(itemFolder), Folder(prefabFolder),
        outputFolder != null ? Folder(outputFolder) : newOutputFolder);

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("필수 컬럼: itemData, prefab, radius  /  선택 컬럼: enabled, assetName, assetPath\nitemData는 에셋 경로 · GUID · dataId · 이름, prefab은 경로 · GUID · 이름을 사용합니다.", MessageType.Info);
        using (new EditorGUILayout.HorizontalScope())
        {
            workbook = EditorGUILayout.TextField("Excel (.xlsx)", workbook);
            if (GUILayout.Button("선택", GUILayout.Width(55))) workbook = EditorUtility.OpenFilePanel("머지 아이템 엑셀", Application.dataPath, "xlsx");
        }
        sheet = EditorGUILayout.TextField("시트", sheet);
        itemFolder = (DefaultAsset)EditorGUILayout.ObjectField("ItemData 검색 폴더", itemFolder, typeof(DefaultAsset), false);
        prefabFolder = (DefaultAsset)EditorGUILayout.ObjectField("프리팹 검색 폴더", prefabFolder, typeof(DefaultAsset), false);
        outputFolder = (DefaultAsset)EditorGUILayout.ObjectField("MergeItem 출력 폴더", outputFolder, typeof(DefaultAsset), false);
        if (outputFolder == null) newOutputFolder = EditorGUILayout.TextField("새 출력 폴더", newOutputFolder);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("검증 / 미리보기")) { preview = Read(); status = null; }
            if (GUILayout.Button("생성 / 갱신"))
            {
                preview = Read(); // 파일과 에셋이 바뀌었을 수 있으므로 적용 직전 다시 검증합니다.
                if (preview.errors.Count == 0)
                    try { MergeItemExcelImport.Apply(preview); status = preview.entries.Count + "개 생성 / 갱신 완료"; }
                    catch (System.Exception exception) { status = exception.Message; }
            }
        }
        scroll = EditorGUILayout.BeginScrollView(scroll);
        if (!string.IsNullOrEmpty(status)) EditorGUILayout.LabelField(status, EditorStyles.wordWrappedLabel);
        if (preview != null)
        {
            foreach (string error in preview.errors) EditorGUILayout.HelpBox(error, MessageType.Error);
            foreach (var entry in preview.entries)
                EditorGUILayout.LabelField($"{entry.row}행 · {(entry.existing != null ? "갱신" : "생성")} · {entry.item.name} · R {entry.radius}\n{entry.path}", EditorStyles.wordWrappedLabel);
        }
        EditorGUILayout.EndScrollView();
    }
}
