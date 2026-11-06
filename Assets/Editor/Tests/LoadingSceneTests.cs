using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class LoadingSceneTests
{
    private const string CanvasPath = "Assets/Prefab/UI/Setting/Canvas/SceneMoveCanvas.prefab";
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject visual;

    private LoadingScenePresentation Presentation()
    {
        visual = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPath));
        var value = visual.GetComponent<LoadingScenePresentation>();
        Assert.That(value, Is.Not.Null);
        return value;
    }
    private static void At(LoadingScenePresentation value, float seconds)
    {
        typeof(LoadingScenePresentation).GetField("elapsed", Private).SetValue(value, seconds);
        typeof(LoadingScenePresentation).GetMethod("RefreshVisuals", Private).Invoke(value, null);
    }
    private static Image[] Runners(LoadingScenePresentation value)
        => value.road.GetComponentsInChildren<Image>().Where(i => i.transform.parent == value.road && i != value.runnerTemplate).ToArray();

    [TearDown] public void TearDown()
    {
        if (visual != null) Object.DestroyImmediate(visual);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    [Test] public void AuthoredLoadingSceneHasVisibleUIFramesAndAnOwnedFallbackListener()
    {
        Assert.That(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == "Assets/Scenes/LoadingScene.unity"), Is.True);
        var core = EditorSceneManager.OpenScene("Assets/Scenes/CoreScene.unity");
        Assert.That(core.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SceneMoveManager>(true)).Count(), Is.EqualTo(1));
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LoadingScene.unity");
        var roots = scene.GetRootGameObjects();
        Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<SceneMoveManager>(true)), Is.Empty);
        var value = roots.SelectMany(r => r.GetComponentsInChildren<LoadingScenePresentation>(true)).Single();
        Assert.That(value.transform.localScale, Is.EqualTo(Vector3.one));
        Assert.That(value.loadingText, Is.Not.Null); Assert.That(value.road.name, Is.EqualTo("Load"));
        Assert.That(value.runnerTemplate, Is.Not.Null); Assert.That(value.screenBounds, Is.Not.Null);
        Assert.That(value.runnerTemplate.gameObject.activeSelf, Is.False);
        Assert.That(value.randomCharacters.Length, Is.EqualTo(8));
        foreach (var character in value.randomCharacters)
        {
            Assert.That(character.runFrames.Length, Is.EqualTo(8));
            Assert.That(character.runFrames.All(s => s != null), Is.True, character.name);
            for (int i = 0; i < 8; i++) Assert.That(character.runFrames[i].name, Does.EndWith("_" + i));
        }
        var camera = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Single();
        Assert.That(camera.cullingMask, Is.Zero); Assert.That(camera.CompareTag("Untagged"), Is.True);
        Assert.That(camera.GetComponent<AudioListener>().enabled, Is.False);
        Assert.That(camera.GetComponent<SceneAudioListenerOwner>(), Is.Not.Null);
        Assert.That(camera.GetComponent<SceneCameraBinding>().role, Is.EqualTo(SceneCameraRole.Background));
    }

    [Test] public void DotsAndSlicedFramesCycleIndependentlyOfProgress()
    {
        var value = Presentation(); value.Begin();
        At(value, 0); Assert.That(value.loadingText.text, Is.EqualTo("Loading."));
        At(value, value.dotIntervalSeconds + .001f); Assert.That(value.loadingText.text, Is.EqualTo("Loading.."));
        At(value, value.dotIntervalSeconds * 2 + .001f); Assert.That(value.loadingText.text, Is.EqualTo("Loading..."));
        At(value, value.dotIntervalSeconds * 3 + .001f); Assert.That(value.loadingText.text, Is.EqualTo("Loading."));
        var runner = Runners(value).Single();
        Assert.That(value.randomCharacters.Count(c => c.runFrames.Contains(runner.sprite)), Is.EqualTo(1));
        var character = value.randomCharacters.Single(c => c.runFrames.Contains(runner.sprite));
        At(value, 1f / character.framesPerSecond + .001f);
        Assert.That(runner.sprite, Is.SameAs(character.runFrames[1]));
    }

    [Test] public void AcquiredRosterUsesOnlyArgumentsWithSpacingAndProgressTracksRoadEndpoints()
    {
        var value = Presentation(); value.characterSelection = LoadingCharacterSelection.AcquiredCharacters;
        var first = value.randomCharacters[0]; var second = value.randomCharacters[1];
        value.Begin(new[] { first, null, second, first });
        var images = Runners(value); Assert.That(images.Length, Is.EqualTo(2));
        Assert.That(images[0].sprite, Is.SameAs(first.runFrames[0]));
        Assert.That(images[1].sprite, Is.SameAs(second.runFrames[0]));
        value.SetProgress(0f);
        Assert.That(images[0].rectTransform.anchoredPosition.x, Is.EqualTo(value.road.rect.xMin).Within(.01f));
        Assert.That(images[0].rectTransform.anchoredPosition.x - images[1].rectTransform.anchoredPosition.x,
            Is.EqualTo(value.characterSize.x + value.characterSpacing).Within(.01f));
        value.SetProgress(.5f);
        Assert.That(images[0].rectTransform.anchoredPosition.x, Is.EqualTo(value.road.rect.center.x).Within(.01f));
        value.SetProgress(1f); value.SetProgress(.1f);
        Assert.That(images[0].rectTransform.anchoredPosition.x, Is.EqualTo(value.road.rect.xMax).Within(.01f));
        value.Begin(new LoadingCharacterAnimation[0]); Assert.That(Runners(value), Is.Empty);
    }

    [TestCase(LoadingRunnerMovement.LoadingProgress)]
    [TestCase(LoadingRunnerMovement.ConstantSpeed)]
    public void CustomStartPointIsAppliedOnTheFirstFrameIncludingOutsideTheScreen(LoadingRunnerMovement movement)
    {
        var value = Presentation(); value.movement = movement;
        var marker = new GameObject("Authored start point", typeof(RectTransform)).GetComponent<RectTransform>();
        marker.SetParent(visual.transform, false);
        marker.anchorMin = marker.anchorMax = new Vector2(0f, .5f);
        marker.anchoredPosition = new Vector2(-200f, 0f);
        value.startPoint = marker; value.startOffsetX = -50f;
        value.characterSelection = LoadingCharacterSelection.AcquiredCharacters;
        value.Begin(new[] { value.randomCharacters[0], value.randomCharacters[1] });
        float start = value.road.InverseTransformPoint(marker.position).x - 50f;
        var images = Runners(value);
        Assert.That(images[0].rectTransform.anchoredPosition.x, Is.EqualTo(start).Within(.01f));
        Assert.That(images[1].rectTransform.anchoredPosition.x,
            Is.EqualTo(start - value.characterSize.x - value.characterSpacing).Within(.01f));
        if (movement == LoadingRunnerMovement.LoadingProgress)
        {
            value.SetProgress(1f);
            Assert.That(images[0].rectTransform.anchoredPosition.x, Is.EqualTo(value.road.rect.xMax).Within(.01f));
        }
        else
        {
            At(value, .1f);
            Assert.That(images[0].rectTransform.anchoredPosition.x, Is.EqualTo(start + value.speed * .1f).Within(.01f));
        }
    }

    [Test] public void OffsetFromRoadEdgeAlsoAppliesWithoutAStartPoint()
    {
        var value = Presentation(); value.startOffsetX = -300f; value.Begin();
        Assert.That(Runners(value).Single().rectTransform.anchoredPosition.x,
            Is.EqualTo(value.road.rect.xMin - 300f).Within(.01f));
    }

    [Test] public void FixedSpeedWrapsBeyondScreenBoundsAndRecalculatesAfterResize()
    {
        var value = Presentation(); value.movement = LoadingRunnerMovement.ConstantSpeed;
        value.Begin(); Canvas.ForceUpdateCanvases(); At(value, 0);
        var runner = Runners(value).Single();
        var corners = new Vector3[4]; value.screenBounds.GetWorldCorners(corners);
        float screenRight = corners.Max(c => value.road.InverseTransformPoint(c).x);
        float start = runner.rectTransform.anchoredPosition.x;
        float exit = screenRight + value.characterSize.x * .5f;
        float exitTime = (exit - start) / value.speed;
        At(value, exitTime - .001f);
        Assert.That(runner.rectTransform.anchoredPosition.x, Is.GreaterThan(screenRight));
        At(value, exitTime + .001f);
        Assert.That(runner.rectTransform.anchoredPosition.x, Is.LessThan(value.road.rect.xMin));
        value.road.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, value.road.rect.width * .7f);
        value.movement = LoadingRunnerMovement.LoadingProgress; value.SetProgress(1f);
        Assert.That(runner.rectTransform.anchoredPosition.x, Is.EqualTo(value.road.rect.xMax).Within(.01f));
    }
}

