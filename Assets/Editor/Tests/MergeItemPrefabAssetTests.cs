#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class MergeItemPrefabAssetTests
{
    [Test]
    public void EveryConfiguredMergeItemHasAnIndependentVariantWithTheSharedMergeScript()
    {
        var prefabs = new HashSet<GameObject>();
        for (int index = 1; index <= 5; index++)
        {
            var definition = AssetDatabase.LoadAssetAtPath<MergeItem>(
                "Assets/Script/Items/Merge/MergePrefab/MergeItem " + index + ".asset");
            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.prefab, Is.Not.Null, definition.name);
            Assert.That(prefabs.Add(definition.prefab), Is.True, "아이템마다 다른 프리팹이어야 합니다.");
            var script = definition.prefab.GetComponent<MergeItemObject>();
            Assert.That(script, Is.Not.Null); Assert.That(script.enabled, Is.True);
            Assert.That(script.spriteRenderer, Is.Not.Null); Assert.That(script.visualSize, Is.Not.Null);
            Assert.That(definition.prefab.GetComponent<Rigidbody2D>(), Is.Not.Null);
            Assert.That(definition.prefab.GetComponent<CircleCollider2D>(), Is.Not.Null);
            Assert.That(definition.prefab.GetComponent<PooledObject>().PoolGroup, Is.EqualTo(PoolObjectGroup.MergeItem));
        }
    }
}
#endif
