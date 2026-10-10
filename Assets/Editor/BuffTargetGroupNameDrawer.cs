using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// IBuffTarget 선언, 씬 인스턴스, 프로젝트 프리팹의 그룹을 같은 목록으로 제공한다.
[InitializeOnLoad]
public static class BuffTargetGroupCatalog
{
    private static string[] cached, declaredGroups, prefabGroups;
    private static double lastRefresh;

    static BuffTargetGroupCatalog()
    {
        EditorApplication.hierarchyChanged += InvalidateScene;
        EditorApplication.projectChanged += Invalidate;
        EditorApplication.playModeStateChanged += state => InvalidateScene();
        Undo.undoRedoPerformed += Invalidate;
    }

    private static void InvalidateScene() => cached = null;
    public static void Invalidate()
    {
        cached = null;
        declaredGroups = null;
        prefabGroups = null;
    }

    public static string[] Groups
    {
        get
        {
            if (cached != null && EditorApplication.timeSinceStartup - lastRefresh < 1d) return cached;
            var groups = new SortedSet<string>(StringComparer.Ordinal);
            if (declaredGroups == null)
            {
                var declarations = new SortedSet<string>(StringComparer.Ordinal);
                var runtimeAssemblies = new HashSet<string>(StringComparer.Ordinal);
                foreach (UnityEditor.Compilation.Assembly assembly in
                    UnityEditor.Compilation.CompilationPipeline.GetAssemblies(UnityEditor.Compilation.AssembliesType.Player))
                    runtimeAssemblies.Add(assembly.name);
                foreach (Type type in TypeCache.GetTypesDerivedFrom<IBuffTarget>())
                {
                    // Editor/테스트 전용 구현의 임시 그룹은 프로젝트 선택 목록에 넣지 않는다.
                    if (!runtimeAssemblies.Contains(type.Assembly.GetName().Name)) continue;
                    foreach (string group in BuffTargetGroupDiscovery.GetDeclaredGroups(type))
                        BuffTargetGroupDiscovery.AddGroup(declarations, group);
                }
                declaredGroups = ToArray(declarations);
            }
            foreach (string group in declaredGroups) BuffTargetGroupDiscovery.AddGroup(groups, group);

            // 프리팹은 최초 조회/프로젝트 변경 때만 읽는다. 씬에 생성하지 않는다.
            if (prefabGroups == null)
            {
                var prefabs = new SortedSet<string>(StringComparer.Ordinal);
                foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                    if (prefab == null) continue;
                    foreach (MonoBehaviour component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                        AddTarget(prefabs, component);
                }
                prefabGroups = ToArray(prefabs);
            }
            foreach (string group in prefabGroups) BuffTargetGroupDiscovery.AddGroup(groups, group);

            // 비활성 씬 대상과 이미 로드된 프리팹의 아직 저장하지 않은 수정도 반영한다.
            foreach (MonoBehaviour component in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
            {
                if (component == null) continue;
                if (EditorUtility.IsPersistent(component))
                {
                    if (!PrefabUtility.IsPartOfPrefabAsset(component)) continue;
                }
                else if (!component.gameObject.scene.IsValid() || !component.gameObject.scene.isLoaded ||
                    UnityEditor.SceneManagement.EditorSceneManager.IsPreviewSceneObject(component.gameObject))
                    continue;
                AddTarget(groups, component);
            }
            cached = ToArray(groups);
            lastRefresh = EditorApplication.timeSinceStartup;
            return cached;
        }
    }

    private static void AddTarget(ISet<string> groups, MonoBehaviour component)
    {
        if (!(component is IBuffTarget target)) return;
        // 동적 getter가 아직 초기화되지 않은 대상 하나 때문에 전체 목록을 잃지 않는다.
        try { BuffTargetGroupDiscovery.AddGroup(groups, target.BuffTargetGroup); }
        catch (Exception) { }
    }

    private static string[] ToArray(SortedSet<string> groups)
    {
        var result = new string[groups.Count];
        groups.CopyTo(result);
        return result;
    }
}

[CustomPropertyDrawer(typeof(BuffTargetGroupNameAttribute))]
public sealed class BuffTargetGroupNameDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "문자열 그룹 필드에 사용하세요.");
            return;
        }

        EditorGUI.BeginProperty(position, label, property);
        bool previousMixed = EditorGUI.showMixedValue;
        try
        {
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            Rect textRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginChangeCheck();
            string value = EditorGUI.TextField(textRect, label, property.stringValue);
            if (EditorGUI.EndChangeCheck()) property.stringValue = value;

            string[] groups = BuffTargetGroupCatalog.Groups;
            GUIContent[] options = new GUIContent[groups.Length + 2];
            options[0] = new GUIContent("(직접 입력 값 유지)");
            options[1] = new GUIContent(((BuffTargetGroupNameAttribute)attribute).EmptyLabel);
            string normalized = BuffTargetHandle.NormalizeGroupName(property.stringValue);
            int selected = string.IsNullOrEmpty(normalized) ? 1 : 0;
            for (int i = 0; i < groups.Length; i++)
            {
                options[i + 2] = new GUIContent(groups[i]);
                if (groups[i] == normalized) selected = i + 2;
            }

            Rect popupRect = new Rect(position.x, textRect.yMax + EditorGUIUtility.standardVerticalSpacing,
                position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginChangeCheck();
            int choice = EditorGUI.Popup(popupRect, new GUIContent("자동 선택"), selected, options);
            if (EditorGUI.EndChangeCheck() && choice > 0)
                property.stringValue = choice == 1 ? string.Empty : groups[choice - 2];
        }
        finally
        {
            EditorGUI.showMixedValue = previousMixed;
            EditorGUI.EndProperty();
        }
    }
}
