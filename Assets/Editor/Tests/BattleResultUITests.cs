using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class BattleResultAuthoringTests
{
    [Test] public void AuthoredResultAndNavigationUseBattleStatisticsWithLocalUIReferences()
    {
        try
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameGeneralScene.unity");
            var groups = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CurrencyUIGroup>(true)).ToArray();
            Assert.That(groups.Length, Is.EqualTo(2));
            Assert.That(groups.All(g => g.source == CurrencyDisplaySource.CurrentGame), Is.True);
            foreach (var group in groups)
                Assert.That(group.currencyUIs.Select(r => r.type).Distinct().Count(), Is.EqualTo(9));
            var ui = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<StatisticsUI>(true)).Single();
            Assert.That(ui.statisticsManager, Is.Null, "Cross-scene battle source must be registered at runtime.");
            foreach (var text in new[] { ui.battleTimeText, ui.killCountText, ui.progressText, ui.stageText, ui.stageProgressText })
            { Assert.That(text, Is.Not.Null); Assert.That(text.gameObject.scene, Is.EqualTo(scene)); }
            Assert.That(ui.progressFill.fillImage.type, Is.EqualTo(Image.Type.Filled));
            Assert.That(ui.progressFill.fillImage.fillMethod, Is.EqualTo(Image.FillMethod.Horizontal));
            Assert.That(ui.currencyUIGroup.Single(), Is.SameAs(groups.Single(g => g.transform.IsChildOf(ui.transform))));
            foreach (var button in ui.GetComponentsInChildren<Button>(true))
                Assert.That(button.onClick.GetPersistentEventCount(), Is.GreaterThan(0), button.name);
            var battle = EditorSceneManager.OpenScene("Assets/Scenes/GameBattleScene.unity");
            var statistics = battle.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameStatisticsManager>(true)).Single();
            Assert.That(statistics.plantManager, Is.Not.Null); Assert.That(statistics.growManager, Is.Not.Null);
            Assert.That(statistics.plantManager.gameObject.scene, Is.EqualTo(battle));
            Assert.That(statistics.growManager.gameObject.scene, Is.EqualTo(battle));
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
    }
}

public sealed class BattleResultUITests
{
    private readonly List<Object> owned = new List<Object>();
    private GameObject Host(string name, bool active = true)
    { var host = new GameObject(name); host.SetActive(active); owned.Add(host); return host; }
    private static void Set(object target, string field, object value)
        => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private TMP_Text Text(Transform parent)
    {
        var host = Host("Authored text"); host.AddComponent<RectTransform>(); host.transform.SetParent(parent, false);
        return host.AddComponent<TextMeshProUGUI>();
    }
    [UnitySetUp] public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode(); Time.timeScale = 1;
    }
    [UnityTearDown] public IEnumerator TearDown()
    {
        Time.timeScale = 1;
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear(); yield return new ExitPlayMode();
    }
    [Test] public void ResultDisplaysFrozenSnapshotAndRebindsWithoutShowingTheWallet()
    {
        var wallet = Host("Wallet").AddComponent<CurrencyManager>();
        var panel = Host("Authored result panel", false);
        var ui = panel.AddComponent<StatisticsUI>();
        ui.battleTimeText = Text(panel.transform); ui.killCountText = Text(panel.transform);
        ui.progressText = Text(panel.transform); ui.stageText = Text(panel.transform); ui.stageProgressText = Text(panel.transform);
        var fill = Host("Authored fill"); fill.AddComponent<RectTransform>(); fill.transform.SetParent(panel.transform, false);
        var image = fill.AddComponent<Image>(); image.type = Image.Type.Filled;
        ui.progressFill = fill.AddComponent<ImageFillUI>(); ui.progressFill.fillImage = image;
        var group = panel.AddComponent<CurrencyUIGroup>(); group.source = CurrencyDisplaySource.CurrentGame;
        var row = panel.AddComponent<CurrencyUI>(); row.type = CurrencyType.CurrencyMain; row.amountText = (TextMeshProUGUI)Text(panel.transform);
        group.currencyUIs = new[] { row }; ui.currencyUIGroup = new[] { group };
        panel.SetActive(true); Assert.That(row.amountText.text, Is.EqualTo("0")); panel.SetActive(false);
        var statistics = Host("Battle statistics").AddComponent<GameStatisticsManager>();
        statistics.plantManager = Host("Stages").AddComponent<PlantManager>();
        statistics.plantManager.allPlants = new PlantData[4];
        Set(statistics.plantManager, "currentPlantIndex", 1);
        statistics.growManager = Host("Growth").AddComponent<GrowManager>();
        statistics.growManager.SetMaxGrowValue(100); statistics.growManager.SetGrowth(50);
        statistics.ResetRound(); Set(statistics, "battleTimeSeconds", 125f);
        statistics.AddKill(3); statistics.AddGold(7);
        var end = Host("End flow").AddComponent<GameEndManager>(); Set(end, "resultPanel", panel);
        end.LoseGame();
        Assert.That(panel.activeSelf, Is.True);
        Assert.That(ui.battleTimeText.text, Is.EqualTo("02:05")); Assert.That(ui.killCountText.text, Is.EqualTo("3"));
        Assert.That(ui.progressText.text, Is.EqualTo("38%")); Assert.That(image.fillAmount, Is.EqualTo(.375f));
        Assert.That(ui.stageText.text, Is.EqualTo("2 / 4")); Assert.That(ui.stageProgressText.text, Is.EqualTo("50%"));
        Assert.That(row.amountText.text, Is.EqualTo("7"));
        wallet.AddCurrency(CurrencyType.CurrencyMain, 100);
        statistics.AddGold(20); statistics.AddKill(); statistics.growManager.SetGrowth(90); statistics.FinishRound(); ui.RefreshUI();
        Assert.That(row.amountText.text, Is.EqualTo("7")); Assert.That(image.fillAmount, Is.EqualTo(.375f));
        Object.DestroyImmediate(statistics.gameObject);
        Assert.That(row.amountText.text, Is.EqualTo("0")); Assert.That(ui.killCountText.text, Is.EqualTo("0"));
        var next = Host("New battle statistics").AddComponent<GameStatisticsManager>(); next.ResetRound(); next.AddGold(2);
        Assert.That(row.amountText.text, Is.EqualTo("2")); Assert.That(ui.progressText.text, Is.EqualTo("0%"));
    }
    [UnityTest] public IEnumerator BattleClockPausesFinishesAndResetsWithTheRound()
    {
        var statistics = Host("Battle statistics").AddComponent<GameStatisticsManager>(); statistics.ResetRound();
        yield return new WaitForSeconds(.05f);
        float elapsed = statistics.BattleTimeSeconds; Assert.That(elapsed, Is.GreaterThan(0));
        Time.timeScale = 0; yield return new WaitForSecondsRealtime(.05f);
        Assert.That(statistics.BattleTimeSeconds, Is.EqualTo(elapsed));
        Time.timeScale = 1; statistics.FinishRound(); yield return new WaitForSeconds(.05f);
        Assert.That(statistics.BattleTimeSeconds, Is.EqualTo(elapsed));
        statistics.ResetRound(); Assert.That(statistics.BattleTimeSeconds, Is.Zero);
        yield return new WaitForSeconds(.05f); Assert.That(statistics.BattleTimeSeconds, Is.GreaterThan(0));
    }
}
