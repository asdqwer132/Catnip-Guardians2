using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        plants.SetPlants();
        round.Victory();
        Assert.That(end.CurrentEndType, Is.EqualTo(GameEndType.Win));
    }

    [UnityTest]
    public IEnumerator CircularSlotsKeepIncomingAnglesAndAttackWithoutPlantDamage()
    {
        slots.layout = EnemyAttackSlotLayout.RadialRings;
        slots.centerTarget = plant.transform;
        plant.transform.position = new Vector3(10f, -3f, 0f);
        slots.slots[0].radius = 2f; slots.slots[0].radialThickness = .5f; slots.slots[0].capacity = 5;
        var directions = new[] { Vector2.right, Vector2.up, Vector2.left, Vector2.down, new Vector2(1, 1).normalized };
        var incoming = new Enemy[directions.Length];
        var destinations = new Vector2[directions.Length];
        int attacks = 0; slots.onSlotAttack.AddListener(_ => attacks++);
        float initialHp = plant.health.Hp;
        for (int i = 0; i < directions.Length; i++)
        {
            Vector2 spawn = slots.RingCenter + directions[i] * 6f;
            incoming[i] = Spawn(spawn.x, spawn.y);
            Assert.That(slots.TryGetDestination(incoming[i], out destinations[i]), Is.True);
            var offset = destinations[i] - slots.RingCenter;
            Assert.That(offset.magnitude, Is.InRange(1.75f, 2.25f));
            Assert.That(Vector2.Dot(offset.normalized, directions[i]), Is.EqualTo(1f).Within(.0001f));
        }
        yield return WaitForGameSeconds(.5f);
        for (int i = 0; i < incoming.Length; i++)
        {
            Assert.That(incoming[i].IsAtAttackSlot, Is.True, incoming[i].transform.position.ToString());
            Assert.That(Vector2.Distance(incoming[i].transform.position, destinations[i]), Is.LessThanOrEqualTo(slots.arrivalTolerance));
        }
        Assert.That(attacks, Is.GreaterThan(0));
        Assert.That(plant.health.Hp, Is.EqualTo(initialHp));
    }

    [Test]
    public void CircularReservationsFillInnerRingsFirstAndReleaseOnDeath()
    {
        slots.layout = EnemyAttackSlotLayout.RadialRings;
        slots.slots[0].radius = 2f; slots.slots[0].radialThickness = 1f;
        slots.slots[1].radius = 4f; slots.slots[1].radialThickness = 1f;
        var first = Spawn(8, 0); var second = Spawn(0, -8); var waiting = Spawn(-8, 0);
        Assert.That(slots.TryGetDestination(first, out var inner), Is.True);
        Assert.That(slots.TryGetDestination(second, out var outer), Is.True);
        Assert.That(inner.magnitude, Is.InRange(1.5f, 2.5f));
        Assert.That(outer.magnitude, Is.InRange(3.5f, 4.5f));
        Assert.That(slots.TryGetDestination(first, out var repeated), Is.True);
        Assert.That(repeated, Is.EqualTo(inner), "The random stopping point is sampled once per reservation.");
        Assert.That(slots.TryGetDestination(waiting, out _), Is.False);
        first.TakeDamage(100f);
        Assert.That(slots.TryGetDestination(waiting, out var freed), Is.True);
        Assert.That(freed.magnitude, Is.InRange(1.5f, 2.5f));
    }

    [UnityTest]
    public IEnumerator RectangularBandsAcceptEnemiesFromAllSidesAndKeepTheirIncomingAngles()
    {
        slots.layout = EnemyAttackSlotLayout.RectangularRings;
        slots.centerTarget = plant.transform; plant.transform.position = new Vector3(12, -4, 0);
        slots.slots[0].rectangularSize = new Vector2(8, 4);
        slots.slots[0].rectangularThickness = 1; slots.slots[0].capacity = 5;
        var directions = new[] { Vector2.right, Vector2.up, Vector2.left, Vector2.down, new Vector2(2, 1).normalized };
        var incoming = new List<Enemy>();
        foreach (var direction in directions)
        {
            Vector2 spawn = slots.RingCenter + direction * 9;
            var enemy = Spawn(spawn.x, spawn.y); incoming.Add(enemy);
            Assert.That(slots.TryGetDestination(enemy, out var destination), Is.True);
            var point = destination - slots.RingCenter;
            Assert.That(Mathf.Abs(point.x) <= 4.5001f && Mathf.Abs(point.y) <= 2.5001f, Is.True);
            Assert.That(Mathf.Abs(point.x) >= 3.4999f || Mathf.Abs(point.y) >= 1.4999f, Is.True);
            Assert.That(Vector2.Dot(point.normalized, direction), Is.EqualTo(1).Within(.0001f));
        }
        yield return WaitForGameSeconds(.6f);
        foreach (var enemy in incoming) Assert.That(enemy.IsAtAttackSlot, Is.True, enemy.transform.position.ToString());
    }

    [Test]
    public void CapacityBudgetIsExactAndZeroCapacityBandsDoNotAdmitEnemies()
    {
        slots.layout = EnemyAttackSlotLayout.RadialRings;
        slots.slots = Enumerable.Range(0, 5).Select(i => new EnemyAttackSlot { radius = i + 1 }).ToList();
        slots.slots.Insert(2, null);
        slots.SetTotalCapacity(103);
        Assert.That(slots.slots.Where(s => s != null).Select(s => s.capacity), Is.EqualTo(new[] { 21, 21, 21, 20, 20 }));
        var existing = Spawn(8, 0);
        Assert.That(slots.TryGetDestination(existing, out var original), Is.True);
        slots.SetTotalCapacity(0);
        Assert.That(slots.TryGetDestination(existing, out var retained), Is.True);
        Assert.That(retained, Is.EqualTo(original), "A capacity decrease must not move an existing reservation.");
        var waiting = Spawn(0, 8);
        Assert.That(slots.TryGetDestination(waiting, out _), Is.False);
        slots.SetTotalCapacity(2);
        Assert.That(slots.slots.Where(s => s != null).Select(s => s.capacity), Is.EqualTo(new[] { 1, 1, 0, 0, 0 }));
        Assert.That(slots.TryGetDestination(waiting, out _), Is.True);
        Assert.That(slots.slots[1].ReservedCount, Is.EqualTo(1));
    }

    [Test]
    public void PlantMaxHpBuffsImmediatelyUpdateLimitCapacitiesGaugeAndDefeatThreshold()
    {
        var buffs = Host("Plant buffs").AddComponent<BuffManager>();
        plant.buffManager = buffs;
        var plantData = Asset<PlantData>(); plantData.maxHP = 7;
        plant.Init(plantData);
        slots.layout = EnemyAttackSlotLayout.RadialRings;
        slots.slots = Enumerable.Range(1, 3).Select(i => new EnemyAttackSlot { radius = i }).ToList();
        enemies.Target = plant; enemies.logSpawnTime = false; enemies.Init(plantData);
        var gaugeHost = Host("Population gauge", false);
        var image = gaugeHost.AddComponent<UnityEngine.UI.Image>(); image.type = UnityEngine.UI.Image.Type.Filled;
        var fill = gaugeHost.AddComponent<ImageFillUI>(); fill.fillImage = image;
        var gauge = gaugeHost.AddComponent<HealthBarUI>(); gauge.hpFill = fill;
        gauge.hideWhenFullHp = gauge.hideWhenDead = false; gauge.SetEnemyManager(enemies); gaugeHost.SetActive(true);
        var first = Spawn(8, 0); Spawn(0, 8);
        Assert.That(slots.TryGetDestination(first, out var original), Is.True);
        Assert.That(slots.slots.Select(s => s.capacity), Is.EqualTo(new[] { 3, 2, 2 }));

        var resolver = Asset<DirectBuffTargetResolver>(); resolver.targetComponent = plant.health;
        var modifier = Asset<FloatFieldBuffModifier>(); modifier.targetStatTypeName = nameof(HealthStat);
        modifier.fieldName = "maxHp"; modifier.addValue = 4;
        var effect = Asset<BuffEffect>(); effect.includeSelf = true; effect.targetResolver = resolver;
        effect.buffInfo.useLimitType = BuffUseLimitType.Infinite; effect.modifiers = new BuffModifier[] { modifier };
        effect.Execute(new ItemEffectContext(plant.gameObject, null, Vector3.zero, Vector3.right, null, buffManager: buffs));
        Assert.That(enemies.maxAliveEnemyCount, Is.EqualTo(11));
        Assert.That(slots.slots.Select(s => s.capacity), Is.EqualTo(new[] { 4, 4, 3 }));
        Assert.That(image.fillAmount, Is.EqualTo(2f / 11).Within(.0001f));
        buffs.RemoveBuffHandle(buffs.Storage.activeBuffs.Single());
        Assert.That(enemies.maxAliveEnemyCount, Is.EqualTo(7));
        Assert.That(slots.TotalCapacity, Is.EqualTo(7));
        Assert.That(image.fillAmount, Is.EqualTo(2f / 7).Within(.0001f));
        Assert.That(slots.TryGetDestination(first, out var unchanged), Is.True);
        Assert.That(unchanged, Is.EqualTo(original));

        enemies.enabled = false;
        plant.health.ApplyBuffedStat(new HealthStat { maxHp = 13 });
        Assert.That(enemies.maxAliveEnemyCount, Is.EqualTo(7));
        enemies.enabled = true;
        Assert.That(enemies.maxAliveEnemyCount, Is.EqualTo(13));
        plant.health.ApplyBuffedStat(new HealthStat { maxHp = 2 });
        Assert.That(slots.slots.Select(s => s.capacity), Is.EqualTo(new[] { 1, 1, 0 }));
        Assert.That(end.CurrentEndType, Is.EqualTo(GameEndType.Lose));
        Assert.That(enemies.CurrentAliveEnemyCount, Is.EqualTo(2));

        end.ResetGameEnd(); enemies.PrepareRound();
        plantData.maxHP = 1; plant.Init(plantData); enemies.Init(plantData);
        Assert.That(enemies.maxAliveEnemyCount, Is.EqualTo(1));
        Assert.That(end.IsGameEnded, Is.False, "Retry must clear the previous population before rebinding plant HP.");
    }

    [UnityTest]
    public IEnumerator DestinationSortIsStableAndPlantSideControlsFlip()
    {
        var renderer = prefab.AddComponent<SpriteRenderer>();
        prefab.AddComponent<ActorVisual>().spriteRenderer = renderer;
        slots.slots[0].capacity = 2;
        slots.slots[0].area = new Rect(-10f, 1f, 20f, 0f);
        plant.transform.position = Vector3.zero;
        var left = Spawn(-3f);
        var right = Spawn(3f);
        var upper = Spawn(0f);
        var leftGroup = left.GetComponent<UnityEngine.Rendering.SortingGroup>();
        var rightGroup = right.GetComponent<UnityEngine.Rendering.SortingGroup>();
        var upperGroup = upper.GetComponent<UnityEngine.Rendering.SortingGroup>();
        Assert.That(leftGroup.sortingOrder, Is.GreaterThan(rightGroup.sortingOrder));
        Assert.That(rightGroup.sortingOrder, Is.GreaterThan(upperGroup.sortingOrder));
        int leftOrder = leftGroup.sortingOrder, rightOrder = rightGroup.sortingOrder;
        slots.SetSpawnSortPosition(left, new Vector2(9f, 99f));
        left.transform.position = new Vector3(-3f, 20f, 0f);
        right.transform.position = new Vector3(3f, 10f, 0f);
        yield return null;
        Assert.That(leftGroup.sortingOrder, Is.EqualTo(leftOrder));
        Assert.That(rightGroup.sortingOrder, Is.EqualTo(rightOrder));
        Assert.That(left.visual.spriteRenderer.flipX, Is.False);
        Assert.That(right.visual.spriteRenderer.flipX, Is.True);
        right.visual.LookDirection(Vector2.right);
        Assert.That(right.visual.spriteRenderer.flipX, Is.True);
    }

    private static IEnumerator WaitForGameSeconds(float duration)
    {
        float until = Time.time + duration;
        float timeout = Time.realtimeSinceStartup + 5f;
        while (Time.time < until && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.That(Time.time, Is.GreaterThanOrEqualTo(until), "Game time must advance while movement is tested.");
    }
}
