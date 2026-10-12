using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// 테스트용 모듈입니다. 두 소환물이 같은 모듈 에셋을 써도 Runtime은 따로 생성되는지 확인합니다.
public sealed class SummonSpawnProbeModule : SummonBehaviourModule
{
    public int preparations;
    public readonly List<ProbeRuntime> runtimes = new List<ProbeRuntime>();
    public override void Prepare(ItemEffectContext context) { preparations++; }
    public override SummonBehaviourRuntime CreateRuntime(SummonActor actor)
    {
        var runtime = new ProbeRuntime(actor);
        runtimes.Add(runtime);
        return runtime;
    }
    public sealed class ProbeRuntime : SummonBehaviourRuntime
    {
        public float elapsed;
        public bool disposed;
        public ProbeRuntime(SummonActor actor) : base(actor) { }
        public override void Tick(float deltaTime) { elapsed += deltaTime; }
        public override void Dispose() { disposed = true; }
    }
}

public sealed class SummonSpawnTests
{
    private readonly List<Object> owned = new List<Object>();
    private BuffManager manager, previousManager;
    private T Asset<T>() where T : ScriptableObject
    {
        T result = ScriptableObject.CreateInstance<T>();
        owned.Add(result);
        return result;
    }
    private GameObject Host(string name)
    {
        var result = new GameObject(name);
        owned.Add(result);
        return result;
    }
    [SetUp]
    public void SetUp()
    {
        previousManager = BuffManager.instance;
        manager = Host("Summon Spawn Tests").AddComponent<BuffManager>();
        if (manager.Storage == null) manager.SendMessage("Awake", SendMessageOptions.RequireReceiver);
    }
    [TearDown]
    public void TearDown()
    {
        for (int i = owned.Count - 1; i >= 0; i--)
            if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
        BuffManager.instance = previousManager;
    }
    private SummonSpawnEffect Effect(float count = 1f)
    {
        GameObject prefab = Host("Test Summon Prefab");
        prefab.SetActive(false);
        SummonActor actor = prefab.AddComponent<SummonActor>();
        actor.baseActorStat = new SummonActorStat { attackPower = 10f, detectionRange = 4f, attackInterval = 2f };
        SummonSpawnEffect effect = Asset<SummonSpawnEffect>();
        effect.summonPrefab = actor;
        effect.spawnStat.count = count;
        return effect;
    }
    private ItemEffectContext Context(ItemData source = null)
        => new ItemEffectContext(null, source != null ? source : Asset<ItemData>(),
            Vector3.zero, new Vector3(10f, 20f, 9f), null, buffManager: manager, direction: Vector3.right);
    private List<SummonActor> Spawn(SummonSpawnEffect effect, ItemEffectContext context = null)
    {
        var actors = new List<SummonActor>();
        effect.Spawn(context ?? Context(), actors);
        foreach (SummonActor actor in actors) owned.Add(actor.gameObject);
        return actors;
    }
    private FloatFieldBuffModifier Modifier(string type, string field, float add = 0f, float multiply = 0f)
    {
        FloatFieldBuffModifier modifier = Asset<FloatFieldBuffModifier>();
        modifier.targetStatTypeName = type;
        modifier.fieldName = field;
        modifier.addValue = add;
        modifier.multiplyValue = multiply;
        return modifier;
    }
    private ActiveBuff Grant(BuffTargetResolver resolver, params BuffModifier[] modifiers)
    {
        BuffEffect effect = Asset<BuffEffect>();
        effect.targetResolver = resolver;
        effect.modifiers = modifiers;
        effect.includeSelf = true;
        effect.buffInfo.useLimitType = BuffUseLimitType.Infinite;
        effect.buffInfo.applyTiming = BuffApplyTiming.Dynamic;
        manager.RegisterBuff(effect, Context());
        return manager.Storage.activeBuffs.Find(buff => buff.sourceEffectData == effect);
    }

    [Test]
    public void SamePrefabKeepsOwnFunctionStatsAcrossEffectsWithDifferentCreationSettings()
    {
        SummonSpawnEffect first = Effect(2f), second = Asset<SummonSpawnEffect>();
        second.summonPrefab = first.summonPrefab;
        first.spawnStat.lifetime = 3f;
        second.spawnStat.lifetime = 9f;
        List<SummonActor> a = Spawn(first), b = Spawn(second);
        Assert.That(a.Count, Is.EqualTo(2));
        Assert.That(b.Count, Is.EqualTo(1));
        foreach (SummonActor actor in a) Assert.That(actor.RemainingLifeTime, Is.EqualTo(3f));
        Assert.That(b[0].RemainingLifeTime, Is.EqualTo(9f));
        foreach (SummonActor actor in a) Assert.That(actor.AttackPower, Is.EqualTo(10f));
        Assert.That(b[0].AttackPower, Is.EqualTo(10f));
        Assert.That(first.summonPrefab.baseActorStat.attackPower, Is.EqualTo(10f));
        Assert.That(first.spawnStat.count, Is.EqualTo(2f));
    }

