#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(PrefabVisualAnimator))]
public sealed class PrefabVisualAnimatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script");
        serializedObject.ApplyModifiedProperties();
        string issue = ((PrefabVisualAnimator)target).ConfigurationIssue;
        if (issue != null) EditorGUILayout.HelpBox(issue, MessageType.Warning);
        else EditorGUILayout.HelpBox("Visual의 모습만 바꿉니다. 범위 기즈모는 실제 판정용 값과 별도로 표시됩니다. 풀 반환·비활성화 시 위치/크기/회전/색을 복원합니다.", MessageType.Info);
    }
}
#endif
