#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SummonBehaviourModule), true)]
public sealed class SummonModuleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script");
        serializedObject.ApplyModifiedProperties();
        var module = (SummonBehaviourModule)target;
        if (module.StatDefinition == null)
        {
            EditorGUILayout.HelpBox("이 기능의 Stat Definition 프로필을 만들어 Stats에 연결하세요. 미지정이면 해당 스탯의 기본값으로 실행됩니다.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField(BuffModifierEditorGUI.GetStatLabel(module.StatDefinition.StatType), EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("기본 스탯 · 공유 프로필\n아래 값은 연결된 프로필 에셋의 기본값입니다. 같은 프로필을 쓰는 모듈에 함께 반영됩니다. 버프 결과는 실행 중 Actor의 런타임 스탯에서 확인하세요.", MessageType.Info);
        var profile = new SerializedObject(module.StatDefinition);
        profile.Update();
        SerializedProperty baseStat = profile.FindProperty("baseStat");
        if (baseStat != null)
        {
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
        }
        profile.ApplyModifiedProperties();
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
