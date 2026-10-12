#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SummonActor), true)]
public sealed class SummonActorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("기본 기능 스탯은 Modules에 연결된 각 모듈의 기본 스탯에서 설정합니다. 소환 수명·개수·배치는 Summon Spawn Effect에서 설정합니다.", MessageType.Info);
        DrawPropertiesExcluding(serializedObject, "m_Script", "runtimeModuleStats", "spawnLifetime");
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("런타임 스탯 · 버프 반영 결과 (읽기 전용)", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("runtimeModuleStats"), new GUIContent("모듈별 현재 스탯"), true);
        EditorGUILayout.FloatField("남은 소환 수명", ((SummonActor)target).RemainingLifeTime);
        EditorGUI.EndDisabledGroup();
        serializedObject.ApplyModifiedProperties();
    }
    public override bool RequiresConstantRepaint() => Application.isPlaying;
}
#endif