    [Test]
    public void ItemBuffChangesCountSpreadAndLifetimeWhileActorSettingsRemainIndependent()
    {
        Grant(Asset<AllItemsBuffTargetResolver>(),
            Modifier(nameof(SummonSpawnStat), nameof(SummonSpawnStat.count), add: 2f),
            Modifier(nameof(SummonSpawnStat), nameof(SummonSpawnStat.spreadRadius), add: 2f),
            Modifier(nameof(SummonSpawnStat), nameof(SummonSpawnStat.lifetime), add: 5f));
        SummonSpawnEffect effect = Effect();
        effect.placementMode = AttackPlacementMode.CircleEven;
        List<SummonActor> actors = Spawn(effect);
        Assert.That(actors.Count, Is.EqualTo(3));
        foreach (SummonActor actor in actors)
        {
            Assert.That(Vector2.Distance(actor.transform.position, new Vector2(10f, 20f)), Is.EqualTo(2f).Within(0.0001f));
            Assert.That(actor.transform.position.z, Is.Zero);
            Assert.That(actor.RemainingLifeTime, Is.EqualTo(10f));
            Assert.That(actor.AttackPower, Is.EqualTo(10f));
        }
        Assert.That(effect.spawnStat.count, Is.EqualTo(1f), "원본 생성 에셋은 변경하지 않습니다.");
        Assert.That(effect.spawnStat.lifetime, Is.EqualTo(5f));
    }

    [Test]
    public void SummonGroupBuffUpdatesExistingAndFutureActorsWithoutChangingSpawnCount()
    {
        SummonSpawnEffect effect = Effect();
        SummonActor existing = Spawn(effect)[0];
        BuffTargetGroupResolver group = Asset<BuffTargetGroupResolver>();
        group.targetGroup = "Summon";
        ActiveBuff buff = Grant(group,
            Modifier(nameof(SummonActorStat), nameof(SummonActorStat.attackPower), multiply: 0.5f),
            Modifier(nameof(SummonActorStat), nameof(SummonActorStat.attackInterval), multiply: -0.5f));
        Assert.That(existing.AttackPower, Is.EqualTo(15f));
        Assert.That(existing.AttackInterval, Is.EqualTo(1f));
        List<SummonActor> future = Spawn(effect);
        Assert.That(future.Count, Is.EqualTo(1));
        Assert.That(future[0].AttackPower, Is.EqualTo(15f));
        manager.RemoveBuffHandle(buff);
        Assert.That(existing.AttackPower, Is.EqualTo(10f));
        Assert.That(future[0].AttackPower, Is.EqualTo(10f));
        Assert.That(effect.summonPrefab.baseActorStat.attackPower, Is.EqualTo(10f));
    }

    [Test]
    public void AllTargetsBuffAppliesEachStatOnceAndExecutionScalesAlsoApplyOnce()
    {
        Grant(Asset<AllTargetsBuffTargetResolver>(),
            Modifier(nameof(SummonSpawnStat), nameof(SummonSpawnStat.count), add: 1f),
            Modifier(nameof(SummonActorStat), nameof(SummonActorStat.attackPower), multiply: 0.5f));
        SummonSpawnEffect effect = Effect();
        effect.spawnStat.spreadRadius = 1f;
        ItemEffectContext context = Context();
        context.damageMultiplier = 2f;
        context.rangeMultiplier = 3f;
        context.durationMultiplier = 2f;
        List<SummonActor> actors = Spawn(effect, context);
        Assert.That(actors.Count, Is.EqualTo(2));
        foreach (SummonActor actor in actors)
        {
            Assert.That(actor.AttackPower, Is.EqualTo(15f), "아이템 조회와 소환물 조회에서 두 번 곱하면 안 됩니다.");
            Assert.That(actor.CalculateDamage(actor.AttackPower), Is.EqualTo(30f));
            Assert.That(actor.AttackRange, Is.EqualTo(12f));
            Assert.That(actor.RemainingLifeTime, Is.EqualTo(10f));
            Assert.That(Vector2.Distance(actor.transform.position, new Vector2(10f, 20f)), Is.EqualTo(3f).Within(0.0001f));
        }
    }

