using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneBgmManager : MonoBehaviour
{
    [System.Serializable]
    public class SceneBgmData
    {
        public string sceneName;
        public string bgmSoundName;
        public bool stopBgm;
    }

    [Header("Scene BGM List")]
    public SceneBgmData[] sceneBgms;

    [Header("Option")]
    public bool playOnStart = true;
    public bool stopWhenNoMatch = false;

    private string lastSceneName;


    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (playOnStart)
            PlayBgmByCurrentScene();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayBgmBySceneName(scene.name);
    }

    public void PlayBgmByCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        PlayBgmBySceneName(sceneName);
    }

    public void PlayBgmBySceneName(string sceneName)
    {
        if (lastSceneName == sceneName)
            return;

        lastSceneName = sceneName;

        SceneBgmData data = GetSceneBgmData(sceneName);

        if (data == null)
        {
            if (stopWhenNoMatch && AudioManager.instance != null)
                AudioManager.instance.StopBgm();

            return;
        }

        if (AudioManager.instance == null)
        {
            Debug.LogWarning("AudioManager가 없습니다.");
            return;
        }

        if (data.stopBgm)
        {
            AudioManager.instance.StopBgm();
            return;
        }

        if (string.IsNullOrEmpty(data.bgmSoundName))
            return;

        AudioManager.instance.PlayBgm(data.bgmSoundName);
    }

    private SceneBgmData GetSceneBgmData(string sceneName)
    {
        for (int i = 0; i < sceneBgms.Length; i++)
        {
            if (sceneBgms[i] == null)
                continue;

            if (sceneBgms[i].sceneName == sceneName)
                return sceneBgms[i];
        }

        return null;
    }
}