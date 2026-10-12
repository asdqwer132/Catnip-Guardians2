#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;

public sealed class SummonArchitectureEditorTests
{
    [Test]
    public void InspectorLabelsGroupCreationAttackMovementAndSupportStats()
    {
        Assert.That(BuffModifierEditorGUI.GetStatLabel(typeof(SummonSpawnStat)), Does.StartWith("소환/생성/"));
        Assert.That(BuffModifierEditorGUI.GetStatLabel(typeof(SummonContactDamageStat)), Does.StartWith("소환/공격/"));
        Assert.That(BuffModifierEditorGUI.GetStatLabel(typeof(SummonOrbitStat)), Does.StartWith("소환/이동/"));
        Assert.That(BuffModifierEditorGUI.GetStatLabel(typeof(SummonPeriodicSpawnStat)), Does.StartWith("소환/지원/"));
        Assert.That(BuffModifierEditorGUI.GetStatLabel(typeof(SummonHealthStat)), Does.StartWith("소환/지원/"));
    }

    [Test]
    public void ModifierDropdownSeparatesCreationAndModuleFields()
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        var types = (IList<Type>)typeof(BuffModifierEditorGUI).GetProperty("StatTypes", flags).GetValue(null);
        Assert.That(types, Does.Contain(typeof(SummonSpawnStat)));
        foreach (Type type in new[] { typeof(SummonContactDamageStat), typeof(SummonContactEffectStat),
            typeof(SummonTurretStat), typeof(SummonItemThrowStat), typeof(SummonOrbitStat), typeof(SummonPullStat),
            typeof(SummonAuraStat), typeof(SummonPeriodicSpawnStat), typeof(SummonHealthStat) })
            Assert.That(types, Does.Contain(type));
        MethodInfo getFields = typeof(BuffModifierEditorGUI).GetMethod("GetFloatFields", flags);
        var creation = (List<FieldInfo>)getFields.Invoke(null, new object[] { typeof(SummonSpawnStat) });
        var actor = (List<FieldInfo>)getFields.Invoke(null, new object[] { typeof(SummonContactDamageStat) });
        Assert.That(creation.Exists(field => field.Name == nameof(SummonSpawnStat.count)), Is.True);
        Assert.That(creation.Exists(field => field.Name == nameof(SummonSpawnStat.lifetime)), Is.True);
        Assert.That(creation.Exists(field => field.Name == "attackPower"), Is.False);
        Assert.That(actor.Exists(field => field.Name == nameof(SummonContactDamageStat.damage)), Is.True);
        Assert.That(actor.Exists(field => field.Name == "lifetime" || field.Name == "count"), Is.False);
        foreach (FieldInfo field in typeof(SummonActor).GetFields())
            Assert.That(Array.Exists(field.FieldType.GetInterfaces(), contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IGameStat<>)), Is.False, "Actor에는 기본 기능 스탯이 없어야 합니다.");
    }

    [Test]
    public void Food28KeepsCreationSettingsInEffectAndFunctionSettingsInModuleProfile()
    {
        const string path = "Assets/Data/Scriptable/Item/Effect/02.Food/Food 28 Yet/SummonSpawnEffect.asset";
        SummonSpawnEffect effect = AssetDatabase.LoadAssetAtPath<SummonSpawnEffect>(path);
        Assert.That(effect, Is.Not.Null);
        Assert.That(effect.spawnStat.lifetime, Is.EqualTo(1f));
        Assert.That(effect.spawnStat.SpawnCount, Is.EqualTo(1));
        Assert.That(effect.summonPrefab, Is.Not.Null);

        Assert.That(effect.summonPrefab.modules, Has.Length.EqualTo(1));
        Assert.That(effect.summonPrefab.modules[0], Is.InstanceOf<SummonContactDamageModule>());
        var contact = (SummonContactDamageModule)effect.summonPrefab.modules[0];
        Assert.That(contact.stats, Is.Not.Null);
        Assert.That(contact.stats.baseStat.damage, Is.EqualTo(20f)); // 이전 10 + Actor 공격력 10을 보존합니다.
        Assert.That(contact.stats.baseStat.interval, Is.EqualTo(1f));
        ItemData food = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Data/Scriptable/Item/Items/02.Food/Food28.asset");
        Assert.That(food, Is.Not.Null);
        Assert.That(Array.Exists(food.effectDatas, entry => entry == effect), Is.True, "효과 GUID를 보존해 Food28 연결이 이어져야 합니다.");
    }
}
#endif
