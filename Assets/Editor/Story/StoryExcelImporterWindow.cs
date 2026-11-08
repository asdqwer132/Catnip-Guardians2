using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class StoryExcelImporterWindow : EditorWindow
{
    [SerializeField] private string workbook = "Assets/Data/Excel/StoryTest.xlsx";
    [SerializeField] private string outputFolder = "Assets/Data/Scriptable/Story/ExcelTest";
    private List<StoryExcelImporter.Entry> preview;
    private string status;
    private Vector2 scroll;

    [MenuItem("Tools/Story/Story Excel Importer")]
    public static void Open() => GetWindow<StoryExcelImporterWindow>("Story Excel");

    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            workbook = EditorGUILayout.TextField("Excel (.xlsx)", workbook);
            if (GUILayout.Button("Browse", GUILayout.Width(65)))
            {
                string selected = EditorUtility.OpenFilePanel("Story Excel", Application.dataPath, "xlsx");
                if (!string.IsNullOrEmpty(selected)) workbook = selected;
            }
        }
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        EditorGUILayout.LabelField("Sheets: english, korean. Sprite order: natural filename order.", EditorStyles.wordWrappedLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Validate / Preview")) Run(false);
            if (GUILayout.Button("Create / Update")) Run(true);
        }
        scroll = EditorGUILayout.BeginScrollView(scroll);
        if (!string.IsNullOrEmpty(status)) EditorGUILayout.LabelField(status, EditorStyles.wordWrappedLabel);
        if (preview != null)
            foreach (var entry in preview)
                EditorGUILayout.LabelField($"{entry.assetPath}\n{entry.sprites.Length} images / {entry.dialogues.Length} dialogues", EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndScrollView();
    }

    private void Run(bool apply)
    {
        preview = null;
        try
        {
            if (apply) status = StoryExcelImporter.Import(workbook, outputFolder) + " stories created / updated.";
            preview = StoryExcelImporter.Read(workbook, outputFolder);
            if (!apply) status = preview.Count + " stories validated. No assets changed.";
        }
        catch (Exception exception) { status = exception.Message; }
    }
}
