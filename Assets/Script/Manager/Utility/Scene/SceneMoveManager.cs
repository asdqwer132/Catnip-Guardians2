using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMoveManager : MonoBehaviour
{
    public static SceneMoveManager instance;

    [Header("Loading UI")]
    public GameObject loadingPanel;

    private bool isLoading;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("이동할 씬 이름이 비어있습니다.");
            return;
        }

        if (isLoading)
            return;

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    public void LoadScene(int sceneIndex)
    {
        if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning($"잘못된 씬 인덱스입니다: {sceneIndex}");
            return;
        }

        if (isLoading)
            return;

        StartCoroutine(LoadSceneRoutine(sceneIndex));
    }

    public void ReloadCurrentScene()
    {
        if (isLoading)
            return;

        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        StartCoroutine(LoadSceneRoutine(currentSceneIndex));
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isLoading = true;

        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        while (!operation.isDone)
            yield return null;

        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        isLoading = false;
    }

    private IEnumerator LoadSceneRoutine(int sceneIndex)
    {
        isLoading = true;

        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);

        while (!operation.isDone)
            yield return null;

        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        isLoading = false;
    }
}