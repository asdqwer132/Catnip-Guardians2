#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkillTreeNodePlacer))]
public class SkillTreeNodePlacerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SkillTreeNodePlacer placer = (SkillTreeNodePlacer)target;

        GUILayout.Space(12);

        if (GUILayout.Button("Generate Skill Tree"))
        {
            Undo.RegisterFullObjectHierarchyUndo(placer.gameObject, "Generate Skill Tree");
            placer.Generate();
        }

        if (GUILayout.Button("Clear Generated Objects"))
        {
            Undo.RegisterFullObjectHierarchyUndo(placer.gameObject, "Clear Skill Tree");
            placer.ClearGeneratedObjects();
        }
    }
}
#endif