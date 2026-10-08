using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class AdditionalHealthTests
{
    private readonly List<Object> owned = new List<Object>();
    private BuffManager previousManager;
    private T Asset<T>() where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();
        owned.Add(asset);
        return asset;
    }
    private GameObject Host()
    {
        GameObject host = new GameObject("AdditionalHealthTest");
        owned.Add(host);
        return host;
    }
    private Health Target(float hp = 100f)
    {
        Health health = Host().AddComponent<Health>();
        health.Init(hp);
        return health;
    }
    private ItemEffectContext Context(Health health)
        => new ItemEffectContext(health.gameObject, null, Vector3.zero, Vector3.right, null);

    [SetUp]
    public void Setup()
    {
        previousManager = BuffManager.instance;
        Host().AddComponent<BuffManager>();
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
    public void OrdinaryDamageAppliesDefenseThenShieldAndReportsActualLoss()
    {
        Health health = Target(10f);
        health.defenseFormula = HealthDefenseFormula.FlatReduction;
        health.SetBaseDefense(3f);
        health.AddShield(15f, 10f, ShieldReapplyMode.Refresh, null, Context(health));
        float popupDamage = 0f;
        HealthDamageResult result = default(HealthDamageResult);
        health.OnDamaged += amount => popupDamage = amount;
        health.OnDamageResolved += value => result = value;
        Assert.That(health.PreviewDamageToHp(20f), Is.EqualTo(2f));
        Assert.That(health.ShieldAmount, Is.EqualTo(15f));
        health.TakeDamage(20f);
        Assert.That(health.Hp, Is.EqualTo(8f));
        Assert.That(health.ShieldAmount, Is.Zero);
        Assert.That(popupDamage, Is.EqualTo(2f));
        Assert.That(result.damageAfterDefense, Is.EqualTo(17f));
        Assert.That(result.absorbedByShield, Is.EqualTo(15f));
        Assert.That(result.hpDamage, Is.EqualTo(2f));
    }

    [Test]
    public void BeamHealingSelectsOnlyAlliesAheadInsideTheRectangle()
    {
        Health inside = Target(), behind = Target(), outside = Target();
        inside.team = behind.team = outside.team = HealthTeam.Ally;
        inside.transform.position = new Vector3(2f, 0.1f, 0f);
        behind.transform.position = new Vector3(-1f, 0f, 0f);
        outside.transform.position = new Vector3(2f, 0.3f, 0f);
        HealthTargetSettings settings = new HealthTargetSettings {
            target = HealthTargetMode.AllAllies, useRadius = true,
            shape = HealthTargetShape.DirectionalRectangle, width = 0.4f, length = 5f
        };
        ItemEffectContext context = new ItemEffectContext(null, null, Vector3.zero, Vector3.zero, null, direction: Vector3.right);
        List<Health> targets = new List<Health>(); settings.Resolve(context, targets);
        Assert.That(targets, Does.Contain(inside));
        Assert.That(targets, Does.Not.Contain(behind));
        Assert.That(targets, Does.Not.Contain(outside));
    }

    [Test]
    public void PercentDefenseCannotTurnDamageIntoHealing()
    {
        Health health = Target();
        health.defenseFormula = HealthDefenseFormula.PercentReduction;
        health.SetBaseDefense(200f);
        health.TakeDamage(20f);
        Assert.That(health.Hp, Is.EqualTo(100f));
        health.SetBaseDefense(25f);
        health.TakeDamage(20f);
        Assert.That(health.Hp, Is.EqualTo(85f));
    }

    [Test]
    public void CurrentHpCostUsesExplicitBypassAndCannotKillPolicy()
    {
        Health health = Target(10f);
        health.defenseFormula = HealthDefenseFormula.FlatReduction;
        health.SetBaseDefense(100f);
        health.AddShield(50f, 10f, ShieldReapplyMode.Refresh, null, Context(health));
        HealthChangeEffect effect = Asset<HealthChangeEffect>();
        effect.targets.target = HealthTargetMode.Owner;
        effect.kind = HealthChangeKind.Cost;
        effect.amountMode = HealthAmountMode.CurrentHpPercent;
        effect.healthStat.healthChangeAmount = 20f;
        effect.damagePolicy.canKill = false;
        effect.damagePolicy.bypassDefense = true;
        effect.damagePolicy.bypassShield = true;
        effect.damagePolicy.notifyDamaged = false;
        ItemEffectContext context = Context(health);
        context.damageMultiplier = 0.5f;
        int hits = 0;
        health.OnDamaged += amount => hits++;
        effect.Execute(context);
        Assert.That(health.Hp, Is.EqualTo(8f));
        Assert.That(health.ShieldAmount, Is.EqualTo(50f));
        health.ApplyDamage(100f, effect.damagePolicy);
        Assert.That(health.Hp, Is.EqualTo(1f));
        Assert.That(health.IsDead, Is.False);
        Assert.That(hits, Is.Zero);
    }

    [Test]
    public void HealingScalesOnceAndReportsOnlyRecoveredHealth()
    {
        Health health = Target(100f);
        health.TakeDamage(8f);
        float healed = 0f;
        health.OnHealed += amount => healed = amount;
        HealthChangeEffect effect = Asset<HealthChangeEffect>();
        effect.targets.target = HealthTargetMode.Owner;
        effect.healthStat.healthChangeAmount = 15f;
        ItemEffectContext context = Context(health);
        context.healingMultiplier = 0.5f;
        effect.Execute(context);
        Assert.That(health.Hp, Is.EqualTo(99.5f));
        Assert.That(healed, Is.EqualTo(7.5f));
        effect.Execute(Context(health));
        Assert.That(health.Hp, Is.EqualTo(100f));
        Assert.That(healed, Is.EqualTo(0.5f));
        Assert.That(effect.healthStat.healthChangeAmount, Is.EqualTo(15f));
    }

    [Test]
    public void MaxHpRefreshDoesNotBuffRuntimeHpOrStackMaximum()
    {
        Health health = Target(100f);
        health.TakeDamage(40f);
        HealthStat buffed = new HealthStat { maxHp = 150f, hp = 150f };
        health.ApplyBuffedStat(buffed);
        health.ApplyBuffedStat(buffed);
        Assert.That(health.Hp, Is.EqualTo(60f));
        Assert.That(health.MaxHp, Is.EqualTo(150f));
        health.Heal(90f);
        health.RefreshBuffedStat();
        Assert.That(health.MaxHp, Is.EqualTo(100f));
        Assert.That(health.Hp, Is.EqualTo(100f));
        health.RefreshBuffedStat();
        Assert.That(health.MaxHp, Is.EqualTo(100f));
    }

    [Test]
    public void FillingMaxHpIncreaseOccursOnlyOncePerActualIncrease()
    {
        Health health = Target(100f);
        health.TakeDamage(40f);
        health.maxHealthIncreasePolicy = MaxHealthIncreasePolicy.AddIncrease;
        HealthStat buffed = new HealthStat { maxHp = 150f, hp = 150f };
        health.ApplyBuffedStat(buffed);
        health.ApplyBuffedStat(buffed);
        Assert.That(health.Hp, Is.EqualTo(110f));
    }

    [Test]
    public void ShieldRefreshAndAddShareExpiryAndRetainBothCompletionLeases()
    {
        Health health = Target();
        ShieldEffect source = Asset<ShieldEffect>();
        int firstCompleted = 0, secondCompleted = 0;
        ItemEffectContext first = Context(health), second = Context(health);
        first.lifetime = new ItemEffectLifetime(onCompleted: () => firstCompleted++);
        second.lifetime = new ItemEffectLifetime(onCompleted: () => secondCompleted++);
        health.AddShield(15f, 10f, ShieldReapplyMode.Refresh, source, first);
        first.lifetime.Close();
        health.TakeDamage(5f);
        health.TickShields(5f);
        health.AddShield(15f, 10f, ShieldReapplyMode.Refresh, source, second);
        second.lifetime.Close();
        health.AddShield(7f, 10f, ShieldReapplyMode.Add, source, Context(health));
        Assert.That(health.ShieldAmount, Is.EqualTo(22f));
        health.TickShields(9f);
        Assert.That(firstCompleted + secondCompleted, Is.Zero);
        health.TickShields(1f);
        Assert.That(health.ShieldAmount, Is.Zero);
        Assert.That(firstCompleted, Is.EqualTo(1));
        Assert.That(secondCompleted, Is.EqualTo(1));
    }

    [Test]
    public void RegenerationWaitsFirstIntervalAndHealsFiveTicksAtFiveSeconds()
    {
        Health health = Target();
        health.TakeDamage(20f);
        RegenerationEffect effect = Asset<RegenerationEffect>();
        effect.targets.target = HealthTargetMode.Owner;
        int finished = 0;
        ItemEffectContext context = Context(health);
        context.lifetime = new ItemEffectLifetime(onCompleted: () => finished++);
        effect.Execute(context);
        context.lifetime.Close();
        HealthRegenerationRunner runner = health.GetComponent<HealthRegenerationRunner>();
        runner.Tick(0.99f);
        Assert.That(health.Hp, Is.EqualTo(80f));
        Assert.That(finished, Is.Zero);
        runner.Tick(0.01f);
        Assert.That(health.Hp, Is.EqualTo(81f));
        runner.Tick(4f);
        Assert.That(health.Hp, Is.EqualTo(85f));
        Assert.That(runner.ActiveCount, Is.Zero);
        Assert.That(finished, Is.EqualTo(1));
    }

    [Test]
    public void ReinitializationCancelsRegenerationAndShieldFromPriorLife()
    {
        Health health = Target();
        health.TakeDamage(20f);
        RegenerationEffect effect = Asset<RegenerationEffect>();
        effect.targets.target = HealthTargetMode.Owner;
        effect.Execute(Context(health));
        health.AddShield(15f, 10f, ShieldReapplyMode.Refresh, null, Context(health));
        int oldLife = health.LifeId;
        health.Init(30f);
        health.TakeDamage(10f);
        health.GetComponent<HealthRegenerationRunner>().Tick(5f);
        Assert.That(health.LifeId, Is.Not.EqualTo(oldLife));
        Assert.That(health.Hp, Is.EqualTo(20f));
        Assert.That(health.ShieldAmount, Is.Zero);
    }

    [Test]
    public void DamageEventCannotKillTheNewLifeCreatedByCallback()
    {
        Health health = Target(10f);
        int deaths = 0;
        health.OnDead += () => deaths++;
        health.OnDamageResolved += result => health.Init(30f);
        health.TakeDamage(20f);
        Assert.That(health.Hp, Is.EqualTo(30f));
        Assert.That(health.IsDead, Is.False);
        Assert.That(deaths, Is.Zero);
    }

    [Test]
    public void ShieldCompletionCallbackCanClearOtherShieldsSafely()
    {
        Health health = Target(10f);
        ItemEffectContext first = Context(health);
        first.lifetime = new ItemEffectLifetime(onCompleted: () => health.Init(30f));
        health.AddShield(1f, 1f, ShieldReapplyMode.Refresh, Asset<ShieldEffect>(), first);
        first.lifetime.Close();
        health.AddShield(2f, 1f, ShieldReapplyMode.Refresh, Asset<ShieldEffect>(), Context(health));
        Assert.DoesNotThrow(() => health.TickShields(1f));
        Assert.That(health.Hp, Is.EqualTo(30f));
        Assert.That(health.ShieldAmount, Is.Zero);
    }

    [Test]
    public void ShieldsGrantedByCompletionDoNotAbsorbTheSameHit()
    {
        Health health = Target(100f);
        ItemEffectContext first = Context(health);
        first.lifetime = new ItemEffectLifetime(onCompleted: () =>
            health.AddShield(100f, 10f, ShieldReapplyMode.Refresh, null, Context(health)));
        health.AddShield(1f, 10f, ShieldReapplyMode.Refresh, null, first);
        first.lifetime.Close();
        health.TakeDamage(10f);
        Assert.That(health.Hp, Is.EqualTo(91f));
        Assert.That(health.ShieldAmount, Is.EqualTo(100f));
    }

    [Test]
    public void ExecutionRespectsThresholdAndExplicitImmunity()
    {
        GameObject host = Host();
        Enemy enemy = host.AddComponent<Enemy>();
        Health health = host.AddComponent<Health>();
        enemy.health = health;
        typeof(Enemy).GetField("isInitialized", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, true);
        health.Init(100f);
        ExecuteHitEffectData effect = Asset<ExecuteHitEffectData>();
        health.TakeDamage(89f);
        Assert.That(effect.TryExecute(new HitEffectContext(enemy, Context(health))), Is.False);
        health.TakeDamage(1f);
        health.executionImmune = true;
        Assert.That(effect.TryExecute(new HitEffectContext(enemy, Context(health))), Is.False);
        health.executionImmune = false;
        health.AddShield(100f, 10f, ShieldReapplyMode.Refresh, null, Context(health));
        Assert.That(effect.TryExecute(new HitEffectContext(enemy, Context(health))), Is.True);
        Assert.That(health.IsDead, Is.True);
    }

    [Test]
    public void DamageResultPreservesSourceSnapshotAndTransferFlag()
    {
        Health health = Target();
        ItemEffectContext context = Context(health);
        context.targetPosition = new Vector3(2f, 3f);
        HealthDamageResult result = default(HealthDamageResult);
        health.OnDamageResolved += value => result = value;
        health.ApplyDamage(2f, new HealthDamagePolicy { sourceContext = context, isTransferredDamage = true });
        context.targetPosition = Vector3.zero;
        Assert.That(result.sourceContext.targetPosition, Is.EqualTo(new Vector3(2f, 3f)));
        Assert.That(result.sourceContext.owner, Is.SameAs(health.gameObject));
        Assert.That(result.isTransferredDamage, Is.True);
    }
}
