#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkillTreeNodePlacer))]
public class SkillTreeNodePlacerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SkillTreeNodePlacer placer = (SkillTreeNodePlacer)target;
        if (placer.autoRememberPositions) placer.CaptureNodePositions();
        if (DrawDefaultInspector()) placer.ApplySavedLayout();

        GUILayout.Space(12);

        if (GUILayout.Button("Generate Skill Tree"))
        {
            placer.Generate();
        }

        if (GUILayout.Button("Clear Generated Objects"))
        {
            placer.ClearGeneratedObjects();
        }

        if (GUILayout.Button("Apply Initial Grid Positions")) placer.ApplyGridPositions();
    }
}
#endif
