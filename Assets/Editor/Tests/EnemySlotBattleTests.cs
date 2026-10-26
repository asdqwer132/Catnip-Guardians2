using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class EnemySlotBattleTests
{
    private List<Object> owned;
    private ObjectPoolManager pool;
    private EnemyManager enemies;
    private EnemyAttackSlotManager slots;
    private GameEndManager end;
    private GameObject prefab;
    private EnemyDataSet data;
    private Plant plant;

    private GameObject Host(string name, bool active = true)
    {
        var host = new GameObject(name);
        host.SetActive(active);
        owned.Add(host);
        return host;
    }

    private T Asset<T>() where T : ScriptableObject
    {
        var asset = ScriptableObject.CreateInstance<T>();
        owned.Add(asset);
        return asset;
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        owned = new List<Object>();
        Time.timeScale = 1f;
        GameManager.instance = null;
        end = Host("Result").AddComponent<GameEndManager>();
        slots = Host("Attack slots").AddComponent<EnemyAttackSlotManager>();
        slots.slots.Add(new EnemyAttackSlot { area = new Rect(-10f, .9f, 20f, .2f), capacity = 1 });
        slots.slots.Add(new EnemyAttackSlot { area = new Rect(-10f, 1.9f, 20f, .2f), capacity = 1 });
        enemies = Host("Enemies").AddComponent<EnemyManager>();
        enemies.maxAliveEnemyCount = 1000;
        enemies.loseWhenFieldFull = true;
        enemies.attackSlotManager = slots;
        pool = Host("Enemy pool").AddComponent<ObjectPoolManager>();
        pool.poolParent = pool.enemyActiveParent = pool.enemyPoolParent = pool.transform;

        var plantHost = Host("Plant", false);
        var plantHealth = plantHost.AddComponent<Health>();
        plant = plantHost.AddComponent<Plant>();
        plant.health = plantHealth;
        plant.useHealthDefeat = false;
        plantHost.transform.position = new Vector3(-100f, 50f, 0f);
        plantHost.SetActive(true);
        var plantData = Asset<PlantData>();
        plantData.maxHP = 10f;
        plant.Init(plantData);

        prefab = Host("Enemy prefab", false);
        var health = prefab.AddComponent<Health>();
        prefab.AddComponent<ActorTarget>();
        var mover = prefab.AddComponent<ActorMover>();
        mover.useAcceleration = false;
        prefab.AddComponent<ActorAttack>().attackPreparationTime = 0f;
        prefab.AddComponent<Enemy>().health = health;
        prefab.AddComponent<PooledObject>().SetPoolGroup(PoolObjectGroup.Enemy);
        data = Asset<EnemyDataSet>();
        data.statData = Asset<EnemyStatData>();
        data.statData.speed = 20f;
        data.statData.minAttackRange = data.statData.maxAttackRange = .1f;
        data.statData.minAttackCooldown = data.statData.maxAttackCooldown = .05f;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1f;
        for (int i = owned.Count - 1; i >= 0; i--)
            if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
        yield return new ExitPlayMode();
    }

    private Enemy Spawn(float x = 3f, float y = 5f)
    {
        var enemy = pool.Spawn(prefab, new Vector3(x, y, 0f), Quaternion.identity).GetComponent<Enemy>();
        enemy.Init(plant, null, data);
        enemies.RegisterEnemy(enemy);
        return enemy;
    }

    [Test]
    public void IncomingEnemiesReserveTheFirstFreeSlotAndWaitWhenAllAreFull()
    {
        var first = Spawn();
        var second = Spawn();
        var waiting = Spawn();
        Assert.That(slots.TryGetDestination(first, out var firstPoint), Is.True);
        Assert.That(firstPoint.y, Is.InRange(.9f, 1.1f));
        Assert.That(slots.TryGetDestination(second, out var secondPoint), Is.True);
        Assert.That(secondPoint.y, Is.InRange(1.9f, 2.1f));
        Assert.That(slots.TryGetDestination(waiting, out _), Is.False);
        Assert.That(slots.slots[0].ReservedCount, Is.EqualTo(1));
        Assert.That(slots.slots[1].ReservedCount, Is.EqualTo(1));
        Assert.That(enemies.CurrentAliveEnemyCount, Is.EqualTo(3));
        Assert.That(firstPoint.x, Is.EqualTo(first.transform.position.x));
        enemies.RegisterEnemy(first);
        Assert.That(slots.slots[0].ReservedCount, Is.EqualTo(1), "Repeated registration must not double-reserve a slot.");
    }

    [Test]
    public void DeathImmediatelyFreesAReservationAndPooledReuseGetsANewSlot()
    {
        var first = Spawn();
        Spawn();
        first.TakeDamage(100f);
        Assert.That(slots.slots[0].ReservedCount, Is.Zero);
        Assert.That(enemies.CurrentAliveEnemyCount, Is.EqualTo(1));
        var reused = Spawn();
        Assert.That(reused, Is.SameAs(first));
        Assert.That(slots.TryGetDestination(reused, out var position), Is.True);
        Assert.That(position.y, Is.InRange(.9f, 1.1f));
        Assert.That(slots.slots[0].ReservedCount, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator EnemyDescendsWithoutChasingAndAttacksAtItsSlotWithoutRangeOrPlantDamage()
    {
        int attacks = 0;
        slots.onSlotAttack.AddListener(_ => attacks++);
        var enemy = Spawn();
        float initialHp = plant.health.Hp;
        yield return WaitForGameSeconds(.5f);
        Assert.That(enemy.transform.position.x, Is.EqualTo(3f).Within(.0001f));
        Assert.That(enemy.IsAtAttackSlot, Is.True, $"y={enemy.transform.position.y}, state={enemy.BehaviorState}, speed={enemy.mover.speed}, active={enemy.isActiveAndEnabled}, stopped={enemy.IsFullyStopped}, timeStop={enemy.IsTimeStopped}, velocity={enemy.mover.FinalVelocity}");
        Assert.That(enemy.transform.position.y, Is.InRange(.85f, 1.15f));
        Assert.That(enemy.attack.IsTargetAtAttackDistance(), Is.False);
        Assert.That(attacks, Is.GreaterThan(0));
        Assert.That(plant.health.Hp, Is.EqualTo(initialHp));
        Assert.That(end.IsGameEnded, Is.False);
    }

    [UnityTest]
    public IEnumerator StunPausesSlotMovementAndReleasesWithoutChangingX()
    {
        var enemy = Spawn();
        Assert.That(enemy.SetStunned(true), Is.True);
        yield return WaitForGameSeconds(.1f);
        Assert.That(enemy.transform.position.y, Is.EqualTo(5f).Within(.0001f));
        Assert.That(slots.slots[0].ReservedCount, Is.EqualTo(1));
        enemy.SetStunned(false);
        yield return WaitForGameSeconds(.4f);
        Assert.That(enemy.IsAtAttackSlot, Is.True, $"y={enemy.transform.position.y}, state={enemy.BehaviorState}, speed={enemy.mover.speed}, active={enemy.isActiveAndEnabled}, stopped={enemy.IsFullyStopped}, timeStop={enemy.IsTimeStopped}, velocity={enemy.mover.FinalVelocity}");
        Assert.That(enemy.transform.position.x, Is.EqualTo(3f).Within(.0001f));
    }

    [Test]
    public void ReachingTheFieldLimitEndsTheGameEvenBeforeEnemiesReachTheirSlots()
    {
        enemies.maxAliveEnemyCount = 2;
        Spawn();
        Assert.That(end.IsGameEnded, Is.False);
        var last = Spawn();
        Assert.That(last.IsAtAttackSlot, Is.False);
        Assert.That(enemies.FieldLimitReached, Is.True);
        Assert.That(end.CurrentEndType, Is.EqualTo(GameEndType.Lose));
        Assert.That(enemies.CanSpawnMoreEnemies(), Is.False);
        Assert.That(Time.timeScale, Is.Zero);
    }

    [Test]
    public void PlantHealthCannotCauseDefeatInTheNewBattleRules()
    {
        float hp = plant.health.Hp;
        plant.TakeDamage(100f);
        plant.health.TakeDamage(100f);
        Assert.That(plant.health.Hp, Is.EqualTo(hp));
        var host = Host("Inactive game manager", false);
        var game = host.AddComponent<GameManager>();
        var round = host.AddComponent<RoundManager>();
        round.endManager = end;
        game.roundManager = round;
        plant.gameManager = game;
        plant.health.ApplyDamage(100f, new HealthDamagePolicy { bypassImmunity = true });
        Assert.That(plant.IsDead, Is.True);
        Assert.That(end.IsGameEnded, Is.False, "Even a forced health death must not use the old loss rule.");
        plant.useHealthDefeat = true;
        plant.Init(plant.plantData);
        Assert.That(plant.health.damageImmune, Is.True, "Legacy health rules must preserve authored damage immunity.");
    }

    [Test]
    public void CompletingTheLastPlantEndsInVictoryInsteadOfUsingTheDefeatPath()
    {
        var host = Host("Round flow", false);
        var plants = host.AddComponent<PlantManager>();
        plants.allPlants = new[] { plant.plantData };
        var round = host.AddComponent<RoundManager>();
        round.plantManager = plants;
        round.endManager = end;
        typeof(RoundManager).GetMethod("Next", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Invoke(round, null);
        Assert.That(end.CurrentEndType, Is.EqualTo(GameEndType.Win));
    }

    private static IEnumerator WaitForGameSeconds(float duration)
    {
        float until = Time.time + duration;
        float timeout = Time.realtimeSinceStartup + 5f;
        while (Time.time < until && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.That(Time.time, Is.GreaterThanOrEqualTo(until), "Game time must advance while movement is tested.");
    }
}
