#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(FloatFieldBuffModifier))]
public sealed class FloatFieldBuffModifierEditor : Editor
{
    public override void OnInspectorGUI()
    {
        BuffModifierEditorGUI.DrawFloatModifier(serializedObject);
    }
}
#endif
