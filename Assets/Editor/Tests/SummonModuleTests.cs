using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// 접촉 모듈이 피해 없이 HitEffect만 실행하는지 관찰합니다.
public sealed class SummonRecordingHitEffect : HitEffectData
{
    public readonly List<HitEffectContext> calls = new List<HitEffectContext>();
    public bool succeeds = true;
    protected override bool ApplyEffect(HitEffectContext context)
    { calls.Add(context); return succeeds; }
}

public sealed class SummonModuleTests
{
    private readonly List<Object> owned = new List<Object>();
    private readonly List<SummonActor> before = new List<SummonActor>();
    private BuffManager manager, previousManager;
    private static readonly Vector3 origin = new Vector3(25000f, 25000f, 0f);
    private T Asset<T>() where T : ScriptableObject
    { T value = ScriptableObject.CreateInstance<T>(); owned.Add(value); return value; }
    private GameObject Host()
    { var value = new GameObject("SummonModuleTest"); value.transform.position = origin; owned.Add(value); return value; }
    [SetUp]
    public void SetUp()
    {
        before.AddRange(SummonRegistry.Active);
        previousManager = BuffManager.instance;
        manager = Host().AddComponent<BuffManager>();
        if (manager.Storage == null) manager.SendMessage("Awake", SendMessageOptions.RequireReceiver);
    }
    [TearDown]
    public void TearDown()
    {
        // 주기적으로 생성한 자식도 정리하고, 테스트 전부터 있던 소환물은 유지합니다.
        foreach (SummonActor actor in new List<SummonActor>(SummonRegistry.Active))
            if (actor != null && !before.Contains(actor)) Object.DestroyImmediate(actor.gameObject);
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear(); before.Clear(); BuffManager.instance = previousManager;
    }
    private ItemEffectContext Context(ItemEffectLifetime lifetime = null)
        => new ItemEffectContext(null, Asset<ItemData>(), origin, origin, null,
            buffManager: manager, direction: Vector3.right) { lifetime = lifetime };
    private SummonActor Actor(params SummonBehaviourModule[] modules)
    {
        SummonActor actor = Host().AddComponent<SummonActor>();
        actor.enemyLayerMask = -1;
        actor.modules = modules;
        actor.Initialize(Context(), 100f);
        return actor;
    }
    private Enemy Enemy()
    {
        GameObject host = Host();
        host.AddComponent<Health>(); host.AddComponent<ActorTarget>();
        host.AddComponent<ActorMover>(); host.AddComponent<ActorAttack>();
        Enemy enemy = host.AddComponent<Enemy>();
        host.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
        enemy.health.Init(1000f); enemy.useDamagePopup = false;
        typeof(Enemy).GetField("isInitialized", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, true);
        host.AddComponent<CircleCollider2D>().radius = 0.1f;
        Physics2D.SyncTransforms();
        return enemy;
    }
    private SummonContactEffectModule Contact(SummonRecordingHitEffect effect)
    {
        var module = Asset<SummonContactEffectModule>();
        module.stats = Asset<SummonContactEffectStatDefinition>();
        module.stats.baseStat.radius = 1f;
        module.contactEffects = new HitEffectData[] { effect };
        module.limitActivations = true;
        module.stats.baseStat.activationCount = 2f;
        return module;
    }
    private SummonSpawnEffect Spawn(float count)
    {
        GameObject host = Host(); host.SetActive(false);
        var prefab = host.AddComponent<SummonActor>();
        var effect = Asset<SummonSpawnEffect>(); effect.summonPrefab = prefab;
        effect.spawnStat = new SummonSpawnStat { count = count, lifetime = 50f };
        return effect;
    }
    private ActiveBuff Buff(string type, string field, float add)
    {
        var modifier = Asset<FloatFieldBuffModifier>();
        modifier.targetStatTypeName = type; modifier.fieldName = field; modifier.addValue = add;
        var resolver = Asset<BuffTargetGroupResolver>(); resolver.targetGroup = "Summon";
        var buff = Asset<BuffEffect>(); buff.targetResolver = resolver;
        buff.includeSelf = true; buff.modifiers = new BuffModifier[] { modifier };
        buff.buffInfo.applyTiming = BuffApplyTiming.Dynamic;
        manager.RegisterBuff(buff, Context());
        return manager.Storage.activeBuffs.Find(value => value.sourceEffectData == buff);
    }

