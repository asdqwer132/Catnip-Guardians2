using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public sealed class SkillRewardInjector : EditorWindow
{
    public sealed class Binding { public SkillNodeData node; public List<SkillRewardData> rewards; }
    private string root = "Assets/Data/Scriptable/SkillTree";
    private Vector2 scroll;
    private string report = "";
    [MenuItem("Tools/Skills/Reward Injector")]
    public static void Open() => GetWindow<SkillRewardInjector>("Skill Rewards");
    public static List<Binding> Plan(string root)
    {
        if (!AssetDatabase.IsValidFolder(root)) throw new ArgumentException("Select an Assets folder containing Node and Reward folders.");
        var result = new List<Binding>();
        var folders = AssetDatabase.IsValidFolder(root + "/Node") ? new[] { root } : AssetDatabase.GetSubFolders(root);
        foreach (var folder in folders.OrderBy(value => value, StringComparer.Ordinal))
        {
            if (!AssetDatabase.IsValidFolder(folder + "/Node") || !AssetDatabase.IsValidFolder(folder + "/Reward")) continue;
            var nodes = new Dictionary<int, SkillNodeData>();
            foreach (string guid in AssetDatabase.FindAssets("t:SkillNodeData", new[] { folder + "/Node" }))
            {
                var node = AssetDatabase.LoadAssetAtPath<SkillNodeData>(AssetDatabase.GUIDToAssetPath(guid));
                var match = Match(node.dataId, node.name, @"^SkillNodeData\s+(\d+)$");
                if (!match.Success) continue;
                int index = int.Parse(match.Groups[1].Value);
                if (nodes.ContainsKey(index)) throw new InvalidOperationException(folder + ": duplicate node number " + index);
                nodes.Add(index, node);
            }
            var rewards = new Dictionary<int, SortedDictionary<int, SkillRewardData>>();
            foreach (string guid in AssetDatabase.FindAssets("t:SkillRewardData", new[] { folder + "/Reward" }))
            {
                var reward = AssetDatabase.LoadAssetAtPath<SkillRewardData>(AssetDatabase.GUIDToAssetPath(guid));
                var match = Match(reward.name, reward.name, @"^S(\d+)[RN](\d+)_.+$");
                if (!match.Success) continue;
                int nodeIndex = int.Parse(match.Groups[1].Value), rewardIndex = int.Parse(match.Groups[2].Value);
                if (!nodes.ContainsKey(nodeIndex)) throw new InvalidOperationException(folder + ": reward " + reward.name + " has no matching node.");
                if (!rewards.TryGetValue(nodeIndex, out var slots)) { slots = new SortedDictionary<int, SkillRewardData>(); rewards.Add(nodeIndex, slots); }
                if (slots.ContainsKey(rewardIndex)) throw new InvalidOperationException(folder + ": duplicate reward slot S" + nodeIndex + "R" + rewardIndex);
                slots.Add(rewardIndex, reward);
            }
            foreach (var pair in rewards.OrderBy(value => value.Key))
                result.Add(new Binding { node = nodes[pair.Key], rewards = pair.Value.Values.ToList() });
        }
        return result;
    }
    private static Match Match(string id, string name, string pattern)
    {
        var match = Regex.Match(id ?? "", pattern, RegexOptions.CultureInvariant);
        return match.Success ? match : Regex.Match(name ?? "", pattern, RegexOptions.CultureInvariant);
    }
    public static void Apply(List<Binding> plan)
    {
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Inject skill rewards");
        foreach (var entry in plan)
        { Undo.RecordObject(entry.node, "Inject skill rewards"); entry.node.rewards = new List<SkillRewardData>(entry.rewards); EditorUtility.SetDirty(entry.node); }
        Undo.CollapseUndoOperations(group); AssetDatabase.SaveAssets();
    }
    private void OnGUI()
    {
        root = EditorGUILayout.TextField("Series folder / Root", root);
        EditorGUILayout.HelpBox("Node: SkillNodeData n (ID or asset name). Reward: SnRn_effect (asset name); legacy SnNn_effect is also supported. Matching nodes are replaced in R order. Nodes without named rewards stay unchanged.", MessageType.Info);
        if (GUILayout.Button("Preview")) Run(false);
        if (GUILayout.Button("Inject Rewards")) Run(true);
        scroll = EditorGUILayout.BeginScrollView(scroll); EditorGUILayout.TextArea(report); EditorGUILayout.EndScrollView();
    }
    private void Run(bool apply)
    {
        try { var plan = Plan(root); report = string.Join("\n", plan.Select(value => value.node.name + " <- " + string.Join(", ", value.rewards.Select(r => r.name)))); if (apply) Apply(plan); }
        catch (Exception error) { report = error.Message; Debug.LogWarning(error.Message); }
    }
}
