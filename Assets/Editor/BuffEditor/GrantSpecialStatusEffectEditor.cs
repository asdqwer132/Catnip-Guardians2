#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GrantSpecialStatusEffect))]
public sealed class GrantSpecialStatusEffectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("specialStatusKey"),
            new GUIContent("Special Status Identity Key", "Star·Arrow 같은 상태의 식별 키. 같은 키 + 대상이면 출처가 달라도 누적합니다."));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("buffClassification"),
            new GUIContent("Buff Classification (Optional)", "해로움·정화 가능 여부·정화 태그. 상태 키와 별개입니다."));
        DrawPropertiesExcluding(serializedObject, "m_Script", "specialStatusKey", "buffClassification", "lifetimeSettings");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lifetimeSettings"),
            new GUIContent("Special Status Lifetime / Reapplication"), true);
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