    [Test]
    public void DirectBuffChangesOnlyOneInstanceOfASharedModuleProfile()
    {
        var module = Asset<SummonContactDamageModule>(); module.stats = Asset<SummonContactDamageStatDefinition>();
        module.stats.baseStat.damage = 10f;
        SummonActor a = Actor(module), b = Actor(module);
        var modifier = Asset<FloatFieldBuffModifier>();
        modifier.targetStatTypeName = nameof(SummonContactDamageStat); modifier.fieldName = "damage"; modifier.addValue = 5f;
        var resolver = Asset<DirectBuffTargetResolver>(); resolver.targetComponent = a;
        var buff = Asset<BuffEffect>(); buff.targetResolver = resolver; buff.includeSelf = true;
        buff.modifiers = new BuffModifier[] { modifier }; buff.buffInfo.applyTiming = BuffApplyTiming.Dynamic;
        manager.RegisterBuff(buff, Context());
        Assert.That(a.GetRuntimeStat<SummonContactDamageStat>(0).damage, Is.EqualTo(15f));
        Assert.That(b.GetRuntimeStat<SummonContactDamageStat>(0).damage, Is.EqualTo(10f));
        Assert.That(module.stats.baseStat.damage, Is.EqualTo(10f));
        a.GetRuntimeStat<SummonContactDamageStat>(0).damage = 999f;
        Assert.That(a.GetRuntimeStat<SummonContactDamageStat>(0).damage, Is.EqualTo(15f), "확인 API는 런타임을 수정하는 참조를 노출하지 않습니다.");
    }

    [Test]
    public void ContactEffectsDoNotDealAutomaticDamageAndRespectReentryLimit()
    {
        Enemy enemy = Enemy(); var effect = Asset<SummonRecordingHitEffect>();
        var module = Contact(effect); SummonActor actor = Actor(module);
        actor.Advance(0f); actor.Advance(0.1f);
        Assert.That(effect.calls.Count, Is.EqualTo(1), "계속 겹쳐 있어도 OnEnter는 한 번만 실행합니다.");
        Assert.That(enemy.health.Hp, Is.EqualTo(1000f), "피해는 Contact Damage 모듈로 별도 부여합니다.");
        for (int i = 0; i < 2; i++)
        {
            enemy.transform.position = origin + Vector3.right * 10f; Physics2D.SyncTransforms(); actor.Advance(0.1f);
            enemy.transform.position = origin; Physics2D.SyncTransforms(); actor.Advance(0.1f);
        }
        Assert.That(effect.calls.Count, Is.EqualTo(2), "소환물당 총 2회 제한입니다.");
    }
    [Test]
    public void RepeatContactsHaveIndependentRuntimesAndFailedEffectsDoNotSpendCount()
    {
        Enemy(); var effect = Asset<SummonRecordingHitEffect>(); effect.succeeds = false;
        var module = Contact(effect); module.triggerMode = SummonContactTriggerMode.WhileTouching;
        module.stats.baseStat.repeatInterval = 0.5f; module.stats.baseStat.activationCount = 1f;
        SummonActor a = Actor(module), b = Actor(module);
        a.Advance(0f); b.Advance(0f); Assert.That(effect.calls.Count, Is.EqualTo(2));
        effect.succeeds = true; a.Advance(0.5f); b.Advance(0.5f);
        a.Advance(0.5f); b.Advance(0.5f);
        Assert.That(effect.calls.Count, Is.EqualTo(4));
    }
    [Test]
    public void ContactDamageBuffChangesActualEnemyHpWithoutChangingProfile()
    {
        Enemy enemy = Enemy(); var module = Asset<SummonContactDamageModule>();
        module.stats = Asset<SummonContactDamageStatDefinition>();
        module.stats.baseStat = new SummonContactDamageStat { damage = 10f, radius = 1f, interval = 0.5f };
        SummonActor actor = Actor(module); ActiveBuff buff = Buff(nameof(SummonContactDamageStat), "damage", 5f);
        Assert.That(actor.GetRuntimeStat<SummonContactDamageStat>(0).damage, Is.EqualTo(15f));
        actor.Advance(0f); Assert.That(enemy.health.Hp, Is.EqualTo(985f));
        manager.RemoveBuffHandle(buff); actor.Advance(0.5f);
        Assert.That(enemy.health.Hp, Is.EqualTo(975f));
        Assert.That(module.stats.baseStat.damage, Is.EqualTo(10f));
    }
    [Test]
    public void PeriodicModuleAlternatesDifferentSpawnCountsAndStopsAtCycleLimit()
    {
        var first = Spawn(1f); var second = Spawn(2f);
        var module = Asset<SummonPeriodicSpawnModule>(); module.stats = Asset<SummonPeriodicSpawnStatDefinition>();
        module.stats.baseStat.interval = 1f; module.stats.baseStat.activationCount = 2f;
        module.limitActivations = true; module.spawnEffects = new[] { first, second };
        SummonActor actor = Actor(module); int initial = SummonRegistry.Active.Count;
        actor.Advance(0.5f); Assert.That(SummonRegistry.Active.Count, Is.EqualTo(initial));
        actor.Advance(0.5f); Assert.That(SummonRegistry.Active.Count, Is.EqualTo(initial + 1));
        actor.Advance(1f); Assert.That(SummonRegistry.Active.Count, Is.EqualTo(initial + 3));
        actor.Advance(1f); Assert.That(SummonRegistry.Active.Count, Is.EqualTo(initial + 3));
        Assert.That(module.stats.baseStat.activationCount, Is.EqualTo(2f));
    }
    [Test]
    public void ZeroSpawnCycleDoesNotSpendLimitAndCreationBuffCanEnableTheNextCycle()
    {
        var spawn = Spawn(0f);
        var module = Asset<SummonPeriodicSpawnModule>(); module.stats = Asset<SummonPeriodicSpawnStatDefinition>();
        module.limitActivations = true; module.stats.baseStat.activationCount = 1f; module.spawnEffects = new[] { spawn };
        SummonActor actor = Actor(module); int initial = SummonRegistry.Active.Count;
        actor.Advance(1f); Assert.That(SummonRegistry.Active.Count, Is.EqualTo(initial));
        var modifier = Asset<FloatFieldBuffModifier>();
        modifier.targetStatTypeName = nameof(SummonSpawnStat); modifier.fieldName = "count"; modifier.addValue = 2f;
        var buff = Asset<BuffEffect>(); buff.includeSelf = true; buff.targetResolver = Asset<AllItemsBuffTargetResolver>();
        buff.modifiers = new BuffModifier[] { modifier }; buff.buffInfo.applyTiming = BuffApplyTiming.Dynamic;
        manager.RegisterBuff(buff, Context());
        actor.Advance(1f); Assert.That(SummonRegistry.Active.Count, Is.EqualTo(initial + 2));
        actor.Advance(1f); Assert.That(SummonRegistry.Active.Count, Is.EqualTo(initial + 2));
        Assert.That(spawn.spawnStat.count, Is.Zero, "생성 버프도 원본 스탯을 수정하지 않습니다.");
    }

