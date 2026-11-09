using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class StoryProgressionPlaybackTests
{
    private const string StoryFixture = "Assets/__ProgressStory.unity";
    private const string LoadingFixture = "Assets/__ProgressLoading.unity";
    private const string ReturnFixture = "Assets/__ProgressReturn.unity";

    [UnityTest]
    public IEnumerator ConfirmPlaysTheChosenStoryThroughTheLoaderAndReturnsWithoutAdvancingLegacyChapter()
    {
        SessionState.SetString("StoryProgressTests.BuildScenes", string.Join("\n", EditorBuildSettings.scenes.Select(s => (s.enabled ? "1" : "0") + s.path)));
        foreach (string key in new[] { StoryProgressManager.SaveKey, StorySave.StoryChapterProgressKey })
        {
            SessionState.SetBool("StoryProgressTests.Has." + key, PlayerPrefs.HasKey(key));
            SessionState.SetString("StoryProgressTests.Value." + key, key == StorySave.StoryChapterProgressKey
                ? PlayerPrefs.GetInt(key).ToString() : PlayerPrefs.GetString(key));
        }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var viewFixture = new GameObject("Story view").AddComponent<StoryManager>();
        var viewData = new SerializedObject(viewFixture);
        viewData.FindProperty("isTest").boolValue = true; // Explicit progression requests override standalone preview mode.
        viewData.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene, StoryFixture);
        scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Loading presentation").AddComponent<LoadingScenePresentation>();
        EditorSceneManager.SaveScene(scene, LoadingFixture);
        scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, ReturnFixture);
        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { StoryFixture, LoadingFixture, ReturnFixture }
            .Select(path => new EditorBuildSettingsScene(path, true))).ToArray();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();

        PlayerPrefs.DeleteKey(StoryProgressManager.SaveKey);
        StorySave.SetStoryChapterProgress(1);
        var core = SceneManager.CreateScene(GameSceneRoutes.Core);
        var loaderHost = new GameObject("Loader fixture");
        SceneManager.MoveGameObjectToScene(loaderHost, core);
        var loader = loaderHost.AddComponent<SceneMoveManager>();
        loader.loadingScene = "__ProgressLoading"; loader.minimumLoadingSeconds = 0;
        string source = SceneManager.GetActiveScene().name;
        var first = ScriptableObject.CreateInstance<StoryProgressRule>(); first.id = "not-yet";
        var selected = ScriptableObject.CreateInstance<StoryProgressRule>(); selected.id = "selected-later";
        selected.story = ScriptableObject.CreateInstance<StoryData>();
        selected.story.dialogues = new StoryData.DialogueData[0];
        selected.confirmationArea = StoryProgressManager.BattleResult;
        var condition = ScriptableObject.CreateInstance<StoryEventCondition>();
        condition.eventId = StoryProgressEventIds.PlantGrowthCompleted;
        condition.comparisons = new[] {
            new StoryEventComparison { valueKey = "plant-id", textValue = "Plant01" },
            new StoryEventComparison { valueKey = "growth", comparison = StoryComparisonOperator.NumberAtLeast, numberValue = 10 }
        };
        selected.condition = condition;
        // Register the source before the subscriber to check scene-load order independence.
        var growth = new GameObject("Growth publisher").AddComponent<GrowManager>();
        var plant = ScriptableObject.CreateInstance<PlantData>(); plant.dataId = "Plant01";
        plant.nutrientsRequired = 10; plant.nutrientsPerSecond = 0;
        growth.Init(plant, null);
        selected.returnScene = "__ProgressReturn";
        var host = new GameObject("Persistent story progress"); host.SetActive(false);
        var progress = host.AddComponent<StoryProgressManager>();
        progress.rules = new[] { first, selected }; progress.sceneLoader = loader;
        progress.storyScene = "__ProgressStory"; progress.automaticScenes = new[] { source, "__ProgressReturn" };
        host.SetActive(true);
        growth.StartGrowth(); growth.AddGrowth(9);
        Assert.That(progress.IsConditionMet(selected.id), Is.False);
        growth.AddGrowth(1);
        Assert.That(progress.IsConditionMet(selected.id), Is.True);
        Assert.That(progress.IsPlayingStory, Is.False);
        Assert.That(progress.Confirm(StoryProgressManager.BattleResult), Is.True);
        float timeout = Time.realtimeSinceStartup + 15;
        while ((SceneManager.GetActiveScene().name != "__ProgressStory" || loader.IsLoading) && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("__ProgressStory"));
        Assert.That(loader.IsLoading, Is.False);
        var view = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<StoryManager>()).Single();
        var current = typeof(StoryManager).GetProperty("CurrentStory", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(view);
        Assert.That(current, Is.SameAs(selected.story));
        view.SkipStory();
        timeout = Time.realtimeSinceStartup + 15;
        while ((SceneManager.GetActiveScene().name != "__ProgressReturn" || loader.IsLoading) && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("__ProgressReturn"));
        Assert.That(progress.IsCompleted(selected.id), Is.True);
        Assert.That(progress.IsCompleted(first.id), Is.False);
        Assert.That(StorySave.GetStoryChapterProgress(), Is.EqualTo(1));
        Assert.That(progress.Confirm(StoryProgressManager.BattleResult), Is.False);
        yield return new ExitPlayMode();
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (Application.isPlaying) yield return new ExitPlayMode();
        string scenes = SessionState.GetString("StoryProgressTests.BuildScenes", "");
        if (!string.IsNullOrEmpty(scenes)) EditorBuildSettings.scenes = scenes.Split('\n')
            .Select(line => new EditorBuildSettingsScene(line.Substring(1), line[0] == '1')).ToArray();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        foreach (string path in new[] { StoryFixture, LoadingFixture, ReturnFixture }) AssetDatabase.DeleteAsset(path);
        foreach (string key in new[] { StoryProgressManager.SaveKey, StorySave.StoryChapterProgressKey })
        {
            if (!SessionState.GetBool("StoryProgressTests.Has." + key, false)) PlayerPrefs.DeleteKey(key);
            else if (key == StorySave.StoryChapterProgressKey) PlayerPrefs.SetInt(key, int.Parse(SessionState.GetString("StoryProgressTests.Value." + key, "0")));
            else PlayerPrefs.SetString(key, SessionState.GetString("StoryProgressTests.Value." + key, ""));
            SessionState.EraseBool("StoryProgressTests.Has." + key); SessionState.EraseString("StoryProgressTests.Value." + key);
        }
        PlayerPrefs.Save(); SessionState.EraseString("StoryProgressTests.BuildScenes");
    }
}
