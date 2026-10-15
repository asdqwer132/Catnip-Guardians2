using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class SceneFlowTests
{
    [TestCase("GameScene", GameSceneRoutes.General)]
    [TestCase("GameBattleScene", GameSceneRoutes.Battle)]
    [TestCase("Assets/Scenes/GameItemScene.unity", GameSceneRoutes.Item)]
    [TestCase("Main", GameSceneRoutes.Main)]
    [TestCase("UpgradeScene", GameSceneRoutes.Upgrade)]
    public void SceneLinksResolveToTheCorrectEntryPoint(string input, string expected)
        => Assert.That(GameSceneRoutes.Resolve(input), Is.EqualTo(expected));

    [TestCase(ScenePlayMode.CurrentSceneOnly, GameSceneRoutes.Battle, "")]
    [TestCase(ScenePlayMode.CurrentSceneWithCore, GameSceneRoutes.Core, GameSceneRoutes.Battle)]
    [TestCase(ScenePlayMode.CoreStartupScene, GameSceneRoutes.Core, "")]
    public void EditorPlayModesKeepAnIndependentBattleTestSeparateFromTheGeneralScene(ScenePlayMode mode, string entry, string target)
    {
        Assert.That(SceneStartupPolicy.EditorEntry("Assets/Scenes/GameBattleScene.unity", mode), Is.EqualTo(entry));
        Assert.That(SceneStartupPolicy.EditorCoreTarget("Assets/Scenes/GameBattleScene.unity", mode), Is.EqualTo(target));
    }
    [Test] public void CoreAutoStartCanBeDisabledOrSuppressedDuringAnIndependentTest()
    {
        Assert.That(SceneStartupPolicy.CoreDestination(GameSceneRoutes.General, true, true, true, ""), Is.EqualTo(GameSceneRoutes.General));
        Assert.That(SceneStartupPolicy.CoreDestination(GameSceneRoutes.General, false, true, true, ""), Is.Empty);
        Assert.That(SceneStartupPolicy.CoreDestination(GameSceneRoutes.General, true, false, true, ""), Is.Empty);
        Assert.That(SceneStartupPolicy.CoreDestination(GameSceneRoutes.General, true, true, true, GameSceneRoutes.Item), Is.EqualTo(GameSceneRoutes.Item));
        Assert.That(SceneStartupPolicy.CoreDestination(GameSceneRoutes.Battle, true, true, true, ""), Is.EqualTo(GameSceneRoutes.Battle));
    }
    [Test] public void StandaloneBattleCanConfirmItsResultWithoutACoreCurrencyManager()
    {
        var previous = CurrencyManager.instance;
        var host = new GameObject("Standalone battle statistics test"); host.SetActive(false);
        try
        {
            CurrencyManager.instance = null;
            var statistics = host.AddComponent<GameStatisticsManager>();
            statistics.AddCurrency(CurrencyType.Gold, 5);
            Assert.DoesNotThrow(statistics.Confirm);
            Assert.That(statistics.RoundGold, Is.EqualTo(5));
        }
        finally { CurrencyManager.instance = previous; Object.DestroyImmediate(host); }
    }

    [Test] public void SplitKeepsBothViewsInsideTheScreenWithAGap()
    {
        GameViewportLayout.Calculate(GameScreenLayout.SideBySide, 0.7f, true, 0.04f, out var battle, out var item);
        Assert.That(battle.width, Is.EqualTo(0.672f).Within(0.0001));
        Assert.That(item.xMin - battle.xMax, Is.EqualTo(0.04f).Within(0.0001));
        Assert.That(item.xMax, Is.EqualTo(1f).Within(0.0001));
    }
    [Test] public void ReversedVerticalSplitStillGivesBattleItsRequestedShare()
    {
        GameViewportLayout.Calculate(GameScreenLayout.TopAndBottom, 0.7f, false, 0f, out var battle, out var item);
        Assert.That(battle.yMin, Is.EqualTo(0f)); Assert.That(battle.height, Is.EqualTo(0.7f).Within(0.0001));
        Assert.That(item.yMin, Is.EqualTo(battle.yMax).Within(0.0001));
    }
    [Test] public void SettingsButtonOpensTheSharedPanelAndRestoresAnExistingPause()
    {
        // 서로 다른 씬의 버튼과 코어 패널 사이에 직렬화 참조가 없어도 동작하는지 확인합니다.
        var previousWindow = SettingWindowController.instance;
        var previousInput = GameInputManager.instance;
        float previousScale = Time.timeScale;
        var host = new GameObject("Core settings test"); host.SetActive(false);
        var panel = new GameObject("Existing settings panel"); panel.SetActive(false);
        try
        {
            var input = host.AddComponent<GameInputManager>(); GameInputManager.instance = input;
            input.BlockGameplayInput(true); // 다른 기능이 이미 막아 둔 입력도 보존해야 합니다.
            var window = host.AddComponent<SettingWindowController>(); window.settingPanel = panel;
            SettingWindowController.instance = window;
            typeof(SettingWindowController).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(window, null);
            var button = host.AddComponent<SettingUIButton>();
            Time.timeScale = 0f; button.OpenSetting(); Assert.That(panel.activeSelf, Is.True);
            button.CloseSetting(); Assert.That(panel.activeSelf, Is.False);
            Assert.That(Time.timeScale, Is.Zero); Assert.That(input.IsBlocked(GameInputBlockType.Gameplay), Is.True);
            input.BlockGameplayInput(false); Time.timeScale = 0.5f;
            button.OpenSetting(); button.OpenSetting(); button.CloseSetting();
            Assert.That(Time.timeScale, Is.EqualTo(0.5f)); Assert.That(input.IsBlocked(GameInputBlockType.Gameplay), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(host); Object.DestroyImmediate(panel);
            SettingWindowController.instance = previousWindow; GameInputManager.instance = previousInput; Time.timeScale = previousScale;
        }
    }
}
