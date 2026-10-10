#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StatusManager))]
public sealed class StatusManagerEditor : Editor
{
    private readonly List<ActiveBuff> statuses = new List<ActiveBuff>();

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (!Application.isPlaying) return;
        StatusManager manager = (StatusManager)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("적용 중인 상태 키", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Current Stat은 수치 버프의 최종 값입니다. 상태 전용 효과의 적용 여부와 남은 양은 아래에서 확인하세요.", MessageType.Info);
        manager.GetActiveStatuses(statuses);
        if (statuses.Count == 0) EditorGUILayout.LabelField("활성 상태 없음");
        foreach (ActiveBuff active in statuses)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("상태 키", active.statusDefinition, typeof(StatusDefinition), false);
                EditorGUILayout.IntField("상태 중첩 수", active.stack);
                if (active.useLimitType == BuffUseLimitType.Time)
                    EditorGUILayout.FloatField("남은 지속시간 (초)", active.remainTime);
                else if (active.useLimitType == BuffUseLimitType.UseCount)
                    EditorGUILayout.IntField("남은 사용 횟수", active.remainUseCount);
                else EditorGUILayout.LabelField("수명", "무한");
            }
            EditorGUILayout.Space();
        }
    }

    public override bool RequiresConstantRepaint() => Application.isPlaying;
}
#endif
