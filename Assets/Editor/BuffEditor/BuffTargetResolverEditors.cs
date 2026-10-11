#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AllTargetsBuffTargetResolver))]
[CanEditMultipleObjects]
public sealed class AllTargetsBuffTargetResolverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("All Targets: 모든 아이템·가방과 IBuffTarget 오브젝트를 포함합니다. " +
            "Enemy, Player, StatusManager, Health, DamageArea, Summon 및 사용자 정의 그룹도 포함합니다. " +
            "버프 유지 중 새로 생성되는 대상도 포함하며, 수치 변경 범위는 Modifier의 스탯/필드로 정합니다.", MessageType.Info);
    }
}

[CustomEditor(typeof(BuffTargetGroupResolver))]
[CanEditMultipleObjects]
public sealed class BuffTargetGroupResolverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty group = serializedObject.FindProperty("targetGroup");
        // 공통 드로어가 직접 입력과 IBuffTarget 자동 선택을 함께 제공한다.
        EditorGUILayout.PropertyField(group, new GUIContent("대상 그룹"));
        if (!group.hasMultipleDifferentValues && string.IsNullOrEmpty(BuffTargetHandle.NormalizeGroupName(group.stringValue)))
            EditorGUILayout.HelpBox("Group Resolver는 그룹을 지정해야 버프 대상을 찾습니다.", MessageType.Warning);
        EditorGUILayout.HelpBox("Group Resolver: Enemy를 지정하면 Enemy/클래스명까지 포함합니다. 스탯 종류 및 수치 변경은 BuffEffect의 모디파이어에서 설정합니다.", MessageType.Info);
        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(BuffTargetAreaResolver))]
[CanEditMultipleObjects]
public sealed class BuffTargetAreaResolverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "requiredGroup");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("requiredGroup"), new GUIContent("필요 그룹"));
        EditorGUILayout.HelpBox("Area Resolver: 지정 그룹과 하위 그룹을 반경 안에서 선택합니다. Enemy는 Enemy/클래스명도 포함하며, 비우면 모든 그룹을 허용합니다.", MessageType.Info);
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
