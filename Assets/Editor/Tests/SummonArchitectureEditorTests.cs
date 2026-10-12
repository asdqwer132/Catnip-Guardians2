#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;

public sealed class SummonArchitectureEditorTests
{
    [Test]
    public void EveryFunctionalModuleOwnsAnInlineStatValue()
    {
        Type[] modules = { typeof(SummonContactDamageModule), typeof(SummonContactEffectModule), typeof(SummonTurretModule),
            typeof(SummonItemThrowModule), typeof(SummonOrbitModule), typeof(SummonPullModule), typeof(SummonAuraModule),
            typeof(SummonPeriodicSpawnModule), typeof(SummonHealthModule) };
        Type[] stats = { typeof(SummonContactDamageStat), typeof(SummonContactEffectStat), typeof(SummonTurretStat),
            typeof(SummonItemThrowStat), typeof(SummonOrbitStat), typeof(SummonPullStat), typeof(SummonAuraStat),
            typeof(SummonPeriodicSpawnStat), typeof(SummonHealthStat) };
        for (int i = 0; i < modules.Length; i++)
        {
            FieldInfo field = modules[i].GetField("baseStat");
            Assert.That(field, Is.Not.Null, modules[i].Name);
            Assert.That(field.FieldType, Is.EqualTo(stats[i]));
            Assert.That(typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType), Is.False,
                "기본 스탯은 별도 에셋 참조가 아닌 모듈 내부의 값이어야 합니다.");
            Assert.That(Attribute.IsDefined(field.FieldType, typeof(SerializableAttribute)), Is.True);
            Assert.That(modules[i].GetField("stats"), Is.Null);
        }
    }

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
    public void Food28KeepsLatestCreationAndContactSettingsInTheirOwnAssets()
    {
        const string path = "Assets/Data/Scriptable/Item/Effect/02.Food/Food 28 Yet/SummonSpawnEffect 1.asset";
        SummonSpawnEffect effect = AssetDatabase.LoadAssetAtPath<SummonSpawnEffect>(path);
        Assert.That(effect, Is.Not.Null);
        Assert.That(effect.spawnStat.lifetime, Is.EqualTo(1.1f));
        Assert.That(effect.spawnStat.SpawnCount, Is.EqualTo(1));
        Assert.That(effect.summonPrefab, Is.Not.Null);

        var contact = AssetDatabase.LoadAssetAtPath<SummonContactDamageModule>(
            "Assets/Data/Scriptable/Item/Effect/02.Food/Food 28 Yet/SummonContactDamage.asset");
        Assert.That(contact, Is.Not.Null);
        Assert.That(Array.Exists(effect.summonPrefab.modules, module => module == contact), Is.True);
        Assert.That(contact.baseStat, Is.Not.Null);
        Assert.That(contact.baseStat.damage, Is.EqualTo(5f), "최신 브랜치에서 사용자가 변경한 값을 보존합니다.");
        Assert.That(contact.baseStat.interval, Is.EqualTo(1f));
        Assert.That(contact.firstHitTiming, Is.EqualTo(SummonContactFirstHitTiming.AfterInterval));
        ItemData food = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Data/Scriptable/Item/Items/02.Food/Food28.asset");
        Assert.That(food, Is.Not.Null);
        Assert.That(Array.Exists(food.effectDatas, entry => entry == effect), Is.True, "이름이 바뀐 생성 효과에 Food28 연결을 이어야 합니다.");
    }
}
#endif
