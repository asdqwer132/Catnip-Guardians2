using System.Collections;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class EnemySpawnAreaTests
{
    [Test]
    public void IndependentOffsetRangesVaryEachIntervalWithoutChangingBaseStats()
    {
        var host = new GameObject("Spawn timing"); host.SetActive(false);
        var randomState = Random.state;
        try
        {
            var first = host.AddComponent<EnemySpawner>(); first.baseStat.spawnInterval = 2f;
            first.spawnIntervalOffsetRange = new Vector2(-.5f, .5f);
            var second = host.AddComponent<EnemySpawner>(); second.baseStat.spawnInterval = 2f;
            second.spawnIntervalOffsetRange = new Vector2(2f, 3f);
            Random.InitState(9182);
            float min = 3f, max = 0f;
            for (int i = 0; i < 200; i++)
            {
                float interval = first.GetNextSpawnInterval();
                Assert.That(interval, Is.InRange(1.5f, 2.5f));
                Assert.That(second.GetNextSpawnInterval(), Is.InRange(4f, 5f));
                min = Mathf.Min(min, interval); max = Mathf.Max(max, interval);
            }
            Assert.That(max - min, Is.GreaterThan(.8f));
            Assert.That(first.baseStat.spawnInterval, Is.EqualTo(2f));
            first.spawnIntervalOffsetRange = new Vector2(-10f, -10f);
            Assert.That(first.GetNextSpawnInterval(), Is.EqualTo(.05f));
        }
        finally { Random.state = randomState; Object.DestroyImmediate(host); }
    }

    [Test]
    public void AuthoredBattleUsesPlantCenteredCircularRegionsAndMigratedSharedRates()
    {
        try
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameBattleScene.unity", OpenSceneMode.Single);
            var manager = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<EnemyManager>(true)).Single();
            var slots = manager.attackSlotManager;
            Assert.That(slots.layout, Is.EqualTo(EnemyAttackSlotLayout.RadialRings));
            Assert.That(slots.centerTarget, Is.SameAs(manager.Target.transform));
            Assert.That(slots.slots.Select(s => s.radius), Is.Ordered);
            float outer = slots.slots.Max(s => s.radius + s.radialThickness * .5f);
            foreach (var spawner in manager.enemySpawners.Concat(new[] { manager.middleBossSpawner, manager.bossSpawner }))
            {
                Assert.That(spawner.spawnShape, Is.EqualTo(EnemySpawnShape.Circle));
                Assert.That(spawner.spawnCenterTarget, Is.SameAs(manager.Target.transform));
                Assert.That(spawner.SpawnWorldCenter, Is.EqualTo(slots.RingCenter));
                Assert.That(spawner.baseStat.spawnDistance, Is.GreaterThan(outer));
            }
            foreach (string guid in AssetDatabase.FindAssets("t:PlantData"))
            {
                var data = AssetDatabase.LoadAssetAtPath<PlantData>(AssetDatabase.GUIDToAssetPath(guid));
                var serialized = new SerializedObject(data);
                Assert.That(data.normalSpawnInterval, Is.GreaterThan(0), data.name);
                var normals = serialized.FindProperty("normalEnemies");
                for (int i = 0; i < normals.arraySize; i++)
                    Assert.That(normals.GetArrayElementAtIndex(i).FindPropertyRelative("spawnRate"), Is.Null);
                foreach (string field in new[] { "middleBossEnemies", "bossEnemies" })
                {
                    var list = serialized.FindProperty(field);
                    Assert.That(list.isArray, Is.True);
                    for (int i = 0; i < list.arraySize; i++)
                    {
                        var entry = list.GetArrayElementAtIndex(i);
                        Assert.That(entry.FindPropertyRelative("spawnRate"), Is.Not.Null);
                        Assert.That(entry.FindPropertyRelative("spawnWeight"), Is.Null);
                        Assert.That(entry.FindPropertyRelative("dataSet").objectReferenceValue, Is.Not.Null, data.name);
                    }
                }
            }
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
    }

    [Test]
    public void DefaultCircleKeepsItsRadiusAndUsesTheConfiguredWorldCenter()
    {
        var host = new GameObject("Circle spawn test");
        host.SetActive(false);
        var randomState = Random.state;
        try
        {
            var spawner = host.AddComponent<EnemySpawner>();
            Assert.That(spawner.spawnShape, Is.EqualTo(EnemySpawnShape.Circle));
            spawner.spawnCenter = new Vector2(12f, -7f);
            spawner.baseStat.spawnDistance = 9f;
            host.transform.position = new Vector3(100f, 200f, 0f);
            Random.InitState(1234);
            for (int i = 0; i < 256; i++)
                Assert.That((spawner.GetRandomSpawnPosition() - spawner.spawnCenter).magnitude,
                    Is.EqualTo(9f).Within(.0001f));
        }
        finally { Random.state = randomState; Object.DestroyImmediate(host); }
    }

    [Test]
    public void RectangleSpawnsInsideTheAuthoredAreaAndReachesAllFourSections()
    {
        var host = new GameObject("Rectangle spawn test");
        host.SetActive(false);
        var randomState = Random.state;
        try
        {
            var spawner = host.AddComponent<EnemySpawner>();
            spawner.spawnShape = EnemySpawnShape.Rectangle;
            spawner.spawnCenter = new Vector2(-25f, 13f);
            spawner.baseStat.spawnWidth = 20f;
            spawner.baseStat.spawnHeight = 4f;
            host.transform.localScale = new Vector3(2f, .5f, 1f);
            var sections = new int[4];
            int interiorCount = 0;
            Random.InitState(5678);
            for (int i = 0; i < 2400; i++)
            {
                Vector2 offset = spawner.GetRandomSpawnPosition() - spawner.spawnCenter;
                Assert.That(Mathf.Abs(offset.x), Is.LessThanOrEqualTo(10.0001f));
                Assert.That(Mathf.Abs(offset.y), Is.LessThanOrEqualTo(2.0001f));
                if (Mathf.Abs(offset.x) < 9.99f && Mathf.Abs(offset.y) < 1.99f) interiorCount++;
                sections[(offset.x > 0f ? 1 : 0) + (offset.y > 0f ? 2 : 0)]++;
            }
            foreach (int count in sections) Assert.That(count, Is.InRange(500, 700));
            Assert.That(interiorCount, Is.GreaterThan(2000), "The rectangle is a spawn area, not a perimeter.");
        }
        finally { Random.state = randomState; Object.DestroyImmediate(host); }
    }

    [Test]
    public void RectangularPerimeterUsesWidthHeightAndThicknessWithoutFillingTheCenter()
    {
        var host = new GameObject("Rectangular border"); host.SetActive(false);
        var randomState = Random.state;
        try
        {
            var spawner = host.AddComponent<EnemySpawner>(); spawner.spawnShape = EnemySpawnShape.RectanglePerimeter;
            spawner.spawnCenter = new Vector2(12, -4);
            spawner.baseStat.spawnWidth = 12; spawner.baseStat.spawnHeight = 6;
            spawner.baseStat.spawnBorderThickness = 2;
            Random.InitState(33521);
            var sides = new bool[4];
            for (int i = 0; i < 2000; i++)
            {
                var point = spawner.GetRandomSpawnPosition() - spawner.SpawnWorldCenter;
                float x = Mathf.Abs(point.x), y = Mathf.Abs(point.y);
                Assert.That(x <= 7.0001f && y <= 4.0001f, Is.True, point.ToString());
                Assert.That(x >= 4.9999f || y >= 1.9999f, Is.True, "The inner rectangle must remain empty.");
                sides[(x / 6 > y / 3 ? (point.x > 0 ? 0 : 1) : (point.y > 0 ? 2 : 3))] = true;
            }
            Assert.That(sides.All(s => s), Is.True);
            spawner.baseStat.spawnBorderThickness = 0;
            for (int i = 0; i < 200; i++)
            {
                var point = spawner.GetRandomSpawnPosition() - spawner.SpawnWorldCenter;
                Assert.That(Mathf.Min(Mathf.Abs(Mathf.Abs(point.x) - 6), Mathf.Abs(Mathf.Abs(point.y) - 3)), Is.LessThan(.0001f));
            }
        }
        finally { Random.state = randomState; Object.DestroyImmediate(host); }
    }

    [Test]
    public void RectangleDimensionsSurviveStatCloningAndClampInvalidSizes()
    {
        var stat = new EnemySpawnerStat { spawnWidth = 27f, spawnHeight = 11f, spawnBorderThickness = 2f };
        var copy = stat.Clone();
        stat.spawnWidth = 100f;
        Assert.That(copy.spawnWidth, Is.EqualTo(27f));
        Assert.That(copy.spawnHeight, Is.EqualTo(11f));
        Assert.That(copy.spawnBorderThickness, Is.EqualTo(2f));
        copy.spawnWidth = -1f;
        copy.spawnHeight = float.NaN;
        copy.spawnBorderThickness = float.NaN;
        copy.Clamp();
        Assert.That(copy.spawnWidth, Is.GreaterThan(0f));
        Assert.That(copy.spawnHeight, Is.GreaterThan(0f));
        Assert.That(float.IsNaN(copy.spawnHeight), Is.False);
        Assert.That(copy.spawnBorderThickness, Is.Zero);
    }

    [UnityTearDown]
    public IEnumerator LeavePlayMode()
    {
        if (Application.isPlaying) yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator NormalSpawnerCountSharedCooldownAndSpecialRatesDriveActualTimedSpawns()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        Time.timeScale = 1f;
        var host = new GameObject("Spawn timing integration");
        var prefab = new GameObject("Enemy stand-in"); prefab.SetActive(false);
        prefab.AddComponent<PooledObject>().SetPoolGroup(PoolObjectGroup.Enemy);
        var data = ScriptableObject.CreateInstance<PlantData>();
        var first = ScriptableObject.CreateInstance<EnemyDataSet>();
        var second = ScriptableObject.CreateInstance<EnemyDataSet>();
        try
        {
            var pool = host.AddComponent<ObjectPoolManager>();
            pool.poolParent = pool.enemyActiveParent = pool.enemyPoolParent = pool.transform;
            var manager = host.AddComponent<EnemyManager>(); manager.logSpawnTime = false;
            manager.Target = host.AddComponent<Plant>();
            EnemySpawner Spawner(string name, float x)
            {
                var go = new GameObject(name); go.transform.SetParent(host.transform);
                var spawner = go.AddComponent<EnemySpawner>(); spawner.enemyPrefab = prefab;
                spawner.spawnCenter = new Vector2(x, 0); spawner.baseStat.spawnDistance = 1;
                return spawner;
            }
            var a = Spawner("Normal A", 100); a.spawnIntervalOffsetRange = new Vector2(.1f, .1f);
            var b = Spawner("Normal B", 200);
            var unused = Spawner("Unused normal", 300);
            manager.enemySpawners = new[] { a, b, unused };
            manager.middleBossSpawner = Spawner("Middle boss", 400);
            manager.bossSpawner = Spawner("Boss", 500);
            data.spawnNormalEnemyCount = 2; data.normalSpawnInterval = .2f;
            data.normalEnemies = new[] { new EnemySpawnInfo { dataSet = first, spawnWeight = 1 }, new EnemySpawnInfo { dataSet = second, spawnWeight = 3 } };
            data.middleBossEnemies = new[] { new SpecialEnemySpawnInfo { dataSet = first, spawnRate = 4 } };
            data.bossEnemies = new[] { new SpecialEnemySpawnInfo { dataSet = second, spawnRate = 7 } };
            float started = Time.time; manager.Init(data);
            Assert.That(a.gameObject.activeSelf && b.gameObject.activeSelf, Is.True);
            Assert.That(unused.gameObject.activeSelf, Is.False);
            Assert.That(a.CurrentStat.spawnInterval, Is.EqualTo(.2f));
            Assert.That(b.CurrentStat.spawnInterval, Is.EqualTo(.2f));
            Assert.That(manager.middleBossSpawner.CurrentStat.spawnInterval, Is.EqualTo(4));
            Assert.That(manager.bossSpawner.CurrentStat.spawnInterval, Is.EqualTo(7));

            var choose = typeof(EnemySpawner).GetMethod("GetRandomEnemySpawnInfo", BindingFlags.Instance | BindingFlags.NonPublic);
            int firstCount = 0;
            for (int i = 0; i < 2000; i++)
                if (((EnemySpawnInfo)choose.Invoke(a, null)).dataSet == first) firstCount++;
            Assert.That(firstCount, Is.InRange(380, 620), "Weights 1 and 3 should select the first enemy about 25% of the time.");

            float aSpawned = -1, bSpawned = -1;
            float timeout = Time.realtimeSinceStartup + 5f;
            while ((aSpawned < 0 || bSpawned < 0) && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
                foreach (var piece in pool.GetComponentsInChildren<PooledObject>(true))
                {
                    if (!piece.IsSpawned) continue;
                    if (aSpawned < 0 && piece.transform.position.x < 150) aSpawned = Time.time - started;
                    if (bSpawned < 0 && piece.transform.position.x > 150) bSpawned = Time.time - started;
                }
            }
            Assert.That(aSpawned, Is.GreaterThanOrEqualTo(.3f - .001f));
            Assert.That(bSpawned, Is.GreaterThanOrEqualTo(.2f - .001f));
            Assert.That(aSpawned, Is.GreaterThan(bSpawned), "The spawner-specific offset must affect the running timer.");
            manager.AllStop();
        }
        finally
        {
            Object.DestroyImmediate(host); Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(data); Object.DestroyImmediate(first); Object.DestroyImmediate(second);
        }
    }

    [UnityTest]
    public IEnumerator SpecialListsUseIndependentCooldownsAndPauseStopAndRestartTogether()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        // Create captured test state after the domain reload; EnterPlayMode cannot restore a closure.
        yield return RunSpecialListTiming();
    }

    private IEnumerator RunSpecialListTiming()
    {
        Time.timeScale = 1f;
        float previousMaximumDelta = Time.maximumDeltaTime; Time.maximumDeltaTime = .02f;
        var host = new GameObject("Special list timing");
        var prefab = new GameObject("Special enemy"); prefab.SetActive(false);
        prefab.AddComponent<Health>(); prefab.AddComponent<ActorTarget>(); prefab.AddComponent<Enemy>();
        prefab.AddComponent<PooledObject>().SetPoolGroup(PoolObjectGroup.Enemy);
        var data = ScriptableObject.CreateInstance<PlantData>();
        var first = ScriptableObject.CreateInstance<EnemyDataSet>();
        var firstStats = ScriptableObject.CreateInstance<EnemyStatData>();
        var second = ScriptableObject.CreateInstance<EnemyDataSet>();
        var secondStats = ScriptableObject.CreateInstance<EnemyStatData>();
        try
        {
            Assert.That(first, Is.Not.Null); Assert.That(firstStats, Is.Not.Null);
            Assert.That(second, Is.Not.Null); Assert.That(secondStats, Is.Not.Null);
            first.statData = firstStats; second.statData = secondStats;
            var pool = host.AddComponent<ObjectPoolManager>();
            pool.poolParent = pool.enemyActiveParent = pool.enemyPoolParent = pool.transform;
            var plantHost = new GameObject("Plant"); plantHost.transform.SetParent(host.transform);
            var health = plantHost.AddComponent<Health>(); var plant = plantHost.AddComponent<Plant>(); plant.health = health; plant.Init(data);
            var manager = host.AddComponent<EnemyManager>(); manager.Target = plant; manager.logSpawnTime = false;
            var spawnHost = new GameObject("Special spawner"); spawnHost.transform.SetParent(host.transform);
            var spawner = spawnHost.AddComponent<EnemySpawner>(); spawner.enemyPrefab = prefab;
            manager.middleBossSpawner = spawner;
            data.middleBossEnemies = new[] { null, new SpecialEnemySpawnInfo(),
                new SpecialEnemySpawnInfo { dataSet = first, spawnRate = .1f },
                new SpecialEnemySpawnInfo { dataSet = second, spawnRate = .3f } };
            manager.Init(data);
            yield return WaitForSpawnTime(.25f);
            Assert.That(manager.ActiveEnemies.Count(e => e.statData == first.statData), Is.GreaterThanOrEqualTo(2),
                $"Active={manager.ActiveEnemies.Count}, pooled={pool.GetComponentsInChildren<PooledObject>(true).Count(p => p.IsSpawned)}, " +
                $"next={spawner.DebugNextEnemyName}, remaining={spawner.DebugSpawnRemainingTime}, interval={spawner.DebugSpawnInterval}, " +
                $"stopped={TimeStopRuntime.IsStopped(TimeStopTargets.EnemySpawning)}, enabled={spawner.isActiveAndEnabled}");
            Assert.That(manager.ActiveEnemies.Count(e => e.statData == second.statData), Is.Zero,
                "A slower entry must keep its own countdown while the faster entry repeats.");
            int count = manager.ActiveEnemies.Count;
            Time.timeScale = 0;
            float pausedUntil = Time.realtimeSinceStartup + .04f;
            while (Time.realtimeSinceStartup < pausedUntil) yield return null;
            Assert.That(manager.ActiveEnemies.Count, Is.EqualTo(count));
            Time.timeScale = 1; yield return WaitForSpawnTime(.12f);
            Assert.That(manager.ActiveEnemies.Count(e => e.statData == second.statData), Is.GreaterThanOrEqualTo(1));
            spawner.StopSpawning(); count = manager.ActiveEnemies.Count;
            yield return WaitForSpawnTime(.15f);
            Assert.That(manager.ActiveEnemies.Count, Is.EqualTo(count));
            spawner.StartSpawning(); yield return WaitForSpawnTime(.15f);
            Assert.That(manager.ActiveEnemies.Count, Is.GreaterThan(count));
        }
        finally
        {
            Time.timeScale = 1; Time.maximumDeltaTime = previousMaximumDelta;
            Object.DestroyImmediate(host); Object.DestroyImmediate(prefab); Object.DestroyImmediate(data);
            Object.DestroyImmediate(firstStats); Object.DestroyImmediate(secondStats);
            Object.DestroyImmediate(first); Object.DestroyImmediate(second);
        }
    }

    private static IEnumerator WaitForSpawnTime(float duration)
    {
        float until = Time.time + duration;
        float timeout = Time.realtimeSinceStartup + 5f;
        while (Time.time < until && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.That(Time.time, Is.GreaterThanOrEqualTo(until), "The timer test must advance actual game time.");
    }

    [UnityTest]
    public IEnumerator ActualPooledSpawnsUseTheCurrentRectangleDimensions()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        var previousPool = ObjectPoolManager.instance;
        var previousManager = EnemyManager.instance;
        var host = new GameObject("Spawn integration test");
        host.SetActive(false);
        var prefab = new GameObject("Enemy stand-in");
        prefab.SetActive(false);
        prefab.AddComponent<PooledObject>().SetPoolGroup(PoolObjectGroup.Enemy);
        var data = ScriptableObject.CreateInstance<EnemyDataSet>();
        try
        {
            EnemyManager.instance = null;
            var pool = host.AddComponent<ObjectPoolManager>();
            pool.poolParent = pool.enemyActiveParent = pool.enemyPoolParent = pool.transform;
            var target = host.AddComponent<Plant>();
            var spawner = host.AddComponent<EnemySpawner>();
            spawner.spawnShape = EnemySpawnShape.Rectangle;
            spawner.spawnCenter = new Vector2(100f, 200f);
            spawner.baseStat.spawnWidth = 30f;
            spawner.baseStat.spawnHeight = 10f;
            spawner.enemyPrefab = prefab;
            host.SetActive(true);
            spawner.RefreshBuffedStat();
            // 이후 원본 설정이 바뀌어도 이번 소환은 버프가 적용된 런타임 스탯을 사용합니다.
            spawner.baseStat.spawnWidth = spawner.baseStat.spawnHeight = 2f;
            typeof(EnemySpawner).GetField("targetPlant", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(spawner, target);
            var spawn = typeof(EnemySpawner).GetMethod("SpawnEnemy", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int i = 0; i < 16; i++)
                spawn.Invoke(spawner, new object[] { new EnemySpawnInfo { dataSet = data }, 0f });
            int count = 0;
            foreach (var piece in pool.GetComponentsInChildren<PooledObject>(true))
            {
                if (!piece.IsSpawned) continue;
                Vector2 offset = (Vector2)piece.transform.position - spawner.spawnCenter;
                Assert.That(Mathf.Abs(offset.x), Is.LessThanOrEqualTo(15.0001f));
                Assert.That(Mathf.Abs(offset.y), Is.LessThanOrEqualTo(5.0001f));
                Assert.That(piece.transform.position.y, Is.GreaterThan(target.transform.position.y),
                    "A spawn area above the target must produce enemies above it.");
                count++;
            }
            Assert.That(count, Is.EqualTo(16));
        }
        finally
        {
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(data);
            ObjectPoolManager.instance = previousPool;
            EnemyManager.instance = previousManager;
        }
    }
}
