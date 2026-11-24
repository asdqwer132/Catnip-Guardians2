using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMoveManager : MonoBehaviour
{
    public static SceneMoveManager instance;
    [Header("Loading Scene")]
    public string loadingScene = GameSceneRoutes.Loading;
    [Min(0f)] public float minimumLoadingSeconds = 0.2f;
    public bool IsLoading { get; private set; }
    public float LoadingProgress { get; private set; }
    private string pendingScene;
    private bool persistentWorker;
    private LoadingCharacterAnimation[] acquiredCharacters;
    private AsyncOperation unloadingOverlay;
    private LoadingScenePresentation presentation;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance() => instance = null;
    private void Awake()
    {
        if (instance == null || (!instance.IsLoading && gameObject.scene.name == GameSceneRoutes.Core)) instance = this;
    }

    public void GoGameScene() => LoadScene(GameSceneRoutes.General);
    public void GoUpgradeScene() => LoadScene(GameSceneRoutes.Upgrade);
    public void GoMainScene() => LoadScene(GameSceneRoutes.Main);
    public void GoStoryScene() => LoadScene(GameSceneRoutes.Story);

    // Call before LoadScene. This only receives ownership information; it does not unlock characters.
    public void SetAcquiredCharacters(IEnumerable<LoadingCharacterAnimation> characters)
    {
        if (instance != null && instance != this) { instance.SetAcquiredCharacters(characters); return; }
        var copy = new List<LoadingCharacterAnimation>();
        if (characters != null)
            foreach (var character in characters)
                if (character != null && !copy.Contains(character)) copy.Add(character);
        acquiredCharacters = copy.ToArray();
    }

    public void LoadScene(string sceneName, IEnumerable<LoadingCharacterAnimation> characters)
    {
        SetAcquiredCharacters(characters);
        LoadScene(sceneName);
    }
    public void LoadScene(string sceneName)
    {
        if (instance != null && instance != this) { instance.LoadScene(sceneName); return; }
        sceneName = GameSceneRoutes.Resolve(sceneName);
        if (string.IsNullOrEmpty(sceneName) || sceneName == GameSceneRoutes.Resolve(loadingScene)) return;
        if (sceneName == GameSceneRoutes.Core && SceneManager.GetSceneByName(sceneName).isLoaded) return;
        if (!ValidateScene(sceneName) || !ValidateScene(GameSceneRoutes.Resolve(loadingScene))) return;
        if (IsLoading) { pendingScene = sceneName; return; }
        var worker = TransitionOwner();
        worker.StartCoroutine(worker.LoadSceneRoutine(sceneName, null));
    }

    // General launched directly in the Editor uses the same overlay while composing its children.
    public IEnumerator LoadChildrenWithLoading(GameGeneralSceneController general)
    {
        var owner = instance != null ? instance : this;
        while (owner.IsLoading) yield return null;
        if (general == null || general.IsReady) yield break;
        if (!owner.ValidateScene(GameSceneRoutes.Resolve(owner.loadingScene))) yield break;
        owner = owner.TransitionOwner();
        yield return owner.StartCoroutine(owner.LoadSceneRoutine(null, general));
    }

    private bool ValidateScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName)) return true;
        Debug.LogError($"빌드 목록에 없는 씬입니다: {sceneName}", this);
        return false;
    }
    private SceneMoveManager TransitionOwner()
    {
        if (persistentWorker || gameObject.scene.name == GameSceneRoutes.Core) return this;
        // Local scene loaders can be on EventSystems. Preserve only a small logic worker,
        // never the source scene's UI, camera or input objects.
        var host = new GameObject("Scene Transition Loader");
        var worker = host.AddComponent<SceneMoveManager>();
        worker.loadingScene = loadingScene;
        worker.minimumLoadingSeconds = minimumLoadingSeconds;
        worker.acquiredCharacters = acquiredCharacters;
        worker.persistentWorker = true;
        instance = worker;
        DontDestroyOnLoad(host);
        return worker;
    }

    public void LoadScene(int sceneIndex)
    {
        if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings) return;
        LoadScene(SceneUtility.GetScenePathByBuildIndex(sceneIndex));
    }
    public void ReloadCurrentScene() => LoadScene(SceneManager.GetActiveScene().name);
    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private IEnumerator LoadSceneRoutine(string sceneName, GameGeneralSceneController composeOnly)
    {
        IsLoading = true;
        LoadingProgress = 0f;
        string overlayName = GameSceneRoutes.Resolve(loadingScene);
        Scene overlay = default;
        try
        {
            if (unloadingOverlay != null) yield return unloadingOverlay;
            unloadingOverlay = null;
            overlay = SceneManager.GetSceneByName(overlayName);
            if (!overlay.isLoaded) yield return SceneManager.LoadSceneAsync(overlayName, LoadSceneMode.Additive);
            overlay = SceneManager.GetSceneByName(overlayName);
            presentation = LoadingScenePresentation.In(overlay);
            if (presentation == null)
            { Debug.LogError($"{overlayName}: LoadingScenePresentation 연결을 확인하세요.", this); yield break; }
            presentation.Begin(acquiredCharacters);
            Canvas.ForceUpdateCanvases();
            float shownAt = Time.realtimeSinceStartup;
            // Draw the loading UI before unloading or activating any content scene.
            yield return null;
            SettingWindowController.instance?.CloseSetting();
            GamePause.SetResumeTimeScale(1f);
            if (composeOnly != null)
            {
                yield return composeOnly.LoadChildren(value => ReportProgress(value * 0.95f));
            }
            else
            {
                var core = SceneManager.GetSceneByName(GameSceneRoutes.Core);
                SceneManager.SetActiveScene(core.isLoaded ? core : overlay);
                var previous = new List<Scene>();
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.isLoaded && scene != core && scene != overlay) previous.Add(scene);
                }
                // Clear old scene singletons before activating the destination.
                for (int i = previous.Count - 1; i >= 0; i--)
                {
                    if (previous[i].isLoaded)
                        yield return Track(SceneManager.UnloadSceneAsync(previous[i]),
                            0.2f * (previous.Count - i - 1) / previous.Count, 0.2f / previous.Count);
                }
                yield return Track(SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive), 0.2f, 0.4f);
                var destination = SceneManager.GetSceneByName(sceneName);
                if (!destination.isLoaded) yield break;
                SceneManager.SetActiveScene(destination);
                foreach (var root in destination.GetRootGameObjects())
                {
                    var general = root.GetComponentInChildren<GameGeneralSceneController>();
                    if (general != null && general.isActiveAndEnabled && general.autoLoadChildren)
                        yield return general.LoadChildren(value => ReportProgress(0.6f + value * 0.35f));
                }
            }
            // Wait for destination Canvas Scalers and the first composed layout before revealing it.
            Canvas.ForceUpdateCanvases();
            ReportProgress(1f);
            yield return null;
            float minimum = float.IsNaN(minimumLoadingSeconds) || float.IsInfinity(minimumLoadingSeconds)
                ? 0f : Mathf.Max(0f, minimumLoadingSeconds);
            while (Time.realtimeSinceStartup - shownAt < minimum) yield return null;
            unloadingOverlay = SceneManager.UnloadSceneAsync(overlay);
            if (unloadingOverlay != null) yield return unloadingOverlay;
            unloadingOverlay = null;
        }
        finally
        {
            presentation = null;
            // Also release the overlay if this coroutine is cancelled.
            if (overlay.isLoaded && unloadingOverlay == null) unloadingOverlay = SceneManager.UnloadSceneAsync(overlay);
            IsLoading = false;
            if (!string.IsNullOrEmpty(pendingScene))
            {
                string next = pendingScene; pendingScene = null; LoadScene(next);
            }
        }
    }
    private IEnumerator Track(AsyncOperation operation, float start, float range)
    {
        if (operation == null) yield break;
        while (!operation.isDone)
        {
            ReportProgress(start + Mathf.Clamp01(operation.progress / 0.9f) * range);
            yield return null;
        }
        ReportProgress(start + range);
    }
    private void ReportProgress(float value)
    {
        LoadingProgress = Mathf.Max(LoadingProgress, Mathf.Clamp01(value));
        if (presentation != null) presentation.SetProgress(LoadingProgress);
    }
    private void OnDestroy() { if (instance == this) instance = null; }
}
