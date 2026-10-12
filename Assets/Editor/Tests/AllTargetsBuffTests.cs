using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class AllTargetsBuffTests
{
    private readonly List<Object> owned = new List<Object>();
    private BuffManager manager, previousManager;

    private T Asset<T>() where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();
        owned.Add(asset);
        return asset;
    }

    private GameObject Host(string name)
    {
        var host = new GameObject(name);
        owned.Add(host);
        return host;
    }

    [SetUp]
    public void SetUp()
    {
        previousManager = BuffManager.instance;
        manager = Host("All Targets Test").AddComponent<BuffManager>();
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

    private FloatFieldBuffModifier Modifier(string type, string field, float add = 0f, float multiply = 0f)
    {
        FloatFieldBuffModifier modifier = Asset<FloatFieldBuffModifier>();
        modifier.targetStatTypeName = type;
        modifier.fieldName = field;
        modifier.addValue = add;
        modifier.multiplyValue = multiply;
        return modifier;
    }

    private BuffEffect GlobalEffect(BuffApplyTiming timing, params BuffModifier[] modifiers)
    {
        BuffEffect effect = Asset<BuffEffect>();
        effect.targetResolver = Asset<AllTargetsBuffTargetResolver>();
        effect.buffInfo.useLimitType = BuffUseLimitType.Infinite;
        effect.buffInfo.applyTiming = timing;
        effect.includeSelf = true;
        effect.modifiers = modifiers;
        return effect;
    }

    private ItemEffectContext Context(ItemData item, ItemEffectData effect)
        => new ItemEffectContext(null, item, Vector3.zero, Vector3.zero, null, effect, manager);

    private ActiveBuff Grant(BuffEffect effect, ItemData source = null)
    {
        if (source == null) source = Asset<ItemData>();
        manager.RegisterBuff(effect, Context(source, effect));
        ActiveBuff record = manager.Storage.activeBuffs.Find(buff => buff.sourceEffectData == effect);
        Assert.That(record, Is.Not.Null);
        return record;
    }

    private sealed class CustomTarget : IBuffTarget
    {
        private readonly BuffManager manager;
        private readonly GameObject host;
        private readonly string group;
        public HealthStat current;
        public int refreshes;
        public CustomTarget(BuffManager manager, GameObject host, string group)
        { this.manager = manager; this.host = host; this.group = group; }
        public Object BuffTargetObject => host;
        public string BuffTargetGroup => group;
        public string BuffTargetDebugName => "Custom Test Target";
        public void RefreshBuffedStat()
        {
            refreshes++;
            current = manager.GetBuffedStatForTarget(new HealthStat { maxHp = 10f }, this);
        }
    }

    [Test]
    public void UniversalRecordCoversItemsBagsExistingCustomGroupsAndLaterTargets()
    {
        // 그룹 이름이 없는 대상도 전체 범위에 들어가야 합니다.
        var existing = new CustomTarget(manager, Host("Existing Target"), "");
        manager.RegisterBuffTarget(existing);
        BuffEffect effect = GlobalEffect(BuffApplyTiming.Dynamic,
            Modifier(nameof(ItemCooldownStat), nameof(ItemCooldownStat.cooldown), add: -1f),
            Modifier(nameof(BagCooldownStat), nameof(BagCooldownStat.cooldown), add: -2f),
            Modifier(nameof(HealthStat), nameof(HealthStat.maxHp), add: 5f));
        Grant(effect);

        Assert.That(manager.Storage.activeBuffs.Count, Is.EqualTo(1), "대상마다 별도 버프를 만들지 않습니다.");
        Assert.That(existing.current.maxHp, Is.EqualTo(15f), "기존 대상도 변경 알림을 받아야 합니다.");
        ItemData item = Asset<ItemData>();
        EquipmentBag bag = Host("Bag").AddComponent<EquipmentBag>();
        Assert.That(manager.GetBuffedStatForItem(new ItemCooldownStat { cooldown = 5f }, item, bag).cooldown,
            Is.EqualTo(4f));
        Assert.That(manager.GetBuffedStat(new BagCooldownStat { cooldown = 5f }, BuffQueryContext.ForBag(bag)).cooldown,
            Is.EqualTo(3f));
        var future = new CustomTarget(manager, Host("Future Target"), "NewCustomGroup/Child");
        manager.RegisterBuffTarget(future);
        Assert.That(future.current.maxHp, Is.EqualTo(15f));
        Assert.That(manager.GetBuffedStatForItem(new DamageAreaAttackStat { damageAreaPower = 5f }, item, bag)
            .damageAreaPower, Is.EqualTo(5f), "Modifier에서 선택하지 않은 스탯 타입은 바뀌지 않습니다.");

        manager.RemoveBuffHandle(manager.Storage.activeBuffs[0]);
        Assert.That(existing.current.maxHp, Is.EqualTo(10f));
        Assert.That(future.current.maxHp, Is.EqualTo(10f));
    }

    private DamageArea Area(ItemData item, float damageMultiplier = 1f)
    {
        ItemEffectContext context = Context(item, Asset<DamageAreaAttackEffect>());
        context.damageMultiplier = damageMultiplier;
        // 실제 아이템 실행과 같은 Snapshot -> 오브젝트 동적 조회 경로를 사용합니다.
        DamageAreaAttackStat snapshot = context.GetUnscaledSnapshotStat(context.currentEffectData,
            new DamageAreaAttackStat { damageAreaPower = 5f, damageAreaLifeTime = 60f });
        DamageArea area = Host("Damage Area").AddComponent<DamageArea>();
        area.BindLifetime(context);
        area.InitWithSnapshotAndDynamicBuff(snapshot, item, null, manager, null);
        return area;
    }

    private static float Damage(DamageArea area)
        => (float)typeof(DamageArea).GetField("damage", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(area);

    [TestCase(BuffApplyTiming.Dynamic)]
    [TestCase(BuffApplyTiming.Snapshot)]
    public void ItemAndDamageAreaQueriesApplyUniversalBuffOnlyOnceIncludingRefreshAndScaling(BuffApplyTiming timing)
    {
        ActiveBuff buff = Grant(GlobalEffect(timing,
            Modifier(nameof(DamageAreaAttackStat), nameof(DamageAreaAttackStat.damageAreaPower), multiply: 0.5f)));
        DamageArea area = Area(Asset<ItemData>(), damageMultiplier: 2f);
        // 5 × 1.5 × 실행 배율 2 = 15. 대상 계산에서 다시 1.5를 곱한 22.5가 되면 안 됩니다.
        Assert.That(Damage(area), Is.EqualTo(15f).Within(0.0001f));
        area.RefreshBuffedStat();
        area.RefreshBuffedStat();
        Assert.That(Damage(area), Is.EqualTo(15f).Within(0.0001f));

        manager.RemoveBuffHandle(buff);
        float expected = timing == BuffApplyTiming.Dynamic ? 10f : 15f;
        Assert.That(Damage(area), Is.EqualTo(expected).Within(0.0001f), "캡처한 Snapshot은 해제 후에도 유지됩니다.");
    }

    [TestCase(BuffApplyTiming.Dynamic)]
    [TestCase(BuffApplyTiming.Snapshot)]
    public void ItemAndSummonQueriesApplyUniversalBuffOnlyOnce(BuffApplyTiming timing)
    {
        Grant(GlobalEffect(timing,
            Modifier(nameof(SummonActorStat), nameof(SummonActorStat.attackPower), multiply: 0.5f)));
        ItemData item = Asset<ItemData>();
        ItemEffectContext context = Context(item, Asset<SummonSpawnEffect>());
        SummonActor summon = Host("Summon").AddComponent<SummonActor>();
        summon.baseActorStat.attackPower = 5f;
        summon.Initialize(context, 60f);
        Assert.That(summon.AttackPower, Is.EqualTo(7.5f).Within(0.0001f));
        summon.RefreshBuffedStat();
        Assert.That(summon.AttackPower, Is.EqualTo(7.5f).Within(0.0001f));
    }

    [Test]
    public void DynamicGlobalBuffUpdatesExistingAndFutureDamageAreasAndRestoresOnRemoval()
    {
        ItemData item = Asset<ItemData>();
        DamageArea existing = Area(item);
        ActiveBuff buff = Grant(GlobalEffect(BuffApplyTiming.Dynamic,
            Modifier(nameof(DamageAreaAttackStat), nameof(DamageAreaAttackStat.damageAreaPower), multiply: 0.5f)));
        DamageArea future = Area(item);
        Assert.That(Damage(existing), Is.EqualTo(7.5f));
        Assert.That(Damage(future), Is.EqualTo(7.5f));
        manager.RemoveBuffHandle(buff);
        Assert.That(Damage(existing), Is.EqualTo(5f));
        Assert.That(Damage(future), Is.EqualTo(5f));
    }

    [Test]
    public void IncludeSelfFalseExcludesTheSourceItemAndItsAttackObjects()
    {
        ItemData source = Asset<ItemData>();
        BuffEffect effect = GlobalEffect(BuffApplyTiming.Dynamic,
            Modifier(nameof(DamageAreaAttackStat), nameof(DamageAreaAttackStat.damageAreaPower), multiply: 0.5f));
        effect.includeSelf = false;
        Grant(effect, source);
        Assert.That(Damage(Area(source)), Is.EqualTo(5f));
        Assert.That(Damage(Area(Asset<ItemData>())), Is.EqualTo(7.5f));
    }

    [Test]
    public void IndependentBuffOriginsStackAndSameOriginReapplicationDoesNotDoubleTheSnapshot()
    {
        BuffEffect first = GlobalEffect(BuffApplyTiming.Snapshot,
            Modifier(nameof(DamageAreaAttackStat), nameof(DamageAreaAttackStat.damageAreaPower), multiply: 0.5f));
        ItemData source = Asset<ItemData>();
        ActiveBuff record = Grant(first, source);
        Grant(GlobalEffect(BuffApplyTiming.Snapshot,
            Modifier(nameof(DamageAreaAttackStat), nameof(DamageAreaAttackStat.damageAreaPower), multiply: 0.5f)));
        DamageArea captured = Area(Asset<ItemData>());
        Assert.That(Damage(captured), Is.EqualTo(11.25f));
        first.buffInfo.stackMode = BuffStackMode.Stack;
        first.buffInfo.maxStack = 3;
        Grant(first, source);
        Assert.That(record.stack, Is.EqualTo(2));
        Assert.That(Damage(captured), Is.EqualTo(11.25f), "이전 공격의 Snapshot은 다시 중첩하지 않습니다.");
        // 첫 출처 2중첩 × 두 번째 출처 1중첩: 5 × 1.5² × 1.5 = 16.875.
        Assert.That(Damage(Area(Asset<ItemData>())), Is.EqualTo(16.875f));
    }

    [Test]
    public void OneUseGlobalBuffKeepsLastAttackDamageAndConsumesOnceAcrossTargets()
    {
        BuffEffect effect = GlobalEffect(BuffApplyTiming.Dynamic,
            Modifier(nameof(DamageAreaAttackStat), nameof(DamageAreaAttackStat.damageAreaPower), multiply: 0.5f));
        effect.buffInfo.useLimitType = BuffUseLimitType.UseCount;
        effect.buffInfo.maxUseCount = 1;
        Grant(effect);
        ItemData weapon = Asset<ItemData>();
        BuffItemUseToken token = manager.BeginItemUse(weapon, null);
        DamageArea lastAttack;
        try { lastAttack = Area(weapon); }
        finally { manager.EndItemUse(token); }
        Assert.That(manager.Storage.activeBuffs.Count, Is.EqualTo(0));
        lastAttack.RefreshBuffedStat();
        Assert.That(Damage(lastAttack), Is.EqualTo(7.5f), "마지막 1회도 생성된 공격의 Snapshot에 남아야 합니다.");
        Assert.That(Damage(Area(weapon)), Is.EqualTo(5f));
    }

    [Test]
    public void UniversalCleanseRemovesMatchingGlobalAndSpecificRecords()
    {
        BuffEffect global = GlobalEffect(BuffApplyTiming.Dynamic);
        global.harmful = true;
        Grant(global);
        BuffEffect group = GlobalEffect(BuffApplyTiming.Dynamic);
        BuffTargetGroupResolver resolver = Asset<BuffTargetGroupResolver>();
        resolver.targetGroup = "DamageArea";
        group.targetResolver = resolver;
        group.harmful = true;
        Grant(group);
        // 유익한 버프는 기본 정화 조건에서 남습니다. 수치 Modifier로 유효한 페이로드를 만듭니다.
        Grant(GlobalEffect(BuffApplyTiming.Dynamic,
            Modifier(nameof(ItemCooldownStat), nameof(ItemCooldownStat.cooldown), add: 1f)));
        Assert.That(manager.Cleanse(BuffTargetHandle.AllTargets()), Is.EqualTo(2));
        Assert.That(manager.Storage.activeBuffs.Count, Is.EqualTo(1));
        Assert.That(manager.Storage.activeBuffs[0].harmful, Is.False);
    }
}
