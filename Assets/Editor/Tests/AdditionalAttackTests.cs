using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class AdditionalAttackTests
{
    private readonly List<Object> owned = new List<Object>();
    private T Asset<T>() where T : ScriptableObject
    { T asset = ScriptableObject.CreateInstance<T>(); owned.Add(asset); return asset; }
    private Enemy EnemyAt(Vector3 position, float hp = 10f)
    {
        GameObject host = new GameObject("Additional Attack Test"); owned.Add(host);
        host.transform.position = position;
        Health health = host.AddComponent<Health>();
        health.currentHealthStat = new HealthStat { hp = hp, maxHp = hp };
        Enemy enemy = host.AddComponent<Enemy>();
        enemy.health = health;
        typeof(Enemy).GetField("isInitialized", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(enemy, true);
        return enemy;
    }
    private ItemEffectContext Context()
        => new ItemEffectContext(null, Asset<ItemData>(), Vector3.zero, Vector3.zero, null, direction: Vector3.right);
    private ExecuteEffectOnHitData Followup(CombatEffectRecordingEffect effect, HitEffectApplyMode scope)
    {
        ExecuteEffectOnHitData followup = Asset<ExecuteEffectOnHitData>();
        followup.effects = new ItemEffectData[] { effect };
        followup.activationScope = scope;
        return followup;
    }
    [TearDown]
    public void Cleanup()
    {
        for (int i = owned.Count - 1; i >= 0; i--)
            if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
    }

    [Test]
    public void ExistingHitScopeSerializedValuesStayCompatible()
    {
        Assert.That((int)HitEffectApplyMode.FirstHitOnly, Is.EqualTo(0));
        Assert.That((int)HitEffectApplyMode.OncePerTarget, Is.EqualTo(0));
        Assert.That((int)HitEffectApplyMode.EveryHit, Is.EqualTo(1));
    }

    [Test]
    public void FollowupKeepsOriginalHitPositionAndLifeAfterPoolReuse()
    {
        Enemy enemy = EnemyAt(new Vector3(2f, 3f));
        ItemEffectContext source = Context();
        HitEffectContext hit = new HitEffectContext(enemy, source);
        int oldLife = hit.targetLifeId;
        enemy.gameObject.SetActive(false);
        enemy.transform.position = new Vector3(80f, 90f);
        enemy.gameObject.SetActive(true);
        Assert.That(hit.IsTargetValid, Is.False);
        HitEffectContext scope = hit.WithLifetime(new ItemEffectLifetime());
        Assert.That(scope.targetLifeId, Is.EqualTo(oldLife));
        Assert.That(scope.hitPosition, Is.EqualTo(new Vector3(2f, 3f)));
        CombatEffectRecordingEffect recorded = Asset<CombatEffectRecordingEffect>();
        Assert.That(Followup(recorded, HitEffectApplyMode.EveryHit).TryExecute(scope), Is.True);
        Assert.That(recorded.calls.Count, Is.EqualTo(1));
        Assert.That(recorded.calls[0].targetPosition, Is.EqualTo(new Vector3(2f, 3f)));
        Assert.That(recorded.calls[0].hitTargetLifeId, Is.EqualTo(oldLife));
        Assert.That(Asset<StunHitEffectData>().TryExecute(scope), Is.False);
    }

    [Test]
    public void FirstHitPerAttackCombinesDifferentTargets()
    {
        ItemEffectContext source = Context();
        CombatEffectRecordingEffect recorded = Asset<CombatEffectRecordingEffect>();
        ExecuteEffectOnHitData followup = Followup(recorded, HitEffectApplyMode.EveryHit);
        HitEffectDispatcher attack = new HitEffectDispatcher(new HitEffectData[] { followup }, HitEffectApplyMode.FirstHitPerAttack, source);
        attack.Dispatch(new HitEffectContext(EnemyAt(Vector3.left), source));
        attack.Dispatch(new HitEffectContext(EnemyAt(Vector3.right), source));
        Assert.That(recorded.calls.Count, Is.EqualTo(1));
    }

    [Test]
    public void FirstHitPerUseCombinesContextCopiesButNotIndependentUses()
    {
        ItemEffectContext source = Context();
        Enemy enemy = EnemyAt(Vector3.zero);
        CombatEffectRecordingEffect recorded = Asset<CombatEffectRecordingEffect>();
        ExecuteEffectOnHitData followup = Followup(recorded, HitEffectApplyMode.FirstHitPerUse);
        HitEffectDispatcher first = new HitEffectDispatcher(new HitEffectData[] { followup }, HitEffectApplyMode.FirstHitPerUse, source);
        ItemEffectContext child = source.Copy(Vector3.right, Vector3.right);
        HitEffectDispatcher second = new HitEffectDispatcher(new HitEffectData[] { followup }, HitEffectApplyMode.FirstHitPerUse, child);
        first.Dispatch(new HitEffectContext(enemy, source));
        second.Dispatch(new HitEffectContext(enemy, child));
        Assert.That(recorded.calls.Count, Is.EqualTo(1));
        ItemEffectContext independent = Context();
        new HitEffectDispatcher(new HitEffectData[] { followup }, HitEffectApplyMode.FirstHitPerUse, independent)
            .Dispatch(new HitEffectContext(enemy, independent));
        Assert.That(recorded.calls.Count, Is.EqualTo(2));
    }

    [Test]
    public void OncePerTargetDistinguishesPooledLives()
    {
        ItemEffectContext source = Context();
        Enemy enemy = EnemyAt(Vector3.zero);
        CombatEffectRecordingEffect recorded = Asset<CombatEffectRecordingEffect>();
        ExecuteEffectOnHitData followup = Followup(recorded, HitEffectApplyMode.EveryHit);
        HitEffectDispatcher attack = new HitEffectDispatcher(new HitEffectData[] { followup }, HitEffectApplyMode.OncePerTarget, source);
        attack.Dispatch(new HitEffectContext(enemy, source));
        attack.Dispatch(new HitEffectContext(enemy, source));
        enemy.gameObject.SetActive(false); enemy.gameObject.SetActive(true);
        attack.Dispatch(new HitEffectContext(enemy, source));
        Assert.That(recorded.calls.Count, Is.EqualTo(2));
    }

    [Test]
    public void HighestHpSelectionRechecksCurrentHealthEachShot()
    {
        Enemy a = EnemyAt(Vector3.left, 30f), b = EnemyAt(Vector3.right, 20f);
        TargetSelection selection = new TargetSelection { mode = TargetSelectionMode.HighestCurrentHp, range = 10f };
        List<Enemy> candidates = new List<Enemy> { a, b };
        Assert.That(selection.Select(candidates, Vector3.zero), Is.SameAs(a));
        a.health.currentHealthStat.hp = 10f;
        Assert.That(selection.Select(candidates, Vector3.zero), Is.SameAs(b));
    }

    [Test]
    public void TargetSelectionHonorsRangeAndLifeExclusions()
    {
        Enemy a = EnemyAt(Vector3.right), b = EnemyAt(Vector3.right * 3f), far = EnemyAt(Vector3.right * 30f);
        List<Enemy> candidates = new List<Enemy> { a, b, far };
        TargetSelection selection = new TargetSelection { mode = TargetSelectionMode.Farthest, range = 5f };
        Assert.That(selection.Select(candidates, Vector3.zero), Is.SameAs(b));
        HashSet<EnemyLifeKey> excluded = new HashSet<EnemyLifeKey> { new EnemyLifeKey(b) };
        Assert.That(selection.Select(candidates, Vector3.zero, excluded), Is.SameAs(a));
        b.gameObject.SetActive(false); b.gameObject.SetActive(true);
        Assert.That(selection.Select(candidates, Vector3.zero, excluded), Is.SameAs(b));
    }

    [Test]
    public void SectorRejectsSideAndRearButIncludesEdges()
    {
        Assert.That(SectorDamageArea.ContainsDirection(Vector2.right, Vector2.right, 90f), Is.True);
        Assert.That(SectorDamageArea.ContainsDirection(Vector2.right, new Vector2(1f, 1f), 90f), Is.True);
        Assert.That(SectorDamageArea.ContainsDirection(Vector2.right, Vector2.up, 90f), Is.False);
        Assert.That(SectorDamageArea.ContainsDirection(Vector2.right, Vector2.left, 90f), Is.False);
        Assert.That(SectorDamageArea.ContainsDirection(Vector2.right, Vector2.left, 360f), Is.True);
    }

    [Test]
    public void ChainDamageIsConfiguredPerHitAndNeverNegative()
    {
        ChainAttackStat stat = new ChainAttackStat { chainFirstDamage = 20f, chainNextDamage = 17f, chainDamageChangePerJump = -3f };
        Assert.That(stat.DamageAt(0), Is.EqualTo(20f));
        Assert.That(stat.DamageAt(1), Is.EqualTo(17f));
        Assert.That(stat.DamageAt(4), Is.EqualTo(8f));
        Assert.That(stat.DamageAt(100), Is.EqualTo(0f));
    }

    [Test]
    public void CancelledUseCannotTriggerHitFollowup()
    {
        ItemEffectContext source = Context();
        source.lifetime = new ItemEffectLifetime();
        CombatEffectRecordingEffect recorded = Asset<CombatEffectRecordingEffect>();
        ExecuteEffectOnHitData followup = Followup(recorded, HitEffectApplyMode.EveryHit);
        HitEffectContext hit = new HitEffectContext(EnemyAt(Vector3.zero), source);
        source.lifetime.Close(false);
        Assert.That(followup.TryExecute(hit), Is.False);
        Assert.That(recorded.calls.Count, Is.EqualTo(0));
    }

    private ProjectileAttackRuntime Projectile(ProjectileAttackPath path, float damage, float speed, float distance)
    {
        ProjectileAttackEffect effect = Asset<ProjectileAttackEffect>();
        effect.path = path;
        effect.attackStat = new ProjectileAttackStat { projectileDamage = damage, projectileSpeed = speed,
            projectileDistance = distance, projectileLifetime = 20f, projectileOrbitRadius = 2f,
            projectileHitRadius = 0.1f };
        GameObject host = new GameObject("Attack Projectile Test"); owned.Add(host);
        ProjectileAttackRuntime runtime = host.AddComponent<ProjectileAttackRuntime>();
        runtime.Init(effect, Context());
        return runtime;
    }

    [Test]
    public void SweptProjectileHitsFastCrossingAndDeduplicatesColliders()
    {
        Enemy enemy = EnemyAt(Vector3.right * 2f, 100f);
        enemy.gameObject.AddComponent<CircleCollider2D>().radius = 0.25f;
        enemy.gameObject.AddComponent<CircleCollider2D>().radius = 0.3f;
        ProjectileAttackRuntime projectile = Projectile(ProjectileAttackPath.Straight, 8f, 100f, 100f);
        Physics2D.SyncTransforms();
        projectile.Tick(0.2f);
        Assert.That(projectile.transform.position.x, Is.EqualTo(20f).Within(0.0001f));
        Assert.That(enemy.health.Hp, Is.EqualTo(92f));
    }

    [Test]
    public void ReturnProjectileHitsSameLivingEnemyOnBothLegs()
    {
        Enemy enemy = EnemyAt(Vector3.right * 2f, 100f);
        enemy.gameObject.AddComponent<CircleCollider2D>().radius = 0.25f;
        ProjectileAttackRuntime projectile = Projectile(ProjectileAttackPath.Return, 8f, 4f, 4f);
        Physics2D.SyncTransforms();
        projectile.Tick(1f);
        Assert.That(enemy.health.Hp, Is.EqualTo(92f));
        projectile.Tick(0.9f);
        Assert.That(enemy.health.Hp, Is.EqualTo(84f));
    }

    [Test]
    public void OrbitProjectileRehitsAfterOneRevolution()
    {
        Enemy enemy = EnemyAt(Vector3.right * 2f, 100f);
        enemy.gameObject.AddComponent<CircleCollider2D>().radius = 0.25f;
        ProjectileAttackRuntime projectile = Projectile(ProjectileAttackPath.Orbit, 5f, 10f, 100f);
        Physics2D.SyncTransforms();
        projectile.Tick(0.1f);
        Assert.That(enemy.health.Hp, Is.EqualTo(95f));
        projectile.Tick(1.3f);
        Assert.That(enemy.health.Hp, Is.EqualTo(90f));
    }

    [Test]
    public void PeriodicDamageAreaDoesNotDoubleInitialDamageForMultipleColliders()
    {
        Enemy enemy = EnemyAt(Vector3.right, 100f);
        Collider2D first = enemy.gameObject.AddComponent<CircleCollider2D>();
        Collider2D second = enemy.gameObject.AddComponent<CircleCollider2D>();
        GameObject host = new GameObject("Periodic Area Test"); owned.Add(host);
        DamageArea area = host.AddComponent<DamageArea>();
        area.damageApplyMode = DamageApplyMode.Periodic;
        area.InitWithSnapshotAndDynamicBuff(new DamageAreaAttackStat { damageAreaPower = 3f,
            damageAreaRange = 5f, damageAreaInterval = 1f, damageAreaLifeTime = 20f }, null, null, null, null);
        area.InitHitEffects(null, HitEffectApplyMode.EveryHit, Context());
        MethodInfo enter = typeof(DamageArea).GetMethod("OnTriggerEnter2D", BindingFlags.NonPublic | BindingFlags.Instance);
        enter.Invoke(area, new object[] { first });
        enter.Invoke(area, new object[] { second });
        Assert.That(enemy.health.Hp, Is.EqualTo(97f));
    }
}
