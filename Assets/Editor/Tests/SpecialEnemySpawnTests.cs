using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class SpecialEnemySpawnTests
{
    private List<Object> owned;
    private ObjectPoolManager pool;
    private EnemyManager enemies;
    private GameStatisticsManager statistics;
    private GameObject prefab;
    private Plant plant;
    private PlantData plantData;
    private EnemyDataSet first, second, external;
    private EnemySpawner middle, final;
    private float previousMaximumDelta;

    private GameObject Host(string name, bool active = true)
    {
        var host = new GameObject(name); host.SetActive(active); owned.Add(host); return host;
    }
    private T Asset<T>() where T : ScriptableObject
    {
        var value = ScriptableObject.CreateInstance<T>(); owned.Add(value); return value;
    }
    private EnemyDataSet Data()
    {
        var value = Asset<EnemyDataSet>(); value.statData = Asset<EnemyStatData>(); return value;
    }
    private EnemySpawner Spawner(string name)
    {
        var value = Host(name).AddComponent<EnemySpawner>();
        value.enemyPrefab = prefab; value.statisticsManager = statistics; value.isBoss = true;
        return value;
    }
    private SpecialEnemySpawnSettings Settings(SpecialEnemySpawnSelection mode, params SpecialEnemySpawnCondition[] conditions)
        => new SpecialEnemySpawnSettings { selection = mode, spawnInterval = .05f, conditions = conditions.ToList() };
    private static SpecialEnemySpawnCondition Condition(SpecialEnemySpawnConditionType type, int amount)
        => new SpecialEnemySpawnCondition { type = type, amount = amount };
    private SpecialEnemySpawnInfo Entry(EnemyDataSet data, float weight = 1)
        => new SpecialEnemySpawnInfo { dataSet = data, spawnWeight = weight };
    private int Count(EnemyDataSet data) => enemies.ActiveEnemies.Count(e => e.statData == data.statData);
    private Enemy External()
    {
        var enemy = pool.Spawn(prefab, Vector3.one * 5, Quaternion.identity).GetComponent<Enemy>();
        enemy.Init(plant, null, external); enemies.RegisterEnemy(enemy); return enemy;
    }
    private void StartMiddle(SpecialEnemySpawnSettings settings, params SpecialEnemySpawnInfo[] entries)
    {
        plantData.middleBossSpawnSettings = settings; plantData.middleBossEnemies = entries;
        enemies.Init(plantData);
        if (settings.selection != SpecialEnemySpawnSelection.IndependentIntervals)
            Assert.That(middle.CurrentStat.spawnInterval, Is.EqualTo(settings.spawnInterval));
    }
    private static IEnumerator Advance(float duration)
    {
        float until = Time.time + duration, timeout = Time.realtimeSinceStartup + 5;
        while (Time.time < until && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.That(Time.time, Is.GreaterThanOrEqualTo(until));
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        owned = new List<Object>(); Time.timeScale = 1;
        previousMaximumDelta = Time.maximumDeltaTime; Time.maximumDeltaTime = .02f;
        pool = Host("Pool").AddComponent<ObjectPoolManager>();
        pool.poolParent = pool.enemyActiveParent = pool.enemyPoolParent = pool.transform;
        prefab = Host("Enemy template", false);
        var health = prefab.AddComponent<Health>(); prefab.AddComponent<ActorTarget>();
        prefab.AddComponent<Enemy>().health = health;
        prefab.AddComponent<PooledObject>().SetPoolGroup(PoolObjectGroup.Enemy);
        plantData = Asset<PlantData>(); plantData.maxHP = 1000;
        var plantHost = Host("Plant", false); health = plantHost.AddComponent<Health>();
        plant = plantHost.AddComponent<Plant>(); plant.health = health; plant.useHealthDefeat = false;
        plantHost.SetActive(true); plant.Init(plantData);
        enemies = Host("Encounter").AddComponent<EnemyManager>(); enemies.Target = plant; enemies.logSpawnTime = false;
        statistics = Host("Battle statistics").AddComponent<GameStatisticsManager>(); statistics.ResetRound();
        first = Data(); second = Data(); external = Data();
        middle = Spawner("Middle boss"); final = Spawner("Final boss");
        enemies.middleBossSpawner = middle; enemies.bossSpawner = final;
    }
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1; Time.maximumDeltaTime = previousMaximumDelta;
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator AllTogetherChecksAliveConditionOnceForTheWholeBatch()
    {
        var settings = Settings(SpecialEnemySpawnSelection.AllTogether,
            Condition(SpecialEnemySpawnConditionType.AliveEnemyCountAtMost, 0));
        StartMiddle(settings, Entry(first, 0), Entry(second, 0));
        yield return Advance(.08f);
        Assert.That(Count(first), Is.EqualTo(1)); Assert.That(Count(second), Is.EqualTo(1));
        yield return Advance(.1f);
        Assert.That(enemies.CurrentAliveEnemyCount, Is.EqualTo(2), "The at-most condition blocks subsequent batches.");
        foreach (var enemy in enemies.ActiveEnemies.ToArray()) pool.Release(enemy.gameObject);
        yield return Advance(.03f);
        Assert.That(Count(first), Is.EqualTo(1)); Assert.That(Count(second), Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator WeightedOneSpawnsOnlyOneEntryAndZeroWeightsWaitWithoutConsumingKills()
    {
        var a = Entry(first, 0); var b = Entry(second, 0);
        var settings = Settings(SpecialEnemySpawnSelection.WeightedOne,
            Condition(SpecialEnemySpawnConditionType.KillsSinceLastSpawnAtLeast, 2));
        middle.spawnImmediatelyOnStart = true;
        statistics.AddKill(2); StartMiddle(settings, a, b);
        yield return Advance(.08f);
        Assert.That(enemies.CurrentAliveEnemyCount, Is.Zero);
        Assert.That(middle.SpecialKillsSinceLastSpawn, Is.EqualTo(2));
        b.spawnWeight = 5;
        yield return Advance(.03f);
        Assert.That(Count(first), Is.Zero); Assert.That(Count(second), Is.EqualTo(1));
        Assert.That(middle.SpecialKillsSinceLastSpawn, Is.Zero);
        Assert.That(statistics.RoundKillCount, Is.EqualTo(2));
        yield return Advance(.08f);
        Assert.That(Count(second), Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator RealEnemyKillsRearmTheConditionAfterEverySuccessfulSummon()
    {
        StartMiddle(Settings(SpecialEnemySpawnSelection.WeightedOne,
            Condition(SpecialEnemySpawnConditionType.KillsSinceLastSpawnAtLeast, 2)), Entry(first));
        External().TakeDamage(100);
        yield return Advance(.08f); Assert.That(Count(first), Is.Zero);
        External().TakeDamage(100);
        yield return Advance(.03f); Assert.That(Count(first), Is.EqualTo(1));
        Assert.That(middle.SpecialKillsSinceLastSpawn, Is.Zero);
        Assert.That(statistics.RoundKillCount, Is.EqualTo(2));
        External().TakeDamage(100);
        yield return Advance(.07f); Assert.That(Count(first), Is.EqualTo(1));
        External().TakeDamage(100);
        yield return Advance(.03f); Assert.That(Count(first), Is.EqualTo(2));
        Assert.That(statistics.RoundKillCount, Is.EqualTo(4));
    }

    [UnityTest]
    public IEnumerator FullFieldRetainsKillProgressUntilSpawningActuallySucceeds()
    {
        enemies.specialSpawnerIgnoresMaxAliveCount = false;
        StartMiddle(Settings(SpecialEnemySpawnSelection.WeightedOne,
            Condition(SpecialEnemySpawnConditionType.KillsSinceLastSpawnAtLeast, 2)), Entry(first));
        plant.health.ApplyBuffedStat(new HealthStat { maxHp = 1 });
        var occupying = External(); statistics.AddKill(2);
        yield return Advance(.08f);
        Assert.That(Count(first), Is.Zero); Assert.That(middle.SpecialKillsSinceLastSpawn, Is.EqualTo(2));
        pool.Release(occupying.gameObject);
        yield return Advance(.03f);
        Assert.That(Count(first), Is.EqualTo(1)); Assert.That(middle.SpecialKillsSinceLastSpawn, Is.Zero);
    }

    [UnityTest]
    public IEnumerator BossGroupsKeepSeparateKillProgressAndResetWithTheBattleRound()
    {
        var condition = Condition(SpecialEnemySpawnConditionType.KillsSinceLastSpawnAtLeast, 2);
        plantData.bossSpawnSettings = Settings(SpecialEnemySpawnSelection.WeightedOne, condition);
        plantData.bossEnemies = new[] { Entry(second) };
        StartMiddle(Settings(SpecialEnemySpawnSelection.WeightedOne, condition), Entry(first));
        statistics.AddKill(2);
        yield return Advance(.08f);
        Assert.That(Count(first), Is.EqualTo(1)); Assert.That(Count(second), Is.EqualTo(1));
        middle.StopSpawning(); statistics.AddKill(2);
        yield return Advance(.08f);
        Assert.That(Count(second), Is.EqualTo(2)); Assert.That(middle.SpecialKillsSinceLastSpawn, Is.EqualTo(2));
        middle.StartSpawning(); yield return Advance(.08f);
        Assert.That(Count(first), Is.EqualTo(2));
        statistics.ResetRound(); statistics.AddKill(1);
        yield return Advance(.08f);
        Assert.That(Count(first), Is.EqualTo(2)); Assert.That(Count(second), Is.EqualTo(2));
        statistics.AddKill(1); yield return Advance(.03f);
        Assert.That(Count(first), Is.EqualTo(3)); Assert.That(Count(second), Is.EqualTo(3));
    }

    [UnityTest]
    public IEnumerator ImmediateSpawnStillWaitsForAliveConditionAndRespectsPause()
    {
        middle.spawnImmediatelyOnStart = true;
        var settings = Settings(SpecialEnemySpawnSelection.AllTogether,
            Condition(SpecialEnemySpawnConditionType.AliveEnemyCountAtLeast, 2));
        settings.spawnInterval = 10;
        StartMiddle(settings, Entry(first), Entry(second));
        External(); yield return Advance(.03f); Assert.That(Count(first), Is.Zero);
        Time.timeScale = 0; External();
        float until = Time.realtimeSinceStartup + .04f;
        while (Time.realtimeSinceStartup < until) yield return null;
        Assert.That(Count(first), Is.Zero);
        Time.timeScale = 1; yield return Advance(.03f);
        Assert.That(Count(first), Is.EqualTo(1)); Assert.That(Count(second), Is.EqualTo(1));
    }

    [Test]
    public void WeightedSelectionUsesRelativeWeightsAndSkipsInvalidEntries()
    {
        middle.specialEnemyInfos = new[] { null, new SpecialEnemySpawnInfo { spawnWeight = 1000 }, Entry(first, 1), Entry(second, 3) };
        var method = typeof(EnemySpawner).GetMethod("PickWeightedSpecial", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var state = Random.state;
        try
        {
            Random.InitState(7654); int count = 0;
            for (int i = 0; i < 4000; i++)
                if (((SpecialEnemySpawnInfo)method.Invoke(middle, null)).dataSet == first) count++;
            Assert.That(count, Is.InRange(850, 1150));
        }
        finally { Random.state = state; }
    }

    [Test]
    public void ConditionListsSupportAllAnyAndMissingSources()
    {
        var settings = Settings(SpecialEnemySpawnSelection.AllTogether,
            Condition(SpecialEnemySpawnConditionType.AliveEnemyCountAtLeast, 5),
            Condition(SpecialEnemySpawnConditionType.AliveEnemyCountAtMost, 10),
            Condition(SpecialEnemySpawnConditionType.KillsSinceLastSpawnAtLeast, 3));
        Assert.That(settings.ConditionsMet(7, 3, true, true), Is.True);
        Assert.That(settings.ConditionsMet(7, 2, true, true), Is.False);
        Assert.That(settings.ConditionsMet(11, 3, true, true), Is.False);
        Assert.That(settings.ConditionsMet(7, 3, true, false), Is.False);
        settings.conditionMatch = SpecialEnemySpawnConditionMatch.Any;
        Assert.That(settings.ConditionsMet(0, 3, false, true), Is.True);
        Assert.That(settings.ConditionsMet(0, 0, false, false), Is.False);
        settings.conditions.Clear(); Assert.That(settings.ConditionsMet(0, 0, false, false), Is.True);
    }
}
