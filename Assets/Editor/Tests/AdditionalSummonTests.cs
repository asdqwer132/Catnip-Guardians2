using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class AdditionalSummonTests
{
    private readonly List<Object> owned = new List<Object>();
    private BuffManager previousManager;
    [SetUp]
    public void SetUp()
    {
        previousManager = BuffManager.instance;
        BuffManager.instance = null;
    }
    private T Asset<T>() where T : ScriptableObject
    {
        T result = ScriptableObject.CreateInstance<T>();
        owned.Add(result);
        return result;
    }
    private GameObject Host()
    {
        GameObject result = new GameObject("AdditionalSummonTests");
        owned.Add(result);
        return result;
    }
    private ItemEffectContext Context(GameObject owner = null)
        => new ItemEffectContext(owner, Asset<ItemData>(), Vector3.zero, Vector3.zero, null);
    private SummonActor Summon(SummonDefinition definition = null, GameObject owner = null)
    {
        SummonActor result = Host().AddComponent<SummonActor>();
        result.definition = definition;
        result.Initialize(Context(owner), 100f);
        return result;
    }
    [TearDown]
    public void Cleanup()
    {
        for (int i = owned.Count - 1; i >= 0; i--)
            if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
        BuffManager.instance = previousManager;
    }

    [Test]
    public void RegistryUsesDefinitionOwnerAndActiveLifeInsteadOfObjectName()
    {
        GameObject owner = Host(), other = Host();
        SummonDefinition turret = Asset<SummonDefinition>(), flower = Asset<SummonDefinition>();
        SummonActor selected = Summon(turret, owner);
        selected.name = "FlowerName";
        Summon(turret, other);
        Summon(flower, owner);
        var selection = new SummonSelection { definitions = new[] { turret } };
        var matches = new List<SummonActor>();
        ItemEffectContext context = Context(owner);
        SummonRegistry.Collect(selection, context, matches);
        Assert.That(matches, Is.EquivalentTo(new[] { selected }));
        int life = selected.LifeId;
        selected.gameObject.SetActive(false);
        SummonRegistry.Collect(selection, context, matches);
        Assert.That(matches, Is.Empty);
        selected.gameObject.SetActive(true);
        Assert.That(selected.LifeId, Is.Not.EqualTo(life));
    }

    [Test]
    public void ExpiredCoveredProfileNeverReturnsAfterTopProfileExpires()
    {
        SummonActor summon = Summon();
        SummonBehaviourModule original = Asset<SummonOrbitModule>();
        SummonBehaviourModule shortProfile = Asset<SummonPullModule>();
        SummonBehaviourModule longProfile = Asset<SummonContactDamageModule>();
        summon.ConfigureModules(new[] { original });
        summon.AddModification(new SummonModificationSettings
        { replaceModules = true, modules = new[] { shortProfile }, duration = 1f }, Context());
        summon.AddModification(new SummonModificationSettings
        { replaceModules = true, modules = new[] { longProfile }, duration = 2f }, Context());
        summon.Advance(1.1f);
        Assert.That(summon.ActiveModules[0], Is.SameAs(longProfile));
        summon.Advance(1f);
        Assert.That(summon.ActiveModules[0], Is.SameAs(original));
    }

    [Test]
    public void AttackCountModificationConsumesOnlySuccessfulAttackNotifications()
    {
        SummonActor summon = Summon();
        summon.AddModification(new SummonModificationSettings { damageMultiplier = 2f, attackCount = 1 }, Context());
        summon.Advance(1f);
        Assert.That(summon.AttackCount, Is.Zero);
        Assert.That(summon.CalculateDamage(8f), Is.EqualTo(16f));
        Assert.That(summon.ThrowItem(null, Vector3.zero), Is.False);
        Assert.That(summon.CalculateDamage(8f), Is.EqualTo(16f));
        summon.NotifyAttack();
        Assert.That(summon.AttackCount, Is.EqualTo(1));
        Assert.That(summon.CalculateDamage(8f), Is.EqualTo(8f));
    }

    [Test]
    public void ItemProfileRestoresAndSharedConfigurationRemainsUnchanged()
    {
        SummonActor summon = Summon();
        ItemData original = Asset<ItemData>(), upgraded = Asset<ItemData>();
        SummonItemThrowModule module = Asset<SummonItemThrowModule>();
        module.item = original;
        summon.ConfigureModules(new[] { module });
        SummonModificationSettings settings = new SummonModificationSettings
        { replaceAttackItem = true, attackItem = upgraded, duration = 1f, damageMultiplier = 2f };
        summon.AddModification(settings, Context());
        settings.damageMultiplier = 100f;
        Assert.That(summon.ResolveAttackItem(original), Is.SameAs(upgraded));
        Assert.That(summon.CalculateDamage(5f), Is.EqualTo(10f));
        Assert.That(module.item, Is.SameAs(original));
        summon.Advance(1.1f);
        Assert.That(summon.ResolveAttackItem(original), Is.SameAs(original));
        Assert.That(summon.CalculateDamage(5f), Is.EqualTo(5f));
    }

    [Test]
    public void TransformRequiresDistinctParticipantsAndCannotReuseNonConsumedBones()
    {
        SummonDefinition bone = Asset<SummonDefinition>();
        SummonActor first = Summon(bone);
        SummonTransformEffect transform = Asset<SummonTransformEffect>();
        transform.requirements = new[] {
            new SummonTransformRequirement { definition = bone },
            new SummonTransformRequirement { definition = bone }
        };
        transform.result = Asset<SummonSpawnEffect>();
        transform.result.summonPrefab = Summon(Asset<SummonDefinition>());
        transform.result.spawnStat = new SummonSpawnStat { lifetime = 100f };
        ItemEffectContext context = Context();
        Assert.That(transform.consumeParticipants, Is.False);
        Assert.That(transform.TryTransform(context, out _), Is.False);
        SummonActor second = Summon(bone);
        Assert.That(transform.TryTransform(context, out SummonActor created), Is.True);
        owned.Add(created.gameObject);
        Assert.That(first.CanAct && second.CanAct, Is.True);
        Assert.That(transform.TryTransform(context, out _), Is.False);
        Assert.That(first.HasTransformed(transform), Is.True);
    }

    [Test]
    public void PooledSummonDropsPreviousRuntimeModifiers()
    {
        SummonActor summon = Summon();
        summon.AddModification(new SummonModificationSettings { damageMultiplier = 3f }, Context());
        Assert.That(summon.CalculateDamage(2f), Is.EqualTo(6f));
        summon.gameObject.SetActive(false);
        summon.gameObject.SetActive(true);
        summon.Initialize(Context(), 100f);
        Assert.That(summon.CalculateDamage(2f), Is.EqualTo(2f));
        Assert.That(summon.AttackCount, Is.Zero);
    }

    [Test]
    public void TwoAurasRemoveOnlyTheirOwnBuffWhenEmitterIsRemoved()
    {
        BuffManager manager = Host().AddComponent<BuffManager>();
        FloatFieldBuffModifier modifier = Asset<FloatFieldBuffModifier>();
        modifier.fieldName = nameof(SummonContactDamageStat.damage);
        modifier.addValue = 5f;
        BuffEffect buff = Asset<BuffEffect>();
        buff.modifiers = new BuffModifier[] { modifier };
        SummonDefinition turret = Asset<SummonDefinition>();
        SummonActor target = Summon(turret);
        SummonContactDamageModule contact = Asset<SummonContactDamageModule>();
        contact.stats = Asset<SummonContactDamageStatDefinition>();
        contact.stats.baseStat.damage = 0f;
        target.ConfigureModules(new SummonBehaviourModule[] { contact });
        target.Initialize(new ItemEffectContext(null, Asset<ItemData>(), Vector3.zero, Vector3.zero,
            null, buffManager: manager), 100f);
        SummonAuraModule aura = Asset<SummonAuraModule>();
        aura.buff = buff;
        aura.targets.definitions = new[] { turret };
        SummonActor first = Summon(), second = Summon();
        ItemEffectContext firstSource = new ItemEffectContext(null, Asset<ItemData>(), Vector3.zero, Vector3.zero, null, buffManager: manager)
        { lifetime = new ItemEffectLifetime() };
        ItemEffectContext secondSource = new ItemEffectContext(null, Asset<ItemData>(), Vector3.zero, Vector3.zero, null, buffManager: manager)
        { lifetime = new ItemEffectLifetime() };
        first.Initialize(firstSource, 100f);
        second.Initialize(secondSource, 100f);
        SummonBehaviourRuntime a = aura.CreateRuntime(first), b = aura.CreateRuntime(second);
        try
        {
            a.Tick(0.1f); b.Tick(0.1f);
            SummonContactDamageStat boosted = manager.GetBuffedStatForTarget(new SummonContactDamageStat { damage = 0f }, target);
            Assert.That(boosted.damage, Is.EqualTo(10f));
            Assert.That(target.GetRuntimeStat<SummonContactDamageStat>(0).damage, Is.EqualTo(10f), "실제 소환물의 최종 기능 스탯도 갱신되어야 합니다.");
            a.Dispose();
            Assert.That(firstSource.CanContinue, Is.True);
            boosted = manager.GetBuffedStatForTarget(new SummonContactDamageStat { damage = 0f }, target);
            Assert.That(boosted.damage, Is.EqualTo(5f));
            Assert.That(target.GetRuntimeStat<SummonContactDamageStat>(0).damage, Is.EqualTo(5f));
            target.transform.position = Vector3.one * 100f;
            b.Tick(0.1f);
            Assert.That(secondSource.CanContinue, Is.True);
            boosted = manager.GetBuffedStatForTarget(new SummonContactDamageStat { damage = 0f }, target);
            Assert.That(boosted.damage, Is.Zero);
            Assert.That(target.GetRuntimeStat<SummonContactDamageStat>(0).damage, Is.Zero);
        }
        finally { a.Dispose(); b.Dispose(); firstSource.lifetime.Close(); secondSource.lifetime.Close(); }
    }

    [Test]
    public void ThrownItemReceivesSummonDamageOnceWithoutMutatingSharedItem()
    {
        SummonActor summon = Summon();
        ItemEffectContext parent = Context();
        parent.damageMultiplier = 2f;
        summon.Initialize(parent, 100f);
        summon.AddModification(new SummonModificationSettings { damageMultiplier = 3f }, Context());
        CombatEffectRecordingEffect record = Asset<CombatEffectRecordingEffect>();
        ItemData item = Asset<ItemData>();
        item.effectDatas = new ItemEffectData[] { record };
        Assert.That(summon.ThrowItem(item, summon.transform.position, damageOverride: 8f), Is.True);
        ItemEffectContext execution = record.calls[0];
        Assert.That(execution.damageMultiplier, Is.EqualTo(6f));
        Assert.That(execution.damageOverride, Is.EqualTo(8f));
        DamageAreaAttackEffect attack = Asset<DamageAreaAttackEffect>();
        DamageAreaAttackStat shared = new DamageAreaAttackStat { damageAreaPower = 10f };
        Assert.That(execution.GetSnapshotStat(attack, shared).damageAreaPower, Is.EqualTo(48f));
        Assert.That(shared.damageAreaPower, Is.EqualTo(10f));
        Assert.That(item.effectDatas[0], Is.SameAs(record));
    }

    [Test]
    public void NaturalModifierCompletionCanReenterWithoutLosingNewProfile()
    {
        SummonActor summon = Summon();
        int completed = 0;
        ItemEffectContext context = Context();
        context.lifetime = new ItemEffectLifetime(onCompleted: () =>
        {
            completed++;
            summon.AddModification(new SummonModificationSettings { damageMultiplier = 3f }, Context());
        });
        summon.AddModification(new SummonModificationSettings { duration = 1f, damageMultiplier = 2f }, context);
        context.lifetime.Close();
        summon.Advance(1.1f);
        Assert.That(completed, Is.EqualTo(1));
        Assert.That(summon.CalculateDamage(2f), Is.EqualTo(6f));
    }

    [Test]
    public void ForcedSummonDisableCancelsModifierCompletion()
    {
        SummonActor summon = Summon();
        int completed = 0;
        ItemEffectContext context = Context();
        context.lifetime = new ItemEffectLifetime(onCompleted: () => completed++);
        summon.AddModification(new SummonModificationSettings { duration = 10f }, context);
        context.lifetime.Close();
        summon.gameObject.SetActive(false);
        Assert.That(completed, Is.Zero);
        Assert.That(context.lifetime.IsCancelled, Is.True);
    }
}
