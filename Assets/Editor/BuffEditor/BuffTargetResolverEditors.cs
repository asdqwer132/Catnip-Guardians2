#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 그룹 문자열은 그대로 저장하되 자주 사용하는 그룹을 드롭다운에서 선택할 수 있게 한다.
/// 직접 입력도 남겨 Enemy/특정클래스 같은 동적 그룹을 지원한다.
/// </summary>
internal static class BuffTargetGroupEditorGUI
{
    private static readonly string[] Options =
    {
        "", "Player", "PlayerStatus", "Enemy", "EnemySpawner", "Summon", "Health"
    };

    internal static void Draw(SerializedProperty group, string title, bool allowEmpty)
    {
        int selected = -1;
        for (int i = 0; i < Options.Length; i++)
            if (Options[i] == group.stringValue)
                selected = i;

        // 새 고정 그룹을 추가하려면 Options에 이름을 한 번만 추가하면 된다.
        // 화면에 보일 라벨은 이 배열을 기반으로 자동 생성한다.
        string[] display = new string[Options.Length + 1];
        display[0] = allowEmpty ? "그룹 제한 없음" : "선택 안 함";
        for (int i = 1; i < Options.Length; i++)
            display[i] = ObjectNames.NicifyVariableName(Options[i]) + " (" + Options[i] + ")";
        display[Options.Length] = "직접 입력";
        if (selected < 0)
            selected = display.Length - 1;

        int next = EditorGUILayout.Popup(title, selected, display);
        if (next != selected && next < Options.Length)
            group.stringValue = Options[next];
        if (next == display.Length - 1)
            group.stringValue = EditorGUILayout.TextField("그룹 이름", group.stringValue);

        if (!allowEmpty && string.IsNullOrEmpty(group.stringValue))
            EditorGUILayout.HelpBox("Group Resolver는 그룹을 지정해야 버프 대상을 찾습니다.", MessageType.Warning);
    }
}

[CustomEditor(typeof(BuffTargetGroupResolver))]
public sealed class BuffTargetGroupResolverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        BuffTargetGroupEditorGUI.Draw(serializedObject.FindProperty("targetGroup"), "대상 그룹", false);
        EditorGUILayout.HelpBox("Group Resolver: Enemy를 지정하면 Enemy/클래스명까지 포함합니다. 스탯 종류 및 수치 변경은 BuffEffect의 모디파이어에서 설정합니다.", MessageType.Info);
        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(BuffTargetAreaResolver))]
public sealed class BuffTargetAreaResolverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "requiredGroup");
        BuffTargetGroupEditorGUI.Draw(serializedObject.FindProperty("requiredGroup"), "필요 그룹", true);
        EditorGUILayout.HelpBox("Area Resolver의 그룹은 정확히 일치하는 대상만 통과시킵니다. 예: Enemy는 Enemy/클래스명을 포함하지 않습니다. 특정 적 계열은 그룹을 비우거나 정확한 그룹 이름을 입력하세요.", MessageType.Info);
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
