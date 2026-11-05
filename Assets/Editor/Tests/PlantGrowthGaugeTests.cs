using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class PlantGrowthGaugeTests
{
    private readonly List<Object> owned = new List<Object>();
    private GameObject Host(string name, bool active = true)
    {
        var go = new GameObject(name); go.SetActive(active); owned.Add(go); return go;
    }
    private T Data<T>() where T : ScriptableObject
    {
        var data = ScriptableObject.CreateInstance<T>(); owned.Add(data); return data;
    }
    private ImageFillUI Fill(string name)
    {
        var go = Host(name); go.AddComponent<RectTransform>();
        var image = go.AddComponent<Image>(); image.type = Image.Type.Filled;
        var fill = go.AddComponent<ImageFillUI>(); fill.fillImage = image; return fill;
    }
    private HealthBarUI PopulationBar(string name, EnemyManager source)
    {
        var fill = Fill(name); var bar = fill.gameObject.AddComponent<HealthBarUI>();
        bar.hpFill = fill; bar.hideWhenFullHp = bar.hideWhenDead = false;
        bar.SetEnemyManager(source); return bar;
    }
    private GeneralUIDataSender Sender(EnemyManager enemies, GrowManager growth)
    {
        var host = Host("Battle sender", false); var sender = host.AddComponent<GeneralUIDataSender>();
        var serialized = new SerializedObject(sender);
        serialized.FindProperty("uIData").FindPropertyRelative("enemyManager").objectReferenceValue = enemies;
        serialized.FindProperty("uIData").FindPropertyRelative("growManager").objectReferenceValue = growth;
        serialized.ApplyModifiedPropertiesWithoutUndo(); host.SetActive(true); return sender;
    }
    private Enemy Register(EnemyManager source)
    {
        var go = Host("Enemy"); var health = go.AddComponent<Health>();
        var enemy = go.AddComponent<Enemy>(); enemy.health = health; enemy.Revive(10);
        source.RegisterEnemy(enemy); return enemy;
    }
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode(); Time.timeScale = 1; GameManager.instance = null;
    }
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1;
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear(); yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator NutrientsAccumulateAtTheConfiguredRateAndRespectPauseStopAndCompletion()
    {
        var data = Data<PlantData>(); data.nutrientsRequired = 100; data.nutrientsPerSecond = 10;
        var growth = Host("Growth").AddComponent<GrowManager>();
        growth.growthFill = Fill("Battle growth"); growth.Init(data, null); growth.StartGrowth();
        float started = Time.time;
        yield return new WaitForSeconds(.1f);
        Assert.That(growth.CurrentGrowValue, Is.EqualTo((Time.time - started) * 10).Within(.01f));
        Assert.That(growth.CurrentGrowValue, Is.GreaterThan(0));
        float beforePause = growth.CurrentGrowValue; Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(.05f);
        Assert.That(growth.CurrentGrowValue, Is.EqualTo(beforePause));
        Time.timeScale = 1; growth.StopGrowth();
        yield return null;
        Assert.That(growth.CurrentGrowValue, Is.EqualTo(beforePause));
        growth.StartGrowth(); growth.AddGrowth(25);
        Assert.That(growth.CurrentGrowValue, Is.EqualTo(25));
        Assert.That(growth.growthFill.fillImage.fillAmount, Is.EqualTo(.25f));
        growth.AddGrowth(1000);
        Assert.That(growth.CurrentGrowValue, Is.EqualTo(100)); Assert.That(growth.IsGrowing, Is.False);
        Assert.That(growth.growthFill.fillImage.fillAmount, Is.EqualTo(1));
        growth.AddGrowth(10); yield return null;
        Assert.That(growth.CurrentGrowValue, Is.EqualTo(100));
        data.nutrientsRequired = 40; data.nutrientsPerSecond = 0;
        growth.Init(data, null); growth.StartGrowth(); yield return null;
        Assert.That(growth.MaxGrowValue, Is.EqualTo(40)); Assert.That(growth.CurrentGrowValue, Is.Zero);
    }

    [Test]
    public void OnePlantSpriteIsUsedForTheWholeGrowthCycleAndDataHealthInitializesThePlant()
    {
        var data = Data<PlantData>(); data.maxHP = 77; data.nutrientsRequired = 5; data.nutrientsPerSecond = 0;
        var texture = new Texture2D(2, 2); owned.Add(texture);
        var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * .5f); owned.Add(sprite);
        data.visualSprite = sprite;
        var go = Host("Plant", false); var health = go.AddComponent<Health>();
        var plant = go.AddComponent<Plant>(); plant.health = health; plant.useHealthDefeat = false;
        plant.plantUI = go.AddComponent<PlantUI>(); plant.plantUI.spriteRenderer = go.AddComponent<SpriteRenderer>();
        plant.growManager = go.AddComponent<GrowManager>(); go.SetActive(true); plant.Init(data);
        Assert.That(health.MaxHp, Is.EqualTo(77));
        Assert.That(plant.plantUI.spriteRenderer.sprite, Is.SameAs(sprite));
        plant.growManager.AddGrowth(2); Assert.That(plant.plantUI.spriteRenderer.sprite, Is.SameAs(sprite));
        plant.growManager.AddGrowth(3); Assert.That(plant.plantUI.spriteRenderer.sprite, Is.SameAs(sprite));
    }

    [UnityTest]
    public IEnumerator GeneralAndBattleGaugesFollowDataLimitsLateSendersAndSceneRebinding()
    {
        var data = Data<PlantData>(); data.maxHP = 5; data.nutrientsRequired = 80; data.nutrientsPerSecond = 0;
        var generalHp = PopulationBar("General HP", null); var generalGrow = Fill("General growth");
        var uiHost = Host("General receiver", false); var receiver = uiHost.AddComponent<GeneralBattleGaugeUI>();
        receiver.hp = generalHp; receiver.grow = generalGrow; uiHost.SetActive(true);
        Assert.That(generalHp.hpFill.fillImage.fillAmount, Is.Zero);
        var enemies = Host("Battle enemies").AddComponent<EnemyManager>(); enemies.logSpawnTime = false;
        enemies.Init(data); Assert.That(enemies.maxAliveEnemyCount, Is.EqualTo(5));
        var battleHp = PopulationBar("Battle HP", enemies);
        var growth = Host("Battle growth manager").AddComponent<GrowManager>(); growth.growthFill = Fill("Battle growth");
        growth.Init(data, null); growth.StartGrowth();
        var sender = Sender(enemies, growth); yield return null;
        var first = Register(enemies); Register(enemies); growth.AddGrowth(20);
        Assert.That(battleHp.hpFill.fillImage.fillAmount, Is.EqualTo(.4f));
        Assert.That(generalHp.hpFill.fillImage.fillAmount, Is.EqualTo(.4f));
        Assert.That(generalGrow.fillImage.fillAmount, Is.EqualTo(.25f));
        Assert.That(growth.growthFill.fillImage.fillAmount, Is.EqualTo(.25f));
        first.TakeDamage(100);
        Assert.That(generalHp.hpFill.fillImage.fillAmount, Is.EqualTo(.2f));
        sender.gameObject.SetActive(false);
        Assert.That(generalHp.hpFill.fillImage.fillAmount, Is.Zero); Assert.That(generalGrow.fillImage.fillAmount, Is.Zero);
        sender.gameObject.SetActive(true); yield return null;
        Assert.That(generalHp.hpFill.fillImage.fillAmount, Is.EqualTo(.2f));
        Assert.That(generalGrow.fillImage.fillAmount, Is.EqualTo(.25f));
        uiHost.SetActive(false); growth.AddGrowth(20); uiHost.SetActive(true);
        Assert.That(generalGrow.fillImage.fillAmount, Is.EqualTo(.5f));
        data.maxHP = 2; enemies.Init(data);
        Assert.That(enemies.maxAliveEnemyCount, Is.EqualTo(2)); Assert.That(generalHp.hpFill.fillImage.fillAmount, Is.Zero);
        var end = Host("End").AddComponent<GameEndManager>(); enemies.loseWhenFieldFull = true;
        Register(enemies); Register(enemies);
        Assert.That(end.CurrentEndType, Is.EqualTo(GameEndType.Lose));
        Assert.That(generalHp.hpFill.fillImage.fillAmount, Is.EqualTo(1));
    }
}
