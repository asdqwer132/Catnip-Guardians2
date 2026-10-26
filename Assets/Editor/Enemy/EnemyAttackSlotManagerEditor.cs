using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[CustomEditor(typeof(EnemyAttackSlotManager))]
public sealed class EnemyAttackSlotManagerEditor : Editor
{
    private readonly List<BoxBoundsHandle> handles = new List<BoxBoundsHandle>();

    private void OnSceneGUI()
    {
        var manager = (EnemyAttackSlotManager)target;
        for (int i = 0; i < manager.slots.Count; i++)
        {
            var slot = manager.slots[i];
            if (slot == null) continue;
            while (handles.Count <= i) handles.Add(new BoxBoundsHandle());
            var bounds = handles[i];
            bounds.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y;
            bounds.center = slot.area.center;
            bounds.size = new Vector3(slot.area.width, slot.area.height, 0f);
            using (new Handles.DrawingScope(manager.gizmoColor))
            {
                string label = string.IsNullOrEmpty(slot.label) ? $"Slot {i + 1}" : slot.label;
                Handles.Label(slot.area.center, $"{label} ({slot.ReservedCount}/{slot.capacity})");
                EditorGUI.BeginChangeCheck();
                bounds.DrawHandle();
                Vector3 center = Handles.Slider2D(bounds.center, Vector3.forward, Vector3.right, Vector3.up,
                    HandleUtility.GetHandleSize(bounds.center) * .05f, Handles.RectangleHandleCap, Vector2.zero);
                if (!EditorGUI.EndChangeCheck()) continue;
                serializedObject.Update();
                Vector2 size = new Vector2(Mathf.Max(.01f, bounds.size.x), Mathf.Max(.01f, bounds.size.y));
                serializedObject.FindProperty("slots").GetArrayElementAtIndex(i).FindPropertyRelative("area").rectValue
                    = new Rect((Vector2)center - size * .5f, size);
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
