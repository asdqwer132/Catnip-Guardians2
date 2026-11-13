using System;
using UnityEditor;
using UnityEngine;

public static class DataExcelGUI
{
    public static bool Workbook(ref string path, string title)
    {
        string before = path;
        using (new EditorGUILayout.HorizontalScope())
        {
            path = EditorGUILayout.TextField("엑셀 파일 (.xlsx)", path);
            if (GUILayout.Button("선택", GUILayout.Width(60)))
            {
                string chosen = EditorUtility.OpenFilePanel(title, Application.dataPath, "xlsx");
                if (!string.IsNullOrEmpty(chosen)) { path = chosen; GUI.changed = true; }
            }
        }
        return before != path;
    }
    public static bool AdditionalSettings(ref bool expanded)
    {
        expanded = EditorGUILayout.Foldout(expanded, "추가 설정", true);
        return expanded;
    }

    public static void Actions(Action validate, Action apply)
    {
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("검증 / 미리보기", GUILayout.Height(30))) validate();
            if (GUILayout.Button("생성 / 갱신", GUILayout.Height(30))) apply();
        }
    }
}
