using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class EnemySpawnAreaTests
{
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
    public void RectangleDimensionsSurviveStatCloningAndClampInvalidSizes()
    {
        var stat = new EnemySpawnerStat { spawnWidth = 27f, spawnHeight = 11f };
        var copy = stat.Clone();
        stat.spawnWidth = 100f;
        Assert.That(copy.spawnWidth, Is.EqualTo(27f));
        Assert.That(copy.spawnHeight, Is.EqualTo(11f));
        copy.spawnWidth = -1f;
        copy.spawnHeight = float.NaN;
        copy.Clamp();
        Assert.That(copy.spawnWidth, Is.GreaterThan(0f));
        Assert.That(copy.spawnHeight, Is.GreaterThan(0f));
        Assert.That(float.IsNaN(copy.spawnHeight), Is.False);
    }

    [UnityTearDown]
    public IEnumerator LeavePlayMode()
    {
        if (Application.isPlaying) yield return new ExitPlayMode();
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
