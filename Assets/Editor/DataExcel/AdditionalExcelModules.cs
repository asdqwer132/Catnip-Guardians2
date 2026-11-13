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
    public string Name => "Enemy Patterns";
    public void DrawGUI()
    {
        EditorGUILayout.LabelField("Enemy action patterns / Excel (.xlsx)", EditorStyles.boldLabel);
        if (GUILayout.Button("Choose Excel and import enemy patterns")) EnemyPatternActionExcelImporter.ImportExcel();
    }
    public void Dispose() { }
}
