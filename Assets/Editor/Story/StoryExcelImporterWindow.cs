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
    private bool showSettings;

    public static void Open() => DataExcelManagerWindow.Open();

    private void OnGUI() => DrawUI();

    public void DrawUI()
    {
        DataExcelGUI.Workbook(ref workbook, "스토리 엑셀 선택");
        if (DataExcelGUI.AdditionalSettings(ref showSettings))
        {
        outputFolder = EditorGUILayout.TextField("출력 폴더", outputFolder);
        EditorGUILayout.LabelField("Sheets: english, korean. Sprite order: natural filename order.", EditorStyles.wordWrappedLabel);
        }
        DataExcelGUI.Actions(() => Run(false), () => Run(true));
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
