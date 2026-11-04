using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class AuthoredSceneViewportTests
{
    private GameObject host, canvasHost, navigationHost, battleHost, itemHost, backgroundHost;
    private GameGeneralSceneController general;
    private Canvas canvas;
    private RectTransform navigation, battle, item;
    private Camera navigationCamera, battleCamera, itemCamera, backgroundCamera;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();

        host = new GameObject("General controller"); host.SetActive(false);
        general = host.AddComponent<GameGeneralSceneController>();
        general.autoLoadChildren = general.loadCoreScene = false;
        general.fitSceneCanvases = false;
        general.viewportSource = GameViewportSource.UIRegions;
        canvasHost = new GameObject("Authored layout", typeof(RectTransform), typeof(Canvas));
        canvas = canvasHost.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        navigation = Region("Navigation", new Rect(.05f, .88f, .9f, .12f));
        battle = Region("Battle", new Rect(.03f, .04f, .52f, .8f));
        item = Region("Item", new Rect(.57f, .04f, .4f, .8f));
        general.navigationCanvas = canvas; general.navigationArea = navigation;
        general.battleArea = battle; general.itemArea = item;

        var battleScene = SceneManager.CreateScene("Authored viewport battle");
        var itemScene = SceneManager.CreateScene("Authored viewport item");
        general.battleScene = battleScene.name; general.itemScene = itemScene.name;
        navigationHost = CameraHost("Navigation camera", SceneCameraRole.Navigation, host.scene);
        battleHost = CameraHost("Battle camera", SceneCameraRole.Content, battleScene);
        itemHost = CameraHost("Item camera", SceneCameraRole.Content, itemScene);
        navigationCamera = navigationHost.GetComponent<Camera>();
        battleCamera = battleHost.GetComponent<Camera>(); itemCamera = itemHost.GetComponent<Camera>();
        backgroundHost = new GameObject("Core background", typeof(Camera));
        backgroundCamera = backgroundHost.GetComponent<Camera>(); backgroundCamera.enabled = false;

        host.SetActive(true);
        var prepare = typeof(GameGeneralSceneController).GetMethod("PrepareContentScene", BindingFlags.Instance | BindingFlags.NonPublic);
        prepare.Invoke(general, new object[] { battleScene });
        prepare.Invoke(general, new object[] { itemScene });
        Canvas.ForceUpdateCanvases();
    }

    [Test]
    public void CamerasFollowAuthoredRegionsWithoutRewritingUIOrUsingRatioSettings()
    {
        // Deliberately incompatible legacy settings must have no effect in UI mode.
        general.layout = GameScreenLayout.BattleOnly; general.battleRatio = .9f;
        general.navigationHeightRatio = .5f;
        general.battlePadding = new SceneViewportPadding { left = 200, top = 100 };
        general.navigationPadding = new SceneViewportPadding { bottom = 100 };
        for (int repeat = 0; repeat < 3; repeat++)
        {
            navigation.hasChanged = battle.hasChanged = item.hasChanged = false;
            general.ApplyLayout();
            AssertRect(navigationCamera.rect, new Rect(.05f, .88f, .9f, .12f));
            AssertRect(battleCamera.rect, new Rect(.03f, .04f, .52f, .8f));
            AssertRect(itemCamera.rect, new Rect(.57f, .04f, .4f, .8f));
            Assert.That(navigation.hasChanged || battle.hasChanged || item.hasChanged, Is.False);
        }
        Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
        Assert.That(canvas.worldCamera, Is.Null);
        AssertRect(backgroundCamera.rect, new Rect(0, 0, 1, 1));

        SceneUILayout.SetRect(battle, new Rect(.1f, .15f, .3f, .65f));
        SceneUILayout.SetRect(item, new Rect(.45f, .15f, .5f, .65f));
        Canvas.ForceUpdateCanvases(); // The real render callback, without manually calling ApplyLayout.
        AssertRect(battleCamera.rect, new Rect(.1f, .15f, .3f, .65f));
        AssertRect(itemCamera.rect, new Rect(.45f, .15f, .5f, .65f));
    }

    [Test]
    public void LayoutGroupsAndNestedOffsetsDetermineTheCameraViewports()
    {
        var container = Region("Authored content layout", new Rect(.05f, .1f, .9f, .7f));
        battle.SetParent(container, false); item.SetParent(container, false);
        var group = container.gameObject.AddComponent<HorizontalLayoutGroup>();
        group.childControlWidth = group.childControlHeight = true;
        group.childForceExpandWidth = group.childForceExpandHeight = true;
        group.spacing = 0;
        var battleWeight = battle.gameObject.AddComponent<LayoutElement>(); battleWeight.flexibleWidth = 6;
        var itemWeight = item.gameObject.AddComponent<LayoutElement>(); itemWeight.flexibleWidth = 4;
        LayoutRebuilder.ForceRebuildLayoutImmediate(container);
        Canvas.ForceUpdateCanvases();
        AssertRect(battleCamera.rect, new Rect(.05f, .1f, .54f, .7f));
        AssertRect(itemCamera.rect, new Rect(.59f, .1f, .36f, .7f));

        battleWeight.flexibleWidth = 2; itemWeight.flexibleWidth = 8;
        group.spacing = 20; group.padding = new RectOffset(10, 10, 5, 5);
        SceneUILayout.SetRect(container, new Rect(.1f, .2f, .8f, .6f));
        LayoutRebuilder.ForceRebuildLayoutImmediate(container);
        Canvas.ForceUpdateCanvases();
        float available = .8f * Screen.width - 40;
        AssertRect(battleCamera.rect, new Rect(.1f + 10f / Screen.width, .2f + 5f / Screen.height,
            available * .2f / Screen.width, .6f - 10f / Screen.height));
        Assert.That(group.enabled, Is.True);
        Assert.That(battleWeight.flexibleWidth, Is.EqualTo(2));
    }

    [Test]
    public void UnavailableRegionsHideTheirCamerasAndVisibleRegionsRecoverWithoutRatioFallback()
    {
        general.battleArea = null; item.gameObject.SetActive(false);
        general.ApplyLayout();
        Assert.That(battleCamera.enabled || itemCamera.enabled, Is.False);
        general.battleArea = battle; item.gameObject.SetActive(true);
        SceneUILayout.SetRect(battle, new Rect(-.1f, .1f, .4f, .5f));
        Canvas.ForceUpdateCanvases();
        Assert.That(battleCamera.enabled && itemCamera.enabled, Is.True);
        AssertRect(battleCamera.rect, new Rect(0, .1f, .3f, .5f));

        SceneUILayout.SetRect(battle, new Rect(2, 0, 1, 1));
        Canvas.ForceUpdateCanvases(); Assert.That(battleCamera.enabled, Is.False);
        // Cropping a layout canvas's own camera must never create a feedback loop.
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = navigationCamera;
        general.ApplyLayout();
        Assert.That(navigationCamera.enabled || battleCamera.enabled || itemCamera.enabled, Is.False);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null;
        SceneUILayout.SetRect(battle, new Rect(.03f, .04f, .52f, .8f));
        Canvas.ForceUpdateCanvases();
        Assert.That(navigationCamera.enabled && battleCamera.enabled && itemCamera.enabled, Is.True);
    }

    private RectTransform Region(string name, Rect rect)
    {
        var area = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        area.SetParent(canvasHost.transform, false); SceneUILayout.SetRect(area, rect);
        return area;
    }
    private static GameObject CameraHost(string name, SceneCameraRole role, Scene scene)
    {
        var value = new GameObject(name); value.SetActive(false);
        SceneManager.MoveGameObjectToScene(value, scene);
        value.AddComponent<Camera>();
        value.AddComponent<SceneCameraBinding>().role = role;
        value.SetActive(true); return value;
    }
    private static void AssertRect(Rect actual, Rect expected)
    {
        Assert.That((actual.min - expected.min).magnitude, Is.LessThan(.0001f));
        Assert.That((actual.size - expected.size).magnitude, Is.LessThan(.0001f));
    }
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.DestroyImmediate(host); Object.DestroyImmediate(canvasHost);
        Object.DestroyImmediate(navigationHost); Object.DestroyImmediate(battleHost);
        Object.DestroyImmediate(itemHost); Object.DestroyImmediate(backgroundHost);
        yield return new ExitPlayMode();
    }
}
