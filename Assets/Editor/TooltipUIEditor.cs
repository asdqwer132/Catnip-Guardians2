using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DataViewUI), true), CanEditMultipleObjects]
public sealed class TooltipUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "modules");
        var installed = serializedObject.FindProperty("modules");
        for (int i = 0; i < installed.arraySize; i++)
        {
            var module = installed.GetArrayElementAtIndex(i);
            string title = module.managedReferenceValue != null ? ObjectNames.NicifyVariableName(module.managedReferenceValue.GetType().Name) : "Missing Module";
            EditorGUILayout.PropertyField(module, new GUIContent(title), true);
            if (GUILayout.Button("Remove " + title)) { installed.DeleteArrayElementAtIndex(i); break; }
            EditorGUILayout.Space();
        }
        if (GUILayout.Button("Add Information Module"))
        {
            var menu = new GenericMenu();
            foreach (var type in TypeCache.GetTypesDerivedFrom<DataViewModule>())
            {
                if (type.IsAbstract) continue;
                Type selected = type;
                bool exists = ((DataViewUI)target).modules.Exists(module => module != null && module.GetType() == selected);
                if (exists) menu.AddDisabledItem(new GUIContent(ObjectNames.NicifyVariableName(type.Name)));
                else menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(type.Name)), false, () =>
                {
                    foreach (var obj in targets)
                    {
                        var data = new SerializedObject(obj); var modules = data.FindProperty("modules");
                        modules.arraySize++; modules.GetArrayElementAtIndex(modules.arraySize - 1).managedReferenceValue = Activator.CreateInstance(selected);
                        data.ApplyModifiedProperties();
                    }
                });
            }
            menu.ShowAsContext();
        }
        serializedObject.ApplyModifiedProperties();
    }
}
