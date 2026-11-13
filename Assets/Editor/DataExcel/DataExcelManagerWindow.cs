using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public interface IDataExcelModule : IDisposable
{
    string Name { get; }
    void DrawGUI();
}

public sealed class ItemExcelManager : IDataExcelModule
{
    private readonly ItemDataExcelTools.ItemDataExcelImporter importer = ScriptableObject.CreateInstance<ItemDataExcelTools.ItemDataExcelImporter>();
    public string Name => "Items";
    public void DrawGUI() => importer.DrawUI();
    public void Dispose() => UnityEngine.Object.DestroyImmediate(importer);
}

// Adding another IDataExcelModule registers another data type without modifying this window.
public sealed class DataExcelManagerWindow : EditorWindow
{
    private IDataExcelModule[] modules;
    private int selected;
    [MenuItem("Tools/Data/Excel Manager")]
    public static void Open() => GetWindow<DataExcelManagerWindow>("Data Excel Manager");
    private void OnEnable()
    {
        modules = TypeCache.GetTypesDerivedFrom<IDataExcelModule>()
            .Where(type => !type.IsAbstract && !type.IsInterface && type.GetConstructor(Type.EmptyTypes) != null)
            .OrderBy(type => type.Name).Select(type => (IDataExcelModule)Activator.CreateInstance(type)).ToArray();
    }
    private void OnDisable() { if (modules != null) foreach (var module in modules) module.Dispose(); }
    private void OnGUI()
    {
        if (modules == null || modules.Length == 0) return;
        selected = Mathf.Clamp(GUILayout.Toolbar(selected, modules.Select(module => module.Name).ToArray()), 0, modules.Length - 1);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            try { modules[selected].DrawGUI(); }
            catch (Exception error) { EditorGUILayout.HelpBox(error.Message, MessageType.Error); }
        }
    }
}
