using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(PlantData))]
public sealed class PlantGrowthEditor : Editor
{
    private ReorderableList growthStages;

    private void OnEnable()
    {
        var stages = serializedObject.FindProperty(nameof(PlantData.growthVisuals));
        growthStages = new ReorderableList(serializedObject, stages, true, true, true, true);
        growthStages.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Growth Visuals");
        growthStages.drawElementCallback = (rect, index, active, focused) =>
        {
            rect.height = EditorGUIUtility.singleLineHeight;
            string label = index == 0 ? "Seed" : $"Growth {index}";
            EditorGUI.PropertyField(rect, stages.GetArrayElementAtIndex(index), new GUIContent(label));
        };
        growthStages.onAddCallback = AddStage;
        growthStages.onRemoveCallback = RemoveStage;
        growthStages.onCanAddCallback = _ => AssetDatabase.IsMainAsset(target);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, nameof(PlantData.growthVisuals));
        growthStages.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }

    private void AddStage(ReorderableList list)
    {
        var plant = (PlantData)target;
        StartUndoGroup("Add Growth Visual");

        var stage = CreateInstance<PlantBattleVisualData>();
        string path = AssetDatabase.GetAssetPath(plant);
        var names = Array.ConvertAll(AssetDatabase.LoadAllAssetsAtPath(path), asset => asset.name);
        stage.name = ObjectNames.GetUniqueName(names, plant.name + " Growth Visual");
        AssetDatabase.AddObjectToAsset(stage, plant);
        Undo.RegisterCreatedObjectUndo(stage, "Add Growth Visual");
        serializedObject.Update();

        var items = list.serializedProperty;
        int index = items.arraySize;
        items.arraySize++;
        items.GetArrayElementAtIndex(index).objectReferenceValue = stage;
        CommitChanges(plant);
        list.index = index;
    }

    private void RemoveStage(ReorderableList list)
    {
        var plant = (PlantData)target;
        var items = list.serializedProperty;
        int index = list.index;
        if (index < 0 || index >= items.arraySize) return;

        StartUndoGroup("Remove Growth Visual");
        var entry = items.GetArrayElementAtIndex(index);
        var stage = entry.objectReferenceValue as PlantBattleVisualData;
        entry.objectReferenceValue = null;
        items.DeleteArrayElementAtIndex(index);
        serializedObject.ApplyModifiedProperties();
        list.index = Mathf.Min(index, items.arraySize - 1);

        if (stage != null && AssetDatabase.IsSubAsset(stage) &&
            AssetDatabase.GetAssetPath(stage) == AssetDatabase.GetAssetPath(plant) &&
            Array.IndexOf(plant.growthVisuals, stage) < 0)
            Undo.DestroyObjectImmediate(stage);

        CommitChanges(plant);
    }

    private void StartUndoGroup(string name)
    {
        serializedObject.ApplyModifiedProperties();
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(name);
        Undo.RegisterCompleteObjectUndo(target, name);
    }

    private void CommitChanges(PlantData plant)
    {
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(plant);
        AssetDatabase.SaveAssetIfDirty(plant);
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
    }
}
