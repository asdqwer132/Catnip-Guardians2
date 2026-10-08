using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 타입 메타데이터와 씬의 실제 그룹을 합친다. 새 대상을 만들 때 고정 enum을 수정할 필요가 없다.
[InitializeOnLoad]
public static class BuffTargetGroupCatalog
{
    private static string[] cached;
    private static double lastRefresh;

    static BuffTargetGroupCatalog()
    {
        EditorApplication.hierarchyChanged += Invalidate;
        EditorApplication.projectChanged += Invalidate;
        EditorApplication.playModeStateChanged += state => Invalidate();
    }

    public static void Invalidate() => cached = null;

    public static string[] Groups
    {
        get
        {
            if (cached != null && EditorApplication.timeSinceStartup - lastRefresh < 1d) return cached;
            var groups = new SortedSet<string>(StringComparer.Ordinal);
            foreach (Type type in TypeCache.GetTypesWithAttribute<BuffTargetGroupsAttribute>())
            {
                if (!typeof(IBuffTarget).IsAssignableFrom(type)) continue;
                var declared = (BuffTargetGroupsAttribute)Attribute.GetCustomAttribute(type,
                    typeof(BuffTargetGroupsAttribute), true);
                if (declared == null) continue;
                foreach (string group in declared.Groups) AddGroup(groups, group);
            }

            // Unity 6000 지원 API. 정렬 없이 비활성 씬 대상도 포함한다.
            foreach (MonoBehaviour component in
             Resources.FindObjectsOfTypeAll<MonoBehaviour>())
            {
                if (component == null ||
                    EditorUtility.IsPersistent(component) ||
                    !component.gameObject.scene.IsValid() ||
                    !component.gameObject.scene.isLoaded ||
                    UnityEditor.SceneManagement.EditorSceneManager
                        .IsPreviewSceneObject(component.gameObject))
                    continue;

                if (component is IBuffTarget target)
                    AddGroup(groups, target.BuffTargetGroup);
            }
            cached = new string[groups.Count];
            groups.CopyTo(cached);
            lastRefresh = EditorApplication.timeSinceStartup;
            return cached;
        }
    }

    private static void AddGroup(SortedSet<string> groups, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        string group = value.Trim().Trim('/');
        while (!string.IsNullOrEmpty(group))
        {
            groups.Add(group);
            int separator = group.LastIndexOf('/');
            if (separator < 0) break;
            group = group.Substring(0, separator);
        }
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
            int selected = string.IsNullOrEmpty(property.stringValue) ? 1 : 0;
            for (int i = 0; i < groups.Length; i++)
            {
                options[i + 2] = new GUIContent(groups[i]);
                if (groups[i] == property.stringValue) selected = i + 2;
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
