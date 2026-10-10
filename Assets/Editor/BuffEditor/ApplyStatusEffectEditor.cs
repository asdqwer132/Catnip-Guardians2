#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ApplyStatusEffect))]
public sealed class ApplyStatusEffectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "statusKey", "lifetimeSettings");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("statusKey"),
            new GUIContent("Status Key", "부여할 상태 키. 같은 상태 키 + 대상이면 출처가 달라도 누적합니다."));
        SerializedProperty settings = serializedObject.FindProperty("lifetimeSettings");
        settings.isExpanded = EditorGUILayout.Foldout(settings.isExpanded, "Lifetime / Reapplication Settings", true);
        if (settings.isExpanded)
        {
            EditorGUI.indentLevel++;
            SerializedProperty child = settings.Copy();
            SerializedProperty end = settings.GetEndProperty();
            bool enterChildren = true;
            while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
            {
                enterChildren = false;
                // 상태 선택은 외부 Status Key만 사용한다. 내부 중복 필드는 표시하지 않는다.
                if (child.name != "statusDefinition")
                    EditorGUILayout.PropertyField(child, true);
            }
            EditorGUI.indentLevel--;
        }
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