public sealed class LoadingTransitionTests
{
    private const string TempFolder = "Assets/__LoadingTransitionTests";
    private const string BuildKey = "Catnip.LoadingTests.BuildScenes";
    private const string GeneralName = "LoadingTestGeneral";
    private const string BattleName = "LoadingTestBattle";
    private const string ItemName = "LoadingTestItem";
    private const string EndName = "LoadingTestEnd";

    [UnitySetUp] public IEnumerator SetUp()
    {
        SessionState.SetString(BuildKey, string.Join("\n", EditorBuildSettings.scenes.Select(s => (s.enabled ? "1" : "0") + s.path)));
        if (!AssetDatabase.IsValidFolder(TempFolder)) AssetDatabase.CreateFolder("Assets", "__LoadingTransitionTests");
        foreach (string name in new[] { GeneralName, BattleName, ItemName, EndName })
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (name == GeneralName)
            {
                var general = new GameObject("General").AddComponent<GameGeneralSceneController>();
                general.battleScene = BattleName; general.itemScene = ItemName;
                general.loadCoreScene = false; general.fitSceneCanvases = false;
                general.gameObject.AddComponent<SceneMoveManager>();
            }
            else new GameObject("Content", typeof(Camera), typeof(SceneCameraBinding));
            EditorSceneManager.SaveScene(scene, TempFolder + "/" + name + ".unity");
        }
        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { GeneralName, BattleName, ItemName, EndName }
            .Select(n => new EditorBuildSettingsScene(TempFolder + "/" + n + ".unity", true))).ToArray();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
    }
    [UnityTearDown] public IEnumerator TearDown()
    {
        Time.timeScale = 1;
        if (Application.isPlaying) yield return new ExitPlayMode();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        string original = SessionState.GetString(BuildKey, "");
        if (!string.IsNullOrEmpty(original))
            EditorBuildSettings.scenes = original.Split('\n').Select(s => new EditorBuildSettingsScene(s.Substring(1), s[0] == '1')).ToArray();
        SessionState.EraseString(BuildKey);
        AssetDatabase.DeleteAsset(TempFolder);
    }
    private static IEnumerator Until(System.Func<bool> ready)
    {
        float deadline = Time.realtimeSinceStartup + 30;
        while (!ready() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(ready(), Is.True, "Scene transition timed out.");
    }

    [UnityTest] public IEnumerator ConstantSpeedFirstUpdateDoesNotCountTheStartupFrameBeforeBegin()
    {
        // Scene import can stall the frame before the loading UI is enabled.
        // The first Update must not move a newly created runner by that entire frame's delta.
        System.Threading.Thread.Sleep(250);
        yield return null;
        Assert.That(Time.unscaledDeltaTime, Is.GreaterThan(.2f));
        var visual = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefab/UI/Setting/Canvas/SceneMoveCanvas.prefab"));
        try
        {
            var view = visual.GetComponent<LoadingScenePresentation>();
            view.movement = LoadingRunnerMovement.ConstantSpeed;
            view.startOffsetX = -100f;
            view.speed = view.road.rect.width * .5f / Time.unscaledDeltaTime;
            view.Begin();
            var runner = view.road.GetComponentsInChildren<Image>()
                .Single(i => i.transform.parent == view.road && i != view.runnerTemplate);
            float start = view.road.rect.xMin + view.startOffsetX;
            typeof(LoadingScenePresentation).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(view, null);
            Assert.That(runner.rectTransform.anchoredPosition.x, Is.EqualTo(start).Within(.01f),
                "The first update counted startup time and skipped from the configured start toward the middle.");
            yield return null;
            Assert.That(runner.rectTransform.anchoredPosition.x, Is.GreaterThan(start),
                "Movement must continue normally after the initial frame.");
        }
        finally { Object.DestroyImmediate(visual); }
    }

    [UnityTest] public IEnumerator StandaloneTransitionSurvivesSourceUnloadAndAnimatesWhilePaused()
    {
        var source = SceneManager.GetActiveScene();
        var local = new GameObject("Local loader").AddComponent<SceneMoveManager>(); local.minimumLoadingSeconds = 1f;
        var characters = new[] { AssetDatabase.LoadAssetAtPath<LoadingCharacterAnimation>("Assets/Data/Scriptable/Loading/Mocha.asset") };
        local.LoadScene(EndName, characters);
        yield return Until(() => LoadingScenePresentation.In(SceneManager.GetSceneByName(GameSceneRoutes.Loading)) != null);
        var view = LoadingScenePresentation.In(SceneManager.GetSceneByName(GameSceneRoutes.Loading));
        view.characterSelection = LoadingCharacterSelection.AcquiredCharacters; view.Begin(characters);
        Assert.That(SceneMoveManager.instance.IsLoading, Is.True);
        Assert.That(view.GetComponent<Canvas>().enabled, Is.True);
        view.dotIntervalSeconds = .1f; var initial = view.loadingText.text;
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(.15f);
        Assert.That(view.loadingText.text, Is.Not.EqualTo(initial));
        yield return Until(() => !SceneMoveManager.instance.IsLoading);
        Assert.That(source.isLoaded, Is.False);
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(EndName));
        Assert.That(SceneManager.GetSceneByName(GameSceneRoutes.Loading).isLoaded, Is.False);
        Assert.That(SceneMoveManager.instance.LoadingProgress, Is.EqualTo(1f));
    }

    [UnityTest] public IEnumerator GeneralStartedIndependentlyShowsLoadingWhileComposingChildren()
    {
        yield return SceneManager.LoadSceneAsync(GeneralName, LoadSceneMode.Single);
        yield return Until(() => SceneMoveManager.instance != null && SceneMoveManager.instance.IsLoading);
        yield return Until(() => LoadingScenePresentation.In(SceneManager.GetSceneByName(GameSceneRoutes.Loading)) != null);
        var scene = SceneManager.GetSceneByName(GeneralName);
        var general = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameGeneralSceneController>()).Single();
        yield return Until(() => !SceneMoveManager.instance.IsLoading);
        Assert.That(general.IsReady, Is.True);
        Assert.That(scene.isLoaded, Is.True);
        Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(scene));
        Assert.That(SceneManager.GetSceneByName(GameSceneRoutes.Core).isLoaded, Is.False);
        Assert.That(SceneManager.GetSceneByName(GameSceneRoutes.Loading).isLoaded, Is.False);
    }

    [UnityTest] public IEnumerator CoreIsRetainedAndGeneralChildrenFinishBeforeOverlayClosesThenQueuedTransitionRuns()
    {
        var core = SceneManager.CreateScene(GameSceneRoutes.Core);
        var host = new GameObject("Core loader"); SceneManager.MoveGameObjectToScene(host, core);
        var loader = host.AddComponent<SceneMoveManager>(); loader.minimumLoadingSeconds = .3f;
        loader.LoadScene(GeneralName);
        yield return Until(() => SceneManager.GetSceneByName(GeneralName).isLoaded);
        var generalScene = SceneManager.GetSceneByName(GeneralName);
        var general = generalScene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameGeneralSceneController>()).Single();
        yield return Until(() => general.IsReady);
        Assert.That(loader.IsLoading, Is.True);
        Assert.That(SceneManager.GetSceneByName(GameSceneRoutes.Loading).isLoaded, Is.True);
        Assert.That(general.ItemPhysics.IsValid(), Is.True);
        Assert.That(general.ItemPhysics, Is.Not.EqualTo(Physics2D.defaultPhysicsScene));
        loader.LoadScene(EndName); // Queue while General's completed layout is still covered.
        yield return Until(() => !loader.IsLoading && SceneManager.GetSceneByName(EndName).isLoaded);
        Assert.That(core.isLoaded, Is.True);
        Assert.That(generalScene.isLoaded, Is.False);
        Assert.That(SceneManager.GetSceneByName(BattleName).isLoaded, Is.False);
        Assert.That(SceneManager.GetSceneByName(ItemName).isLoaded, Is.False);
        Assert.That(SceneManager.GetSceneByName(GameSceneRoutes.Loading).isLoaded, Is.False);
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(EndName));
    }
}