    [Test]
    public void LastUseBuffCoversTheWholeBatchAndRestoresOnlyFutureSpawns()
    {
        FloatFieldBuffModifier modifier = Modifier(nameof(SummonActorStat), nameof(SummonActorStat.attackPower), multiply: 0.5f);
        BuffEffect buff = Asset<BuffEffect>();
        buff.targetResolver = Asset<AllTargetsBuffTargetResolver>();
        buff.modifiers = new BuffModifier[] { modifier };
        buff.includeSelf = true;
        buff.buffInfo.useLimitType = BuffUseLimitType.UseCount;
        buff.buffInfo.maxUseCount = 1;
        manager.RegisterBuff(buff, Context());
        SummonSpawnEffect effect = Effect(3f);
        ItemEffectContext context = Context();
        BuffItemUseToken token = manager.BeginItemUse(context.sourceItemData, null);
        List<SummonActor> actors;
        try { actors = Spawn(effect, context); }
        finally { manager.EndItemUse(token); }
        Assert.That(manager.Storage.activeBuffs, Is.Empty);
        foreach (SummonActor actor in actors)
        {
            actor.RefreshBuffedStat();
            Assert.That(actor.AttackPower, Is.EqualTo(15f), "마지막 사용의 모든 소환물이 버프를 유지해야 합니다.");
        }
        foreach (SummonActor actor in Spawn(effect)) Assert.That(actor.AttackPower, Is.EqualTo(10f));
    }

    [Test]
    public void EffectPreparationLeavesModulesToActorsAndEveryInstanceGetsIndependentRuntimes()
    {
        SummonSpawnEffect effect = Effect(2f);
        SummonSpawnProbeModule a = Asset<SummonSpawnProbeModule>(), b = Asset<SummonSpawnProbeModule>();
        effect.summonPrefab.modules = new SummonBehaviourModule[] { a, b };
        ItemEffectContext context = Context();
        effect.Prepare(context);
        Assert.That(a.preparations, Is.Zero, "생성 효과는 소환물 기능을 실행하거나 준비하지 않습니다.");
        List<SummonActor> actors = Spawn(effect, context);
        Assert.That(actors[0].ActiveModules.Count, Is.EqualTo(2));
        Assert.That(a.runtimes.Count, Is.EqualTo(2));
        Assert.That(b.runtimes.Count, Is.EqualTo(2));
        actors[0].Advance(0.25f);
        Assert.That(a.runtimes[0].elapsed, Is.EqualTo(0.25f));
        Assert.That(b.runtimes[0].elapsed, Is.EqualTo(0.25f));
        Assert.That(a.runtimes[1].elapsed, Is.Zero);
        Assert.That(b.runtimes[1].elapsed, Is.Zero);
    }

    [Test]
    public void NaturalCompletionWaitsForEverySpawnedActorAndCancellationSuppressesCompletion()
    {
        SummonSpawnEffect effect = Effect(2f);
        int completed = 0;
        ItemEffectContext context = Context();
        context.lifetime = new ItemEffectLifetime(onCompleted: () => completed++);
        List<SummonActor> actors = Spawn(effect, context);
        context.lifetime.Close();
        actors[0].Despawn(true);
        Assert.That(completed, Is.Zero);
        actors[1].Despawn(true);
        Assert.That(completed, Is.EqualTo(1));

        ItemEffectContext cancelled = Context();
        cancelled.lifetime = new ItemEffectLifetime(onCompleted: () => completed++);
        List<SummonActor> pending = Spawn(effect, cancelled);
        cancelled.lifetime.Close(false);
        foreach (SummonActor actor in pending) actor.Advance(0f);
        Assert.That(completed, Is.EqualTo(1));
        foreach (SummonActor actor in pending) Assert.That(actor == null, Is.True);
    }

    [Test]
    public void FourSummonsUseUnityRotationToOccupyDistinctCardinalPositions()
    {
        SummonSpawnEffect effect = Effect(4f);
        effect.spawnStat.spreadRadius = 2f;
        effect.spreadStartMode = AttackSpreadStartMode.FromDirection;
        List<SummonActor> actors = Spawn(effect);
        var expected = new[] { new Vector3(12f, 20f), new Vector3(10f, 22f),
            new Vector3(8f, 20f), new Vector3(10f, 18f) };
        for (int i = 0; i < actors.Count; i++)
            Assert.That(Vector3.Distance(actors[i].transform.position, expected[i]), Is.LessThan(0.0001f));
    }

    [Test]
    public void ZeroCountAndMissingPrefabProduceNoActors()
    {
        SummonSpawnEffect effect = Effect(0f);
        Assert.That(Spawn(effect), Is.Empty);
        effect.spawnStat.count = 2f;
        effect.summonPrefab = null;
        Assert.That(Spawn(effect), Is.Empty);
    }
}
