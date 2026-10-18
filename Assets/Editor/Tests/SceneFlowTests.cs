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
    [TestCase(GameScreenLayout.SideBySide)]
    [TestCase(GameScreenLayout.TopAndBottom)]
    [TestCase(GameScreenLayout.BattleOnly)]
    [TestCase(GameScreenLayout.ItemOnly)]
    public void NavigationSpaceRemainsOutsideAllContentViews(GameScreenLayout layout)
    {
        GameViewportLayout.Calculate(layout, .6f, false, .02f, .15f, out var battle, out var item);
        Assert.That(battle.yMax, Is.LessThanOrEqualTo(.85001f));
        Assert.That(item.yMax, Is.LessThanOrEqualTo(.85001f));
        Assert.That(Mathf.Max(battle.yMax, item.yMax), Is.EqualTo(.85f).Within(.0001f));
    }
    [Test] public void NavigationUsesAnExistingOverlayCanvasAndRestoresItsAuthoredSettings()
    {
        var host = new GameObject("General navigation test");
        var canvasHost = new GameObject("Authored navigation", typeof(RectTransform), typeof(Canvas));
        try
        {
            var controller = host.AddComponent<GameGeneralSceneController>();
            controller.autoLoadChildren = controller.loadCoreScene = false;
            var canvas = canvasHost.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace; canvas.sortingOrder = 42;
            controller.navigationCanvas = canvas; controller.navigationHeightRatio = .2f;
            controller.ApplyLayout();
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(canvas.worldCamera, Is.Null);
            Assert.That(canvas.sortingOrder, Is.EqualTo(5));
            Assert.That(controller.NavigationTopInset, Is.EqualTo(.2f).Within(.0001f));
            controller.reserveNavigationSpace = false;
            Assert.That(controller.NavigationTopInset, Is.Zero);
            controller.reserveNavigationSpace = true; canvas.enabled = false;
            Assert.That(controller.NavigationTopInset, Is.Zero);
            canvas.enabled = true;
            controller.navigationCanvas = null; controller.ApplyLayout();
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.WorldSpace));
            Assert.That(canvas.sortingOrder, Is.EqualTo(42));
            Assert.That(controller.NavigationTopInset, Is.Zero);
            Assert.That(host.GetComponent<Camera>(), Is.Null);
            Assert.That(host.GetComponent<AudioListener>(), Is.Null);
        }
        finally { Object.DestroyImmediate(host); Object.DestroyImmediate(canvasHost); }
    }
    [Test] public void NavigationCanMeasureTheActualTopPanelInsteadOfUsingARatio()
    {
        var host = new GameObject("General measured navigation test");
        var canvasHost = new GameObject("Authored overlay", typeof(RectTransform), typeof(Canvas));
        var panelHost = new GameObject("Authored top bar", typeof(RectTransform));
        try
        {
            var controller = host.AddComponent<GameGeneralSceneController>();
            controller.autoLoadChildren = controller.loadCoreScene = false;
            controller.navigationCanvas = canvasHost.GetComponent<Canvas>();
            var panel = panelHost.GetComponent<RectTransform>(); panel.SetParent(canvasHost.transform, false);
            panel.anchorMin = new Vector2(0, 1); panel.anchorMax = Vector2.one;
            panel.pivot = new Vector2(.5f, 1); panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(0, 100);
            controller.navigationArea = panel;
            controller.ApplyLayout(); Canvas.ForceUpdateCanvases();
            Assert.That(controller.NavigationTopInset, Is.EqualTo(Mathf.Min(.8f, 100f / Screen.height)).Within(.001f));
            panel.sizeDelta = new Vector2(0, 150); Canvas.ForceUpdateCanvases();
            Assert.That(controller.NavigationTopInset, Is.EqualTo(Mathf.Min(.8f, 150f / Screen.height)).Within(.001f));
            panelHost.SetActive(false);
            Assert.That(controller.NavigationTopInset, Is.Zero);
        }
        finally { Object.DestroyImmediate(host); Object.DestroyImmediate(panelHost); Object.DestroyImmediate(canvasHost); }
    }
    [Test] public void CurrencyDisplayRebindsWhenTheCoreArrivesAfterTheGeneralUI()
    {
        var previous = CurrencyManager.instance;
        var firstHost = new GameObject("Late Core currency"); firstHost.SetActive(false);
        var secondHost = new GameObject("Replacement Core currency"); secondHost.SetActive(false);
        var uiHost = new GameObject("General currency UI"); uiHost.SetActive(false);
        var seedVisual = new GameObject("Authored seed display"); seedVisual.SetActive(false);
        var group = uiHost.AddComponent<CurrencyUIGroup>();
        // 씬 로딩 없이 매니저 준비 알림을 전달해 UI가 먼저 켜지는 순서를 재현합니다.
        System.Action notify = () => ((System.Action)typeof(CurrencyManager)
            .GetField("InstanceChanged", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null))?.Invoke();
        var onEnable = typeof(CurrencyUIGroup).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
        var onDisable = typeof(CurrencyUIGroup).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic);
        try
        {
            CurrencyManager.instance = null;
            group.currencyUIs = new[] { new CurrencyUIGroup.CurrencyUI { type = CurrencyType.Seed, textObj = seedVisual } };
            onEnable.Invoke(group, null);
            var first = firstHost.AddComponent<CurrencyManager>(); first.AddCurrency(CurrencyType.Seed, 1);
            CurrencyManager.instance = first; notify();
            Assert.That(seedVisual.activeSelf, Is.True);
            first.SpendCurrency(CurrencyType.Seed, 1);
            Assert.That(seedVisual.activeSelf, Is.False);
            var second = secondHost.AddComponent<CurrencyManager>(); second.AddCurrency(CurrencyType.Seed, 2);
            CurrencyManager.instance = second; notify();
            Assert.That(seedVisual.activeSelf, Is.True);
            second.SpendCurrency(CurrencyType.Seed, 2);
            Assert.That(seedVisual.activeSelf, Is.False);
            first.AddCurrency(CurrencyType.Seed, 3);
            Assert.That(seedVisual.activeSelf, Is.False, "이전 매니저에서는 등록 해제되어야 합니다.");
            onDisable.Invoke(group, null); second.AddCurrency(CurrencyType.Seed, 4); notify();
            Assert.That(seedVisual.activeSelf, Is.False, "꺼진 UI는 다시 등록되지 않아야 합니다.");
        }
        finally
        {
            onDisable.Invoke(group, null);
            Object.DestroyImmediate(uiHost); Object.DestroyImmediate(seedVisual);
            Object.DestroyImmediate(firstHost); Object.DestroyImmediate(secondHost);
            CurrencyManager.instance = previous; notify();
        }
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
