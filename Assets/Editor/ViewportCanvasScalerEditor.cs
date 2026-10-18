#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// CanvasScaler 기본 에디터는 파생 클래스의 추가 필드를 표시하지 않으므로 명시적으로 연결합니다.
[CustomEditor(typeof(ViewportCanvasScaler)), CanEditMultipleObjects]
public sealed class ViewportCanvasScalerEditor : UnityEditor.UI.CanvasScalerEditor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var profile = serializedObject.FindProperty("sharedScaleProfile");
        EditorGUILayout.PropertyField(profile, new GUIContent("Shared Scale Profile"));
        serializedObject.ApplyModifiedProperties();
        if (profile.objectReferenceValue != null && !profile.hasMultipleDifferentValues)
        {
            EditorGUILayout.HelpBox("UI 배율과 Reference PPU는 연결된 프로필에서 수정합니다. 카메라/Anchor/Layout Group의 영역 비율은 유지됩니다. 같은 Sprite와 PPU Multiplier는 같은 테두리 크기로 표시됩니다.", MessageType.Info);
            return;
        }
        base.OnInspectorGUI();
    }
}
#endif
