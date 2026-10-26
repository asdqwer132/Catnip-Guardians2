using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[CustomEditor(typeof(EnemySpawner))]
public sealed class EnemySpawnerEditor : Editor
{
    private readonly BoxBoundsHandle boundsHandle = new BoxBoundsHandle();

    private void OnSceneGUI()
    {
        var spawner = (EnemySpawner)target;
        if (spawner.spawnShape != EnemySpawnShape.Rectangle || spawner.baseStat == null) return;

        boundsHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y;
        boundsHandle.center = spawner.spawnCenter;
        boundsHandle.size = new Vector3(spawner.baseStat.spawnWidth, spawner.baseStat.spawnHeight, 0f);
        using (new Handles.DrawingScope(Color.yellow))
        {
            EditorGUI.BeginChangeCheck();
            boundsHandle.DrawHandle();
            Vector3 center = Handles.Slider2D(boundsHandle.center, Vector3.forward, Vector3.right, Vector3.up,
                HandleUtility.GetHandleSize(boundsHandle.center) * .07f, Handles.RectangleHandleCap, Vector2.zero);
            if (!EditorGUI.EndChangeCheck()) return;

            // Inspector와 동일하게 Undo 및 프리팹 변경사항을 보존합니다.
            serializedObject.Update();
            serializedObject.FindProperty("spawnCenter").vector2Value = center;
            var stat = serializedObject.FindProperty("baseStat");
            stat.FindPropertyRelative("spawnWidth").floatValue = Mathf.Max(.01f, boundsHandle.size.x);
            stat.FindPropertyRelative("spawnHeight").floatValue = Mathf.Max(.01f, boundsHandle.size.y);
            serializedObject.ApplyModifiedProperties();
            if (Application.isPlaying) spawner.RefreshBuffedStat();
        }
    }
}
