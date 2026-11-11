using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(MergegameManager))]
public sealed class MergegameManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var manager = (MergegameManager)target;
        if (manager.recipeBook != null && GUILayout.Button("레시피북 그래프 열기")) MergeRecipeGraphWindow.Open(manager.recipeBook);
    }
}
