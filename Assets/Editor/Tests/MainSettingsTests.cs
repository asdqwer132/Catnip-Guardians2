using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class MainSettingsTests
{
    [Test]
    public void MainSettingsButtonUsesTheCoreLoaderWithoutChangingButtonStyle()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Single);
        try
        {
            var button = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Button>(true)).Single(b => b.name == "Setting");
            var loader = button.GetComponent<SceneCoreLoader>();
            Assert.That(loader, Is.Not.Null); Assert.That(loader.loadOnStart, Is.True);
            Assert.That(loader.coreScene, Is.EqualTo(GameSceneRoutes.Core));
            Assert.That(button.onClick.GetPersistentTarget(0), Is.SameAs(loader));
            Assert.That(button.onClick.GetPersistentMethodName(0), Is.EqualTo("OpenSettings"));
            Assert.That(button.transition, Is.EqualTo(Selectable.Transition.SpriteSwap));
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
    }

    [UnityTest]
    public IEnumerator StartingMainAloneOpensCoreSettingsWithoutRedirectOrDuplicateCore()
    {
        const string fixture = "Assets/__MainSettingsCore.unity";
        SessionState.SetString("MainSettings.OriginalTarget", SessionState.GetString("Catnip.SceneToPlay", ""));
        SessionState.SetString("MainSettings.BuildScenes", string.Join("\n", EditorBuildSettings.scenes.Select(scene => (scene.enabled ? "1" : "0") + scene.path)));
        var testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var host = new GameObject("Core settings fixture");
        var settings = host.AddComponent<SettingWindowController>();
        settings.settingPanel = new GameObject("Settings panel"); settings.settingPanel.transform.SetParent(host.transform);
        host.AddComponent<CoreSceneBootstrap>();
        EditorSceneManager.SaveScene(testScene, fixture);
        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(fixture, true) }).ToArray();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        SessionState.SetString("Catnip.SceneToPlay", GameSceneRoutes.General);
        var main = SceneManager.GetActiveScene();
        var loaderHost = new GameObject("Standalone main loader");
        var loader = loaderHost.AddComponent<SceneCoreLoader>(); loader.coreScene = fixture;
        loader.OpenSettings();
        float deadline = Time.realtimeSinceStartup + 10f;
        while ((SettingWindowController.instance == null || !SettingWindowController.instance.IsOpen) && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(SettingWindowController.instance, Is.Not.Null);
        Assert.That(SettingWindowController.instance.IsOpen, Is.True);
        Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(main));
        Assert.That(SceneManager.GetSceneByName(GameSceneRoutes.General).isLoaded, Is.False);
        yield return loader.EnsureCore();
        int cores = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).path == fixture) cores++;
        Assert.That(cores, Is.EqualTo(1));
        SettingWindowController.instance.CloseSetting();
        Assert.That(SettingWindowController.instance.IsOpen, Is.False);
        yield return new ExitPlayMode();
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (Application.isPlaying) yield return new ExitPlayMode();
        string saved = SessionState.GetString("MainSettings.BuildScenes", "");
        if (!string.IsNullOrEmpty(saved))
        {
            EditorBuildSettings.scenes = saved.Split('\n').Select(line => new EditorBuildSettingsScene(line.Substring(1), line[0] == '1')).ToArray();
            SessionState.EraseString("MainSettings.BuildScenes");
        }
        SessionState.SetString("Catnip.SceneToPlay", SessionState.GetString("MainSettings.OriginalTarget", ""));
        SessionState.EraseString("MainSettings.OriginalTarget");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AssetDatabase.DeleteAsset("Assets/__MainSettingsCore.unity");
    }
}