    [Test]
    public void DynamicPeriodicBuffChangesIntervalAndLimitForExistingInstance()
    {
        var module = Asset<SummonPeriodicSpawnModule>(); module.stats = Asset<SummonPeriodicSpawnStatDefinition>();
        module.stats.baseStat.interval = 2f; module.stats.baseStat.activationCount = 1f;
        module.limitActivations = true; module.spawnEffects = new[] { Spawn(1f) };
        SummonActor actor = Actor(module); int initial = SummonRegistry.Active.Count;
        actor.Advance(1f); Assert.That(SummonRegistry.Active.Count, Is.EqualTo(initial));
        Buff(nameof(SummonPeriodicSpawnStat), "interval", -1f);
        Buff(nameof(SummonPeriodicSpawnStat), "activationCount", 1f);
        actor.Advance(0f); actor.Advance(1f);
        Assert.That(SummonRegistry.Active.Count, Is.EqualTo(initial + 2));
    }
    [Test]
    public void CancelledPeriodicSourceProducesNoChildrenAndNaturalParentCompletionKeepsChildren()
    {
        var module = Asset<SummonPeriodicSpawnModule>(); module.stats = Asset<SummonPeriodicSpawnStatDefinition>();
        module.spawnEffects = new[] { Spawn(1f) };
        SummonActor actor = Actor(module); var lifetime = new ItemEffectLifetime();
        actor.Initialize(Context(lifetime), 100f); int initial = SummonRegistry.Active.Count;
        actor.Advance(1f); Assert.That(SummonRegistry.Active.Count, Is.EqualTo(initial + 1));
        actor.Despawn(true); Assert.That(SummonRegistry.Active.Count, Is.EqualTo(initial));
        foreach (SummonActor child in new List<SummonActor>(SummonRegistry.Active))
            if (!before.Contains(child) && child != null && child.CanAct) Assert.That(child.RemainingLifeTime, Is.EqualTo(50f));
        lifetime.Close(false);
        foreach (SummonActor child in new List<SummonActor>(SummonRegistry.Active))
            if (!before.Contains(child) && child != null && child.gameObject.activeSelf) child.Advance(0f);
        Assert.That(SummonRegistry.Active.Count, Is.EqualTo(before.Count));
    }
    [Test]
    public void HealthModuleBuffAndProfileRebuildPreserveDamageAndRemoveHealthWhenModuleIsRemoved()
    {
        var module = Asset<SummonHealthModule>(); module.stats = Asset<SummonHealthStatDefinition>();
        module.stats.baseStat = new SummonHealthStat { maxHealth = 100f, defense = 3f };
        SummonActor actor = Actor(module); actor.TakeDamage(13f);
        Assert.That(actor.Health.Hp, Is.EqualTo(90f));
        ActiveBuff buff = Buff(nameof(SummonHealthStat), "maxHealth", 50f);
        Assert.That(actor.Health.MaxHp, Is.EqualTo(150f)); Assert.That(actor.Health.Hp, Is.EqualTo(90f));
        actor.ConfigureModules(new SummonBehaviourModule[] { module });
        Assert.That(actor.Health.Hp, Is.EqualTo(90f), "프로필 재생성은 새 생명이 아니므로 체력을 채우면 안 됩니다.");
        manager.RemoveBuffHandle(buff); Assert.That(actor.Health.MaxHp, Is.EqualTo(100f));
        Assert.That(module.stats.baseStat.maxHealth, Is.EqualTo(100f));
        actor.ConfigureModules(new SummonBehaviourModule[0]); Assert.That(actor.Health, Is.Null);
    }
}
