using UnityEngine;
using UnityEngine.SceneManagement;

// Core owns this service; gameplay reports facts and UI confirms them separately.
public sealed class StoryProgressManager : MonoBehaviour
{
    public const string SaveKey = "StoryProgression";
    public const string BattleResult = "battle-result";
    public const string PotionResult = "potion-result";
    public const string ChestResult = "chest-result";
    public static StoryProgressManager Instance { get; private set; }
    public StoryProgressRule[] rules;
    public string storyScene = GameSceneRoutes.Story;
    public SceneMoveManager sceneLoader;
    public string[] automaticScenes = { GameSceneRoutes.General, GameSceneRoutes.Battle, GameSceneRoutes.Item, GameSceneRoutes.Upgrade };
    private StoryProgressTracker tracker;
    private StoryProgressRule playing;
    private string returnScene;
    public bool IsPlayingStory => playing != null;
    public StoryData PlayingStory => playing != null ? playing.story : null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        ReloadProgress();
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    private void Update() => TryStartReady(null);
    private void Save() { PlayerPrefs.SetString(SaveKey, tracker.ToJson()); PlayerPrefs.Save(); }
    public void ReloadProgress()
    {
        playing = null;
        tracker = new StoryProgressTracker(rules, PlayerPrefs.GetString(SaveKey, ""));
    }
    public void Report(StoryProgressEvent fact) { if (tracker.Report(fact)) Save(); }
    public void MeetCondition(string ruleId) { if (tracker.MeetCondition(ruleId)) Save(); }
    public void ReportCustomEvent(string eventId) => Report(new StoryProgressEvent(StoryProgressEventKind.Custom, eventId: eventId));

    // Returns true when a story owns the next transition, so the caller must not load another scene.
    public bool Confirm(string area, string fallbackReturnScene = null)
    {
        if (tracker.Confirm(area)) Save();
        return TryStartReady(fallbackReturnScene) || playing != null;
    }
    public void ConfirmArea(string area) => Confirm(area);
    public bool IsCompleted(string ruleId) => tracker.IsCompleted(ruleId);

    private bool TryStartReady(string fallbackReturnScene)
    {
        if (tracker == null || playing != null) return false;
        if (System.Array.IndexOf(automaticScenes ?? System.Array.Empty<string>(), SceneManager.GetActiveScene().name) < 0) return false;
        var loader = sceneLoader != null ? sceneLoader : SceneMoveManager.instance;
        if (loader == null || loader.IsLoading || SceneManager.GetSceneByName(GameSceneRoutes.Resolve(storyScene)).isLoaded) return false;
        var ready = tracker.NextReady();
        if (ready == null || !Application.CanStreamedLevelBeLoaded(GameSceneRoutes.Resolve(storyScene))) return false;
        playing = ready;
        returnScene = !string.IsNullOrWhiteSpace(ready.returnScene) ? ready.returnScene
            : !string.IsNullOrWhiteSpace(fallbackReturnScene) ? fallbackReturnScene : SceneManager.GetActiveScene().name;
        loader.LoadScene(storyScene);
        return true;
    }
    public string CompletePlayingStory()
    {
        string destination = returnScene;
        if (playing != null) { tracker.Complete(playing.id); Save(); }
        playing = null;
        return destination;
    }
}
