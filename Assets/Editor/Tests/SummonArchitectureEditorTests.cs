#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;

public sealed class SummonArchitectureEditorTests
{
    [Test]
    public void ModifierDropdownSeparatesCreationAndActorFields()
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        var types = (IList<Type>)typeof(BuffModifierEditorGUI).GetProperty("StatTypes", flags).GetValue(null);
        Assert.That(types, Does.Contain(typeof(SummonSpawnStat)));
        Assert.That(types, Does.Contain(typeof(SummonActorStat)));
        MethodInfo getFields = typeof(BuffModifierEditorGUI).GetMethod("GetFloatFields", flags);
        var creation = (List<FieldInfo>)getFields.Invoke(null, new object[] { typeof(SummonSpawnStat) });
        var actor = (List<FieldInfo>)getFields.Invoke(null, new object[] { typeof(SummonActorStat) });
        Assert.That(creation.Exists(field => field.Name == nameof(SummonSpawnStat.count)), Is.True);
        Assert.That(creation.Exists(field => field.Name == nameof(SummonSpawnStat.lifetime)), Is.True);
        Assert.That(creation.Exists(field => field.Name == "attackPower"), Is.False);
        Assert.That(actor.Exists(field => field.Name == nameof(SummonActorStat.attackPower)), Is.True);
        Assert.That(actor.Exists(field => field.Name == "lifetime" || field.Name == "count"), Is.False);
    }

    [Test]
    public void Food28KeepsCreationSettingsInEffectAndFunctionSettingsOnItsPrefab()
    {
        const string path = "Assets/Data/Scriptable/Item/Effect/02.Food/Food 28 Yet/SummonSpawnEffect.asset";
        SummonSpawnEffect effect = AssetDatabase.LoadAssetAtPath<SummonSpawnEffect>(path);
        Assert.That(effect, Is.Not.Null);
        Assert.That(effect.spawnStat.lifetime, Is.EqualTo(1f));
        Assert.That(effect.spawnStat.SpawnCount, Is.EqualTo(1));
        Assert.That(effect.summonPrefab, Is.Not.Null);
        Assert.That(effect.summonPrefab.baseActorStat.attackPower, Is.EqualTo(10f));
        Assert.That(effect.summonPrefab.baseActorStat.attackInterval, Is.EqualTo(0.5f));
        Assert.That(effect.summonPrefab.modules, Has.Length.EqualTo(1));
        Assert.That(effect.summonPrefab.modules[0], Is.InstanceOf<SummonContactDamageModule>());
        ItemData food = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Data/Scriptable/Item/Items/02.Food/Food28.asset");
        Assert.That(food, Is.Not.Null);
        Assert.That(Array.Exists(food.effectDatas, entry => entry == effect), Is.True, "효과 GUID를 보존해 Food28 연결이 이어져야 합니다.");
    }
}
#endif
