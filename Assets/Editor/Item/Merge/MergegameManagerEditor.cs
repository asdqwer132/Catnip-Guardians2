using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MergegameManager))]
public sealed class MergegameManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var manager = (MergegameManager)target;
        foreach (var book in manager.RecipeBooks)
            if (book != null && GUILayout.Button(book.name + " 그래프 열기")) MergeRecipeGraphWindow.Open(book);
    }
}
