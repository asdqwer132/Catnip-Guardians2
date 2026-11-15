using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TooltipUI), true), CanEditMultipleObjects]
public sealed class TooltipUIEditor : Editor
{
    private readonly Dictionary<string, bool> expanded = new Dictionary<string, bool>();
    private static readonly string[][] Groups =
    {
        new[] { "Basic Information", "icon", "dataNameText", "descriptionText", "detailDescriptionText", "dataSource", "hideEmptyDescription", "hideEmptyDetailDescription" },
        new[] { "Series And Grade", "seriesText", "seriesIcon", "gradeText", "gradeIcon", "series", "grade" },
        new[] { "Item Information", "amountText", "categoryText", "weightText", "cooldownText", "effectDescriptions" },
        new[] { "Skill Map Information", "totalNodesText", "completedNodesText" },
        new[] { "Character Portraits", "portraitImage", "portraitDescription", "portraitPage", "portraitInformation", "portraitContent", "portraitSlotPrefab", "portraitSelectionGroup", "previousPortrait", "nextPortrait" },
        new[] { "Map And Stage", "previewImage", "childInventory" },
        new[] { "Showing And Position", "tooltipPanel", "alternatePanels", "startHidden", "useTooltip", "removePannel", "positionAtAnchor", "offset", "clampToParent" }
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

        var drawn = new HashSet<string> { "m_Script" };
        foreach (var group in Groups)
        {
            for (int i = 1; i < group.Length; i++) drawn.Add(group[i]);
            if (!expanded.TryGetValue(group[0], out bool open))
                open = group[0] == "Basic Information" || HasAssignedReferences(group);
            open = EditorGUILayout.Foldout(open, group[0], true);
            expanded[group[0]] = open;
            if (!open) continue;
            using (new EditorGUI.IndentLevelScope())
                for (int i = 1; i < group.Length; i++)
                {
                    var property = serializedObject.FindProperty(group[i]);
                    if (property != null) EditorGUILayout.PropertyField(property, true);
                }
            EditorGUILayout.Space();
        }

        // Gameplay/slot subclasses keep their own controls, without another renderer layer.
        DrawPropertiesExcluding(serializedObject, new List<string>(drawn).ToArray());
        serializedObject.ApplyModifiedProperties();
    }

    private bool HasAssignedReferences(string[] group)
    {
        for (int i = 1; i < group.Length; i++)
        {
            var property = serializedObject.FindProperty(group[i]);
            if (property != null && property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null)
                return true;
        }
        return false;
    }
}
