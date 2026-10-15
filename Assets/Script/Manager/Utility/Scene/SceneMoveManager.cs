using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMoveManager : MonoBehaviour
{
    public static SceneMoveManager instance;
    [Header("직접 만든 로딩 패널")]
    public GameObject loadingPanel;
    public bool IsLoading { get; private set; }
    private string pendingScene;
    private void Awake()
    {
        if (instance == null || gameObject.scene.name == GameSceneRoutes.Core) instance = this;
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }
    public void GoGameScene() => LoadScene(GameSceneRoutes.General);
    public void GoUpgradeScene() => LoadScene(GameSceneRoutes.Upgrade);
    public void GoMainScene() => LoadScene(GameSceneRoutes.Main);
    public void GoStoryScene() => LoadScene(GameSceneRoutes.Story);
    public void LoadScene(string sceneName)
    {
        if (instance != null && instance != this) { instance.LoadScene(sceneName); return; }
        sceneName = GameSceneRoutes.Resolve(sceneName);
        if (string.IsNullOrEmpty(sceneName)) return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        { Debug.LogError($"빌드 목록에 없는 씬입니다: {sceneName}", this); return; }
        if (!SceneManager.GetSceneByName(GameSceneRoutes.Core).isLoaded)
        {
            if (IsLoading) return;
            // 독립 테스트에서는 코어를 요구하지 않습니다. 새 씬의 로컬 로더가 이어받습니다.
            IsLoading = true; Time.timeScale = 1f;
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single); return;
        }
        if (sceneName == GameSceneRoutes.Core) return;
        if (IsLoading) { pendingScene = sceneName; return; }
        StartCoroutine(LoadSceneRoutine(sceneName));
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
    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        IsLoading = true;
        if (loadingPanel != null) loadingPanel.SetActive(true);
        try
        {
            if (SettingWindowController.instance != null) SettingWindowController.instance.CloseSetting();
            Time.timeScale = 1f;
            var core = SceneManager.GetSceneByName(GameSceneRoutes.Core);
            SceneManager.SetActiveScene(core);
            var previous = new List<Scene>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene != core) previous.Add(scene);
            }
            // 기존 싱글턴 정리 후 다음 씬을 열어 중복 매니저 생성을 막습니다.
            foreach (var scene in previous)
                if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            var destination = SceneManager.GetSceneByName(sceneName);
            if (!destination.isLoaded) yield break;
            SceneManager.SetActiveScene(destination);
            foreach (var root in destination.GetRootGameObjects())
            {
                var general = root.GetComponentInChildren<GameGeneralSceneController>();
                if (general != null && general.autoLoadChildren) yield return general.LoadChildren();
            }
        }
        finally
        {
            if (loadingPanel != null) loadingPanel.SetActive(false);
            IsLoading = false;
            if (!string.IsNullOrEmpty(pendingScene))
            {
                string next = pendingScene; pendingScene = null; LoadScene(next);
            }
        }
    }
    private void OnDestroy() { if (instance == this) instance = null; }
}
