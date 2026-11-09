using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class UpgradeProgressCatalogBuilder : IPreprocessBuildWithReport
{
    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode) Rebuild();
        };
    }
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report) => Rebuild();
    [MenuItem("Tools/Save Data/Rebuild Upgrade Asset Catalog")]
    public static void Rebuild()
    {
        const string path = "Assets/Resources/UpgradeProgressCatalog.asset";
        var catalog = AssetDatabase.LoadAssetAtPath<UpgradeProgressCatalog>(path);
        if (catalog == null) { catalog = ScriptableObject.CreateInstance<UpgradeProgressCatalog>(); AssetDatabase.CreateAsset(catalog, path); }
        catalog.entries.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:ItemData").Concat(AssetDatabase.FindAssets("t:SkillNodeData")).Distinct())
            catalog.entries.Add(new UpgradeProgressCatalog.Entry { key = guid, asset = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid)) });
        EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
    }
}
