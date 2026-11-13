using UnityEditor;
using UnityEngine;

// Modules preserve each importer's file schema and validation, while sharing one manager UI.
public abstract class ExcelWindowModule<T> : IDataExcelModule where T : EditorWindow
{
    private T window;
    protected T Window => window != null ? window : (window = ScriptableObject.CreateInstance<T>());
    public abstract string Name { get; }
    public abstract void DrawGUI();
    public void Dispose() { if (window != null) Object.DestroyImmediate(window); }
}
public sealed class StoryExcelManager : ExcelWindowModule<StoryExcelImporterWindow>
{
    public override string Name => "Story";
    public override void DrawGUI() => Window.DrawUI();
}
public sealed class MergeItemExcelManager : ExcelWindowModule<MergeItemExcelImporterWindow>
{
    public override string Name => "Merge Items";
    public override void DrawGUI() => Window.DrawUI();
}
public sealed class EnemyPatternExcelManager : IDataExcelModule
{
    private string workbook = "Assets/Data/Excel/EnemyPatternsExample.xlsx", status = "";
    private string outputFolder = "Assets/Items/EnemyPatterns";
    private bool showSettings;
    public string Name => "Enemy Patterns";
    public void DrawGUI()
    {
        DataExcelGUI.Workbook(ref workbook, "적 패턴 엑셀 선택");
        if (DataExcelGUI.AdditionalSettings(ref showSettings)) outputFolder = EditorGUILayout.TextField("출력 폴더", outputFolder);
        DataExcelGUI.Actions(
            () => status = (EnemyPatternActionExcelImporter.ReadWorkbookRows(workbook).Count - 1) + "개 패턴 검증 완료",
            () => { EnemyPatternActionExcelImporter.ImportWorkbook(workbook, outputFolder); status = "적 패턴 적용 완료"; });
        EditorGUILayout.LabelField(status, EditorStyles.wordWrappedLabel);
    }
    public void Dispose() { }
}
