using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class AdditionalEnemyControlTests
{
    private readonly List<Object> owned = new List<Object>();
    private readonly List<IDisposable> handles = new List<IDisposable>();
    private GameObject Host()
    {
        GameObject host = new GameObject("AdditionalEnemyControlTest");
        owned.Add(host);
        return host;
    }
    private Enemy Actor()
    {
        GameObject host = Host();
        Health health = host.AddComponent<Health>();
        health.Init(100f);
        host.AddComponent<ActorTarget>();
        host.AddComponent<ActorMover>();
        host.AddComponent<ActorAttack>();
        Enemy enemy = host.AddComponent<Enemy>();
        enemy.useDamagePopup = false;
        Set(enemy, "isInitialized", true);
        Set(enemy.mover, "enemyOwner", enemy);
        Set(enemy.attack, "enemyOwner", enemy);
        return enemy;
    }
    private ItemEffectContext Context(ItemEffectLifetime lifetime = null) =>
        new ItemEffectContext(null, null, Vector3.zero, Vector3.zero, null) { lifetime = lifetime };
    private T Hold<T>(T handle) where T : IDisposable { handles.Add(handle); return handle; }
    private static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static T Get<T>(object target, string name) =>
        (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Call(object target, string name, params object[] args) =>
        target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);

    [TearDown]
    public void Cleanup()
    {
        foreach (IDisposable handle in handles) handle.Dispose();
        handles.Clear();
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
    }

    [Test]
    public void RootStopsMovementButAllowsDefaultAttackAndExpiresIndependently()
    {
        Enemy enemy = Actor();
        Assert.That(enemy.statusController.ApplyRoot(1f), Is.True);
        Assert.That(enemy.IsRooted, Is.True);
        Assert.That(enemy.CanRunDefaultActions, Is.True);
        Assert.That(enemy.mover.IsMoveStopped, Is.False);
        enemy.mover.MoveBy(Vector2.right * 3f);
        Assert.That(enemy.transform.position, Is.EqualTo(Vector3.zero));
        enemy.statusController.TickStatuses(1f);
        Assert.That(enemy.IsRooted, Is.False);
        enemy.mover.MoveBy(Vector2.right * 3f);
        Assert.That(enemy.transform.position.x, Is.EqualTo(3f));
    }

    [Test]
    public void StackedRootExpiryDoesNotReleaseStunOrManagerDisable()
    {
        Enemy enemy = Actor();
        enemy.statusController.ApplyRoot(1f);
        enemy.statusController.ApplyRoot(3f);
        enemy.DisableAction();
        enemy.statusController.ApplyStun(4f);
        enemy.statusController.TickStatuses(1f);
        Assert.That(enemy.IsRooted, Is.True);
        Assert.That(enemy.statusController.ActiveRootCount, Is.EqualTo(1));
        enemy.statusController.TickStatuses(2f);
        Assert.That(enemy.IsRooted, Is.False);
        Assert.That(enemy.IsStunned, Is.True);
        Assert.That(enemy.IsActionDisabled, Is.True);
        Assert.That(enemy.mover.IsMoveStopped, Is.True);
        enemy.statusController.TickStatuses(1f);
        Assert.That(enemy.IsStunned, Is.False);
        Assert.That(enemy.IsFullyStopped, Is.True);
    }

    [Test]
    public void RootCancellationAndPooledLifeDoNotAffectNewLife()
    {
        Enemy enemy = Actor();
        ItemEffectLifetime scope = new ItemEffectLifetime();
        enemy.statusController.ApplyRoot(10f, new HitEffectContext(enemy, Context(scope)));
        scope.Close(false);
        enemy.statusController.TickStatuses(0f);
        Assert.That(enemy.IsRooted, Is.False);
        Assert.That(scope.IsFinished, Is.True);
        enemy.statusController.ApplyRoot(10f);
        enemy.OnReturnedToPool();
        Assert.That(enemy.IsRooted, Is.False);
        Assert.That(enemy.statusController.ActiveRootCount, Is.Zero);
    }

    [Test]
    public void TargetOverrideRestoresNextValidPriorityThenBaseTarget()
    {
        Enemy enemy = Actor(), original = Actor(), low = Actor(), high = Actor();
        enemy.actorTarget.SetTarget(original);
        Hold(enemy.actorTarget.AddTargetOverride(low, 10));
        TargetOverrideHandle highest = Hold(enemy.actorTarget.AddTargetOverride(high, 20));
        Assert.That(enemy.actorTarget.TargetDamageable, Is.SameAs(high));
        highest.Dispose();
        Assert.That(enemy.actorTarget.TargetDamageable, Is.SameAs(low));
        low.OnReturnedToPool();
        Assert.That(enemy.actorTarget.TargetDamageable, Is.SameAs(original));
    }

    [Test]
    public void TargetOverrideTimerAndCancellationReleaseOnlyTheirHandle()
    {
        Enemy enemy = Actor(), original = Actor(), low = Actor(), high = Actor();
        enemy.actorTarget.SetTarget(original);
        enemy.statusController.ApplyTargetOverride(low, 5f, 10);
        ItemEffectLifetime scope = new ItemEffectLifetime();
        enemy.statusController.ApplyTargetOverride(high, 10f, 20, new HitEffectContext(enemy, Context(scope)));
        scope.Close(false);
        enemy.statusController.TickStatuses(0f);
        Assert.That(enemy.actorTarget.TargetDamageable, Is.SameAs(low));
        enemy.statusController.TickStatuses(5f);
        Assert.That(enemy.actorTarget.TargetDamageable, Is.SameAs(original));
    }

    [Test]
    public void TimeStopStackAndTargetFlagsReleaseIndependently()
    {
        float before = Time.timeScale;
        TimeStopHandle actions = Hold(TimeStopRuntime.Acquire(TimeStopTargets.EnemyActions));
        TimeStopHandle overlap = Hold(TimeStopRuntime.Acquire(TimeStopTargets.EnemyActions | TimeStopTargets.EnemyProjectiles));
        actions.Dispose();
        Assert.That(TimeStopRuntime.IsStopped(TimeStopTargets.EnemyActions), Is.True);
        Assert.That(TimeStopRuntime.IsStopped(TimeStopTargets.EnemyProjectiles), Is.True);
        Assert.That(TimeStopRuntime.IsStopped(TimeStopTargets.EnemySpawning), Is.False);
        overlap.Dispose();
        Assert.That(TimeStopRuntime.IsStopped(TimeStopTargets.EnemyActions), Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(before));
    }

    [Test]
    public void TimeStopKeepsProjectileMotionLifetimeAndDamageFrozen()
    {
        EnemySimpleProjectile projectile = Host().AddComponent<EnemySimpleProjectile>();
        projectile.Init(null, Vector2.right, 5f, 3f, 10f, ~0);
        projectile.canPierce = true;
        projectile.destroyOnHit = false;
        projectile.pierceCount = 10;
        Enemy victim = Actor();
        Collider2D collider = victim.gameObject.AddComponent<CircleCollider2D>();
        TimeStopHandle stop = Hold(TimeStopRuntime.Acquire(TimeStopTargets.EnemyProjectiles));
        projectile.Tick(4f);
        Call(projectile, "OnTriggerEnter2D", collider);
        Assert.That(victim.health.Hp, Is.EqualTo(100f));
        Assert.That(projectile.transform.position, Is.EqualTo(Vector3.zero));
        Assert.That(Get<float>(projectile, "lifeTime"), Is.EqualTo(10f));
        stop.Dispose();
        projectile.Tick(2f);
        Assert.That(projectile.transform.position.x, Is.EqualTo(6f));
        Assert.That(Get<float>(projectile, "lifeTime"), Is.EqualTo(8f));
        Call(projectile, "OnTriggerEnter2D", collider);
        Assert.That(victim.health.Hp, Is.EqualTo(95f));
    }

    [Test]
    public void TimeStopBlocksPatternClockAndMoverWithoutChangingStopReasons()
    {
        Enemy enemy = Actor();
        EnemyPatternRunner runner = enemy.gameObject.AddComponent<EnemyPatternRunner>();
        runner.Init(enemy);
        enemy.patternRunner = runner;
        Set(runner, "patternCooldownTimer", 3f);
        TimeStopHandle stop = Hold(TimeStopRuntime.Acquire(TimeStopTargets.EnemyActions));
        Assert.That(runner.CanAdvancePattern, Is.False);
        runner.TickPattern();
        Assert.That(Get<float>(runner, "patternCooldownTimer"), Is.EqualTo(3f));
        enemy.mover.MoveBy(Vector2.right * 2f);
        Assert.That(enemy.transform.position, Is.EqualTo(Vector3.zero));
        enemy.DisableAction();
        enemy.statusController.ApplyStun(10f);
        stop.Dispose();
        Assert.That(enemy.IsStunned, Is.True);
        Assert.That(enemy.IsFullyStopped, Is.True);
    }

    [Test]
    public void StatusTimerFreezeIsExplicitAndStillHonorsCancellation()
    {
        Enemy enemy = Actor();
        ItemEffectLifetime scope = new ItemEffectLifetime();
        enemy.statusController.ApplyRoot(1f, new HitEffectContext(enemy, Context(scope)));
        TimeStopHandle stop = Hold(TimeStopRuntime.Acquire(TimeStopTargets.EnemyStatusTimers));
        enemy.statusController.TickStatuses(3f);
        Assert.That(enemy.IsRooted, Is.True);
        scope.Close(false);
        enemy.statusController.TickStatuses(0f);
        Assert.That(enemy.IsRooted, Is.False);
        stop.Dispose();
    }


    [Test]
    public void TimeStopAnimationReleaseKeepsOtherPauseReasonAndOriginalSpeed()
    {
        GameObject host = Host();
        Animator animator = host.AddComponent<Animator>();
        ActorVisual visual = host.AddComponent<ActorVisual>();
        animator.speed = 0.5f;
        visual.SetPatternAnimationPaused(true);
        visual.SetTimeStopAnimationPaused(true);
        visual.SetPatternAnimationPaused(false);
        Assert.That(animator.speed, Is.Zero);
        visual.SetTimeStopAnimationPaused(false);
        Assert.That(animator.speed, Is.EqualTo(0.5f));
        animator.speed = 0f;
        visual.SetTimeStopAnimationPaused(true);
        visual.SetTimeStopAnimationPaused(false);
        Assert.That(animator.speed, Is.Zero);
    }

    [Test]
    public void TimeStopNaturalTimerAdvancesWhileEnemyActionsAreFrozen()
    {
        ItemEffectLifetime scope = new ItemEffectLifetime();
        ItemEffectContext context = Context(scope);
        context.durationMultiplier = 2f;
        TimeStopEffect effect = ScriptableObject.CreateInstance<TimeStopEffect>();
        owned.Add(effect);
        effect.duration = 2f;
        effect.targets = TimeStopTargets.EnemyActions;
        effect.ExecuteEffect(context);
        TimeStopRunner runner = Object.FindAnyObjectByType<TimeStopRunner>();
        owned.Add(runner.gameObject);
        scope.Close();
        Assert.That(runner.RemainingTime, Is.EqualTo(4f));
        runner.Tick(3f);
        Assert.That(TimeStopRuntime.IsStopped(TimeStopTargets.EnemyActions), Is.True);
        Assert.That(scope.IsFinished, Is.False);
        runner.Tick(1f);
        Assert.That(TimeStopRuntime.IsStopped(TimeStopTargets.EnemyActions), Is.False);
        Assert.That(scope.IsFinished, Is.True);
    }

    [Test]
    public void MarkDamageReactionKeepsCreatorAndCapturedDamageSourceWhenConfigured()
    {
        Enemy victim = Actor();
        BuffManager manager = Host().AddComponent<BuffManager>();
        GameObject creator = Host(), attacker = Host();
        CombatEffectRecordingEffect recording = ScriptableObject.CreateInstance<CombatEffectRecordingEffect>();
        MarkHitEffectData mark = ScriptableObject.CreateInstance<MarkHitEffectData>();
        owned.Add(recording);
        owned.Add(mark);
        StatusDefinition key = ScriptableObject.CreateInstance<StatusDefinition>();
        owned.Add(key);
        mark.markKey = key;
        mark.onDamagedEffects = new ItemEffectData[] { recording };
        mark.reactionSource = MarkReactionSource.DamageSource;
        ItemEffectContext markContext = Context();
        markContext.owner = creator;
        markContext.buffManager = manager;
        Assert.That(mark.TryExecute(new HitEffectContext(victim, markContext)), Is.True);
        ItemEffectContext damageContext = Context();
        damageContext.owner = attacker;
        victim.TakeDamage(3f, damageContext);
        Assert.That(recording.calls.Count, Is.EqualTo(1));
        Assert.That(recording.calls[0].owner, Is.SameAs(attacker));
    }

    [Test]
    public void MarkNaturalExpiryAndCancellationHaveDifferentReactions()
    {
        Enemy victim = Actor();
        CombatEffectRecordingEffect recording = ScriptableObject.CreateInstance<CombatEffectRecordingEffect>();
        MarkHitEffectData mark = ScriptableObject.CreateInstance<MarkHitEffectData>();
        owned.Add(recording);
        owned.Add(mark);
        BuffManager manager = Host().AddComponent<BuffManager>();
        StatusDefinition key = ScriptableObject.CreateInstance<StatusDefinition>();
        owned.Add(key);
        key.harmful = true;
        mark.markKey = key;
        mark.statusInfo.duration = 1f;
        mark.onNaturalExpiryEffects = new ItemEffectData[] { recording };
        ItemEffectContext source = Context();
        source.buffManager = manager;
        Assert.That(mark.TryExecute(new HitEffectContext(victim, source)), Is.True);
        MarkStatusController controller = victim.GetComponent<MarkStatusController>();
        Assert.That(manager.GetStatusStack(key, BuffQueryContext.ForTarget(victim)), Is.EqualTo(1));
        new BuffTicker(manager.Storage).Tick(1f);
        Assert.That(recording.calls.Count, Is.EqualTo(1));
        ItemEffectLifetime scope = new ItemEffectLifetime();
        ItemEffectContext cancelled = Context(scope);
        cancelled.buffManager = manager;
        Assert.That(mark.TryExecute(new HitEffectContext(victim, cancelled)), Is.True);
        scope.Close(false);
        controller.Tick();
        Assert.That(recording.calls.Count, Is.EqualTo(1));
    }

    [Test]
    public void CancelledExecutionCannotLeaveGlobalTimeStopEnabled()
    {
        ItemEffectLifetime scope = new ItemEffectLifetime();
        Hold(TimeStopRuntime.Acquire(TimeStopTargets.EnemyActions, Context(scope)));
        Assert.That(TimeStopRuntime.IsStopped(TimeStopTargets.EnemyActions), Is.True);
        scope.Close(false);
        Assert.That(TimeStopRuntime.IsStopped(TimeStopTargets.EnemyActions), Is.False);
    }
}
