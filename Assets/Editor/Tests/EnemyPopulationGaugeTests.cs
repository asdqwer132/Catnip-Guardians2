using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class EnemyPopulationGaugeTests
{
    private List<Object> owned;
    private EnemyManager enemies;
    private ObjectPoolManager pool;
    private GameObject prefab;
    private Health health;
    private HealthBarUI gauge;
    private Image image;

    private GameObject Host(string name, bool active = true)
    {
        var host = new GameObject(name);
        host.SetActive(active);
        owned.Add(host);
        return host;
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        owned = new List<Object>();
        Time.timeScale = 1f;
        GameManager.instance = null;
        enemies = Host("Enemy count").AddComponent<EnemyManager>();
        enemies.maxAliveEnemyCount = 4;
        pool = Host("Pool").AddComponent<ObjectPoolManager>();
        pool.poolParent = pool.enemyActiveParent = pool.enemyPoolParent = pool.transform;
        prefab = Host("Enemy prefab", false);
        prefab.AddComponent<Enemy>().health = prefab.AddComponent<Health>();
        prefab.AddComponent<PooledObject>().SetPoolGroup(PoolObjectGroup.Enemy);

        health = Host("Original plant health").AddComponent<Health>();
        health.Init(10f);
        var host = Host("Authored health gauge", false);
        var visual = Host("Authored fill image");
        visual.transform.SetParent(host.transform, false);
        image = visual.AddComponent<Image>();
        image.type = Image.Type.Filled;
        var fill = host.AddComponent<ImageFillUI>();
        fill.fillImage = image;
        gauge = host.AddComponent<HealthBarUI>();
        gauge.hpFill = fill;
        gauge.valueSource = HealthBarValueSource.EnemyPopulation;
        gauge.enemyManager = enemies;
        gauge.health = health;
        gauge.hideWhenFullHp = gauge.hideWhenDead = true;
        host.SetActive(true);
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

    private Enemy Spawn()
    {
        var enemy = pool.Spawn(prefab, Vector3.zero, Quaternion.identity).GetComponent<Enemy>();
        enemy.Revive(10f);
        enemies.RegisterEnemy(enemy);
        return enemy;
    }

    [Test]
    public void GaugeTracksSpawnsDeathsPooledReuseAndDeactivationInsteadOfPlantHealth()
    {
        Assert.That(image.fillAmount, Is.Zero);
        var first = Spawn();
        Assert.That(image.fillAmount, Is.EqualTo(.25f));
        var second = Spawn();
        Assert.That(image.fillAmount, Is.EqualTo(.5f));
        enemies.RegisterEnemy(second);
        Assert.That(image.fillAmount, Is.EqualTo(.5f));
        gauge.SetTarget(health); // HealthActor가 초기화 때 다시 연결해도 개체 수 표시를 유지합니다.
        health.TakeDamage(3f);
        Assert.That(image.fillAmount, Is.EqualTo(.5f));
        health.TakeDamage(100f);
        Assert.That(gauge.gameObject.activeSelf, Is.True);

        first.TakeDamage(100f);
        Assert.That(image.fillAmount, Is.EqualTo(.25f));
        var reused = Spawn();
        Assert.That(reused, Is.SameAs(first));
        Assert.That(image.fillAmount, Is.EqualTo(.5f));
        reused.gameObject.SetActive(false);
        Assert.That(image.fillAmount, Is.EqualTo(.25f));
        Object.DestroyImmediate(second.gameObject);
        Assert.That(image.fillAmount, Is.Zero);
    }

    [UnityTest]
    public IEnumerator ChangingTheLimitAndReopeningTheGaugeRefreshesItsCurrentRatio()
    {
        Spawn();
        Spawn();
        enemies.maxAliveEnemyCount = 8;
        yield return null;
        Assert.That(image.fillAmount, Is.EqualTo(.25f));
        gauge.gameObject.SetActive(false);
        Spawn();
        Assert.That(image.fillAmount, Is.EqualTo(.25f));
        gauge.gameObject.SetActive(true);
        Assert.That(image.fillAmount, Is.EqualTo(.375f));
        enemies.maxAliveEnemyCount = 0;
        yield return null;
        Assert.That(image.fillAmount, Is.Zero);
        enemies.maxAliveEnemyCount = 8;
        yield return null;
        enemies.KillAllEnemies();
        Assert.That(image.fillAmount, Is.Zero);
    }

    [Test]
    public void GaugeRemainsFullAndVisibleWhenThePopulationTriggersDefeat()
    {
        var end = Host("End game").AddComponent<GameEndManager>();
        enemies.loseWhenFieldFull = true;
        enemies.maxAliveEnemyCount = 2;
        Spawn();
        Spawn();
        Assert.That(image.fillAmount, Is.EqualTo(1f));
        Assert.That(gauge.gameObject.activeSelf, Is.True, "The old hide-when-full-HP option must not hide the danger gauge.");
        Assert.That(end.CurrentEndType, Is.EqualTo(GameEndType.Lose));
    }

    [Test]
    public void OtherHealthBarsStillDisplayHpAndIgnoreEnemyPopulation()
    {
        gauge.gameObject.SetActive(false);
        gauge.valueSource = HealthBarValueSource.Health;
        gauge.hideWhenFullHp = false;
        gauge.gameObject.SetActive(true);
        Assert.That(image.fillAmount, Is.EqualTo(1f));
        health.TakeDamage(3f);
        Assert.That(image.fillAmount, Is.EqualTo(.7f).Within(.0001f));
        Spawn();
        Assert.That(image.fillAmount, Is.EqualTo(.7f).Within(.0001f));
    }
}
