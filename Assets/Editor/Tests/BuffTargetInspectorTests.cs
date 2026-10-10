#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class BuffTargetInspectorTests
{
    [Test] public void GroupCatalogIncludesDamageAreaWithoutAnAttackInTheOpenScene()
    {
        BuffTargetGroupCatalog.Invalidate();
        Assert.That(BuffTargetGroupCatalog.Groups, Does.Contain("DamageArea"));
        Assert.That(BuffTargetGroupCatalog.Groups, Does.Contain("Player"));
    }

    [Test] public void ReadingAutoChoicesPreservesAUserEnteredGroupAndSupportsUndo()
    {
        BuffTargetGroupResolver resolver = ScriptableObject.CreateInstance<BuffTargetGroupResolver>();
        try
        {
            var serialized = new SerializedObject(resolver);
            SerializedProperty group = serialized.FindProperty("targetGroup");
            group.stringValue = "MyCustomGroup/Fire";
            serialized.ApplyModifiedProperties();
            CollectionAssert.IsNotEmpty(BuffTargetGroupCatalog.Groups);
            serialized.Update();
            Assert.That(group.stringValue, Is.EqualTo("MyCustomGroup/Fire"));
            Undo.IncrementCurrentGroup();
            group.stringValue = "DamageArea";
            serialized.ApplyModifiedProperties();
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            serialized.Update();
            Assert.That(group.stringValue, Is.EqualTo("MyCustomGroup/Fire"));
        }
        finally { Undo.ClearUndo(resolver); Object.DestroyImmediate(resolver); }
    }

    private sealed class UnannotatedTarget : IBuffTarget
    {
        public Object BuffTargetObject => null;
        public string BuffTargetGroup => "InspectorCatalog/DeclaredOnly";
        public string BuffTargetDebugName => "Test";
        public void RefreshBuffedStat() { }
    }

    [Test] public void GroupDiscoveryReadsAnUnannotatedImplementationWithoutAnInstance()
    {
        Assert.That(BuffTargetGroupDiscovery.GetDeclaredGroups(typeof(UnannotatedTarget)), Does.Contain("InspectorCatalog/DeclaredOnly"));
        BuffTargetGroupCatalog.Invalidate();
        Assert.That(BuffTargetGroupCatalog.Groups, Does.Not.Contain("InspectorCatalog/DeclaredOnly"));
    }

    [Test] public void GroupCatalogIncludesInactiveSceneTargetsAndTheirParentGroups()
    {
        var host = new GameObject("Inactive Buff Group Test");
        host.SetActive(false);
        try
        {
            var target = host.AddComponent<Health>(); target.buffTargetGroup = "InspectorCatalog/Inactive";
            BuffTargetGroupCatalog.Invalidate();
            Assert.That(BuffTargetGroupCatalog.Groups, Does.Contain("InspectorCatalog/Inactive"));
            Assert.That(BuffTargetGroupCatalog.Groups, Does.Contain("InspectorCatalog"));
        }
        finally { Object.DestroyImmediate(host); BuffTargetGroupCatalog.Invalidate(); }
    }

    [Test] public void GroupCatalogIncludesAProjectPrefabWithoutASceneInstance()
    {
        string folder = "Assets/__BuffGroupTest_" + System.Guid.NewGuid().ToString("N");
        var host = new GameObject("Prefab Buff Group Test"); host.SetActive(false);
        try
        {
            AssetDatabase.CreateFolder("Assets", folder.Substring("Assets/".Length));
            var target = host.AddComponent<Health>(); target.buffTargetGroup = "InspectorCatalog/PrefabOnly";
            Assert.That(PrefabUtility.SaveAsPrefabAsset(host, folder + "/Target.prefab"), Is.Not.Null);
            Object.DestroyImmediate(host); host = null;
            BuffTargetGroupCatalog.Invalidate();
            Assert.That(BuffTargetGroupCatalog.Groups, Does.Contain("InspectorCatalog/PrefabOnly"));
            Assert.That(BuffTargetGroupCatalog.Groups, Does.Contain("InspectorCatalog"));
        }
        finally
        {
            if (host != null) Object.DestroyImmediate(host);
            AssetDatabase.DeleteAsset(folder);
            BuffTargetGroupCatalog.Invalidate();
        }
    }
}
#endif
