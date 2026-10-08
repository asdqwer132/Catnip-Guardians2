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
}
#endif
