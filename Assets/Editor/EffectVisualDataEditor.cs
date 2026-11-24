using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EffectVisualData)), CanEditMultipleObjects]
public sealed class EffectVisualDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var loop = serializedObject.FindProperty("loopAnimation");
        var property = serializedObject.GetIterator();
        bool enter = true;
        while (property.NextVisible(enter))
        {
            enter = false;
            bool lifetime = property.name == "useAnimatorClipLifeTime";
            bool locked = loop.boolValue;
            if (loop.hasMultipleDifferentValues) foreach (var selected in targets)
                if (((EffectVisualData)selected).loopAnimation) { locked = true; break; }
            // 다중 편집 시 반복하지 않는 에셋의 설정은 보존합니다.
            if (lifetime && loop.boolValue && !loop.hasMultipleDifferentValues) property.boolValue = false;
            using (new EditorGUI.DisabledScope(property.name == "m_Script" || (lifetime && locked)))
                EditorGUILayout.PropertyField(property, true);
        }
        serializedObject.ApplyModifiedProperties();
    }
}
