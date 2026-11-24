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
            if (manager.layout == EnemyAttackSlotLayout.RectangularRings)
            {
                using (new Handles.DrawingScope(manager.gizmoColor))
                {
                    Vector3 center = manager.RingCenter;
                    EnemyRegionGeometry.RectangleHalfSizes(slot.rectangularSize, slot.rectangularThickness, out var inner, out var outer);
                    Handles.DrawWireCube(center, inner * 2f);
                    Handles.DrawWireCube(center, outer * 2f);
                    Handles.Label(center + Vector3.up * outer.y, $"{slot.label} ({slot.ReservedCount}/{slot.capacity})");
                    EditorGUI.BeginChangeCheck();
                    float width = Handles.ScaleSlider(slot.rectangularSize.x * .5f, center, Vector3.right,
                        Quaternion.identity, HandleUtility.GetHandleSize(center), .1f) * 2f;
                    float height = Handles.ScaleSlider(slot.rectangularSize.y * .5f, center, Vector3.up,
                        Quaternion.identity, HandleUtility.GetHandleSize(center), .1f) * 2f;
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.Update();
                        serializedObject.FindProperty("slots").GetArrayElementAtIndex(i).FindPropertyRelative("rectangularSize").vector2Value
                            = new Vector2(Mathf.Max(.01f, width), Mathf.Max(.01f, height));
                        serializedObject.ApplyModifiedProperties();
                    }
                }
                continue;
            }
            if (manager.layout == EnemyAttackSlotLayout.RadialRings)
            {
                using (new Handles.DrawingScope(manager.gizmoColor))
                {
                    Vector3 center = manager.RingCenter;
                    string ringLabel = string.IsNullOrEmpty(slot.label) ? $"Ring {i + 1}" : slot.label;
                    Handles.Label(center + Vector3.right * slot.radius, $"{ringLabel} ({slot.ReservedCount}/{slot.capacity})");
                    EditorGUI.BeginChangeCheck();
                    float inner = Handles.RadiusHandle(Quaternion.identity, center, Mathf.Max(0f, slot.radius - slot.radialThickness * .5f));
                    float outer = Handles.RadiusHandle(Quaternion.identity, center, slot.radius + slot.radialThickness * .5f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.Update();
                        var element = serializedObject.FindProperty("slots").GetArrayElementAtIndex(i);
                        element.FindPropertyRelative("radius").floatValue = Mathf.Max(0f, (inner + outer) * .5f);
                        element.FindPropertyRelative("radialThickness").floatValue = Mathf.Abs(outer - inner);
                        serializedObject.ApplyModifiedProperties();
                    }
                }
                continue;
            }
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
