using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class SkillGraphTests
{
    private readonly List<Object> owned = new List<Object>();
    private T Asset<T>() where T : ScriptableObject { var a = ScriptableObject.CreateInstance<T>(); owned.Add(a); return a; }
    [TearDown] public void Cleanup()
    { foreach (var a in owned) if (a != null) Object.DestroyImmediate(a); owned.Clear(); AssetDatabase.DeleteAsset("Assets/__SkillInjectorTests"); }
    [Test] public void ConnectionsBecomeRuntimePrerequisitesAndCyclesAreRejected()
    {
        var layout = Asset<SkillTreeLayoutData>(); var a = Asset<SkillNodeData>(); var b = Asset<SkillNodeData>(); var c = Asset<SkillNodeData>();
        foreach (var n in new[] { a, b, c }) SkillTreeGraphEditing.Add(layout, n, Vector2.zero);
        Assert.That(SkillTreeGraphEditing.Connect(layout, a, b), Is.True);
        Assert.That(SkillTreeGraphEditing.Connect(layout, b, c), Is.True);
        Assert.That(SkillTreeGraphEditing.Connect(layout, c, a), Is.False);
        Assert.That(SkillTreeGraphEditing.Connect(layout, a, b), Is.False);
        Assert.That(b.requiredSkills, Is.EqualTo(new[] { a }));
        SkillTreeGraphEditing.Move(layout, b, new Vector2(123, 456));
        Assert.That(layout.nodeLayout.Single(n => n.node == b).position, Is.EqualTo(new Vector2(123, 456)));
        SkillTreeGraphEditing.Remove(layout, b); Assert.That(c.requiredSkills, Is.Empty);
    }
    [Test] public void ExistingSeriesFoldersCanBePreviewedWithoutChangingRewards()
    { Assert.That(SkillRewardInjector.Plan("Assets/Data/Scriptable/SkillTree").Count, Is.GreaterThan(0)); }

    [Test] public void InjectorMatchesSeriesAndNumericRewardOrderAndRejectsAmbiguousSlots()
    {
        string root = "Assets/__SkillInjectorTests"; AssetDatabase.CreateFolder("Assets", "__SkillInjectorTests");
        AssetDatabase.CreateFolder(root, "Node"); AssetDatabase.CreateFolder(root, "Reward");
        var node = ScriptableObject.CreateInstance<SkillNodeData>(); node.dataId = "SkillNodeData 2";
        AssetDatabase.CreateAsset(node, root + "/Node/SkillNodeData 2.asset");
        foreach (string name in new[] { "S2R10_Ten", "S2R2_Two", "S2N1_One" })
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<MainPlantIndexSkillReward>(), root + "/Reward/" + name + ".asset");
        var plan = SkillRewardInjector.Plan(root);
        Assert.That(plan.Count, Is.EqualTo(1));
        Assert.That(plan[0].rewards.Select(r => r.name), Is.EqualTo(new[] { "S2N1_One", "S2R2_Two", "S2R10_Ten" }));
        SkillRewardInjector.Apply(plan); SkillRewardInjector.Apply(SkillRewardInjector.Plan(root));
        Assert.That(node.rewards.Count, Is.EqualTo(3));
        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<MainPlantIndexSkillReward>(), root + "/Reward/S2R1_Duplicate.asset");
        Assert.Throws<InvalidOperationException>(() => SkillRewardInjector.Plan(root));
        Assert.That(node.rewards.Count, Is.EqualTo(3));
    }
}

public class SkillGraphLayoutIntegrationTests : SkillTreeLayoutFixture
{
    [SetUp] public void Setup()
    {
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
        owned = new List<Object>();
    }
    [TearDown] public void Cleanup()
    {
        Dispose();
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
    }
    [Test] public void AssetChangesCannotBeOverwrittenByStaleSceneViewsAndSceneMovesUpdateTheSameAsset()
    {
        var placer = Placer(); placer.Generate();
        var layout = Asset<SkillTreeLayoutData>();
        layout.nodeDatas.AddRange(placer.nodeDatas);
        foreach (var entry in placer.nodeLayout) layout.nodeLayout.Add(new SkillNodePlacement { node = entry.node, position = entry.position });
        placer.layout = layout; placer.ApplySavedLayout();
        var node = layout.nodeDatas[1]; var rect = (RectTransform)View(placer, node).transform;
        SkillTreeGraphEditing.Move(layout, node, new Vector2(310, 270));
        placer.CaptureNodePositions();
        Assert.That(rect.anchoredPosition, Is.EqualTo(new Vector2(310, 270)));
        rect.anchoredPosition = new Vector2(400, 300); placer.CaptureNodePositions();
        Assert.That(layout.nodeLayout.Single(n => n.node == node).position, Is.EqualTo(new Vector2(400, 300)));
    }
    [Test] public void GraphCanvasBuildsNodesPortsAndPrerequisiteEdges()
    {
        var layout = Asset<SkillTreeLayoutData>(); var a = Data("a"); var b = Data("b");
        SkillTreeGraphEditing.Add(layout, a, Vector2.zero); SkillTreeGraphEditing.Add(layout, b, Vector2.right * 300);
        SkillTreeGraphEditing.Connect(layout, a, b);
        // Build the real GraphView without opening a native window: batchmode has no graphics device.
        var canvasType = typeof(SkillTreeGraphWindow).GetNestedType("Canvas", System.Reflection.BindingFlags.NonPublic);
        var graph = (UnityEditor.Experimental.GraphView.GraphView)Activator.CreateInstance(canvasType, new object[] { layout });
        Assert.That(graph.nodes.Count(), Is.EqualTo(2)); Assert.That(graph.edges.Count(), Is.EqualTo(1));
    }
}
