using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MergeItemIconEditor : EditorWindow
{
    [SerializeField] private List<MergeItem> items = new();
    [SerializeField] private List<ItemData> datas = new();

    private SerializedObject serializedData;

    [MenuItem("Tools/Item/Merge/Merge Item Icon Editor")]
    public static void Open()
    {
        GetWindow<MergeItemIconEditor>("Merge Item Icons");
    }

    private void OnEnable()
    {
        serializedData = new SerializedObject(this);
    }

    private void OnGUI()
    {
        serializedData.Update();

        EditorGUILayout.PropertyField(
            serializedData.FindProperty("items"), true);

        EditorGUILayout.PropertyField(
            serializedData.FindProperty("datas"), true);

        serializedData.ApplyModifiedProperties();

        GUILayout.Space(10);

        if (GUILayout.Button("Apply Items"))
        {
            int count = Mathf.Min(items.Count, datas.Count);

            for (int i = 0; i < count; i++)
            {
                if (items[i] == null || datas[i] == null)
                    continue;

                Undo.RecordObject(items[i], "Set Merge Item");

                items[i].item = datas[i];

                EditorUtility.SetDirty(items[i]);
            }

            AssetDatabase.SaveAssets();
        }
    }
}