using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class SaveDataViewerWindow : EditorWindow
{
    private readonly string[] tabs = { "JSON Saves", "PlayerPrefs" };
    private List<string> files = new List<string>();
    private List<SavePreferenceSnapshot> preferences = new List<SavePreferenceSnapshot>();
    private SaveJsonSnapshot json;
    private SavePreferenceSnapshot customPreference;
    private string listingError = "";
    [SerializeField] private int tab;
    [SerializeField] private int selectedFile;
    [SerializeField] private string customKey = "";
    [SerializeField] private SavePreferenceKind customKind;
    private Vector2 scroll;

    [MenuItem("Tools/Save Data Viewer")]
    public static void Open()
    {
        var window = GetWindow<SaveDataViewerWindow>("Save Data Viewer");
        window.minSize = new Vector2(620f, 450f);
        window.Show();
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        RefreshData();
    }
    private void OnDisable() => EditorApplication.playModeStateChanged -= OnPlayModeChanged;
    private void OnFocus() => RefreshData();
    private void OnPlayModeChanged(PlayModeStateChange _) => RefreshData();

    internal void RefreshData()
    {
        string previous = selectedFile >= 0 && selectedFile < files.Count ? files[selectedFile] : null;
        files = SaveDataInspection.ListJsonFiles(Application.persistentDataPath, out listingError);
        int index = previous != null ? files.IndexOf(previous) : selectedFile;
        selectedFile = Mathf.Clamp(index, 0, Mathf.Max(0, files.Count - 1));
        json = files.Count > 0 ? SaveDataInspection.ReadJson(files[selectedFile]) : null;
        preferences = SaveDataInspection.ReadPreferences();
        if (!string.IsNullOrWhiteSpace(customKey)) customPreference = SaveDataInspection.ReadCustomPreference(customKey, customKind);
        Repaint();
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            tab = GUILayout.Toolbar(tab, tabs, EditorStyles.toolbarButton);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(75f))) RefreshData();
        }
        EditorGUILayout.LabelField(Application.companyName + " / " + Application.productName, EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        if (tab == 0) DrawJson();
        else DrawPreferences();
        EditorGUILayout.EndScrollView();
    }

    private void DrawJson()
    {
        EditorGUILayout.LabelField("Save directory");
        Selectable(Application.persistentDataPath, 38f);
        if (!string.IsNullOrEmpty(listingError)) EditorGUILayout.HelpBox(listingError, MessageType.Warning);
        if (files.Count == 0) return;
        var labels = files.ConvertAll(path => SaveDataInspection.JsonLabel(path) + (File.Exists(path) ? "" : " (not saved)"));
        int next = EditorGUILayout.Popup("File", selectedFile, labels.ToArray());
        if (next != selectedFile)
        {
            selectedFile = next;
            json = SaveDataInspection.ReadJson(files[selectedFile]);
        }
        if (json == null) return;
        Selectable(json.Path, 38f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Copy path")) EditorGUIUtility.systemCopyBuffer = json.Path;
            using (new EditorGUI.DisabledScope(!Directory.Exists(Application.persistentDataPath)))
                if (GUILayout.Button("Show in folder")) EditorUtility.RevealInFinder(json.Exists ? json.Path : Application.persistentDataPath);
        }
        if (!json.Exists)
        {
            EditorGUILayout.LabelField("No saved file yet.");
            return;
        }
        EditorGUILayout.LabelField("Size / modified", json.Bytes + " bytes / " + json.LastWriteUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
        if (!string.IsNullOrEmpty(json.Error)) EditorGUILayout.HelpBox(json.Error, MessageType.Warning);
        if (!string.IsNullOrEmpty(json.Summary))
        {
            EditorGUILayout.LabelField("Contents", EditorStyles.boldLabel);
            Selectable(json.Summary);
        }
        EditorGUILayout.LabelField("Raw JSON", EditorStyles.boldLabel);
        if (GUILayout.Button("Copy JSON")) EditorGUIUtility.systemCopyBuffer = json.Json;
        Selectable(json.Json);
    }

    private void DrawPreferences()
    {
        EditorGUILayout.LabelField("PlayerPrefs for this Unity Editor project. Refresh after changing settings.", EditorStyles.wordWrappedLabel);
        string group = null;
        foreach (var pref in preferences)
        {
            if (group != pref.Group)
            {
                group = pref.Group;
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(group, EditorStyles.boldLabel);
            }
            DrawPreference(pref);
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Additional key lookup", EditorStyles.boldLabel);
        customKey = EditorGUILayout.TextField("Key", customKey);
        customKind = (SavePreferenceKind)EditorGUILayout.EnumPopup("Stored type", customKind);
        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(customKey)))
            if (GUILayout.Button("Read key")) customPreference = SaveDataInspection.ReadCustomPreference(customKey, customKind);
        if (customPreference != null) DrawPreference(customPreference);
    }

    private static void DrawPreference(SavePreferenceSnapshot pref)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(new GUIContent(pref.Label, "Default: " + pref.DefaultValue), new GUIContent(pref.Value));
            EditorGUILayout.LabelField("Type / state", pref.Kind + " / " + (pref.HasKey ? "Stored" : "Not set (default)"), EditorStyles.miniLabel);
            EditorGUILayout.SelectableLabel(pref.Key, EditorStyles.miniLabel, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }
    }

    private static void Selectable(string value, float height = 0f)
    {
        float desired = height > 0f ? height : Mathf.Max(60f, EditorStyles.textArea.CalcHeight(new GUIContent(value), Mathf.Max(200f, EditorGUIUtility.currentViewWidth - 45f)));
        EditorGUILayout.SelectableLabel(value ?? "", EditorStyles.textArea, GUILayout.Height(desired));
    }
}
