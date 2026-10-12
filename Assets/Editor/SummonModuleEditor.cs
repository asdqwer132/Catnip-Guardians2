#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SummonBehaviourModule), true)]
public sealed class SummonModuleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "baseStat");
        serializedObject.ApplyModifiedProperties();
        var module = (SummonBehaviourModule)target;
        SerializedProperty baseStat = serializedObject.FindProperty("baseStat");
        if (baseStat == null || module.StatType == null) return;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("기본 스탯 · " + BuffModifierEditorGUI.GetStatLabel(module.StatType), EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("이 모듈의 기본값입니다. 소환물마다 복제해서 사용하며, 버프 결과는 실행 중 Actor의 모듈별 현재 스탯에서 확인하세요.", MessageType.Info);
        serializedObject.Update();
        // 현재 옵션에서 쓰지 않는 수치는 모듈 인스펙터에서 숨깁니다.
        SerializedProperty field = baseStat.Copy();
        SerializedProperty end = baseStat.GetEndProperty();
        bool enter = true;
        while (field.NextVisible(enter) && !SerializedProperty.EqualContents(field, end))
        {
            enter = false;
            if (!IsStatUsed(module, field.name)) continue;
            EditorGUILayout.PropertyField(field, true);
        }
        serializedObject.ApplyModifiedProperties();
    }

    internal static bool IsStatUsed(SummonBehaviourModule module, string field)
    {
        if (module is SummonContactEffectModule contact)
        {
            if (field == "repeatInterval") return contact.triggerMode == SummonContactTriggerMode.WhileTouching;
            if (field == "activationCount") return contact.limitActivations;
        }
        if (module is SummonPeriodicSpawnModule periodic && field == "activationCount") return periodic.limitActivations;
        if (module is SummonHealthModule health && field == "defense") return health.defenseFormula != HealthDefenseFormula.None;
        if (module is SummonItemThrowModule throwing)
        {
            if (field == "forwardSpacing" || field == "sideSpacing") return throwing.spreadMode == AttackPlacementMode.Forward;
            if (field == "damage") return throwing.useModuleDamage;
            if (field == "spreadRadius") return throwing.spreadMode == AttackPlacementMode.CircleEven || throwing.spreadMode == AttackPlacementMode.CircleRandom;
            if (field == "spreadAngle") return throwing.spreadMode == AttackPlacementMode.CircleEven || throwing.spreadMode == AttackPlacementMode.Shotgun;
        }
        if (module is SummonOrbitModule orbit && field == "initialAngle") return !orbit.startFromSpawnAngle;
        return true;
    }
}
#endif
