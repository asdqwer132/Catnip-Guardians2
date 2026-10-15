using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// 기본값은 현재 씬만 실행합니다. 공용 매니저가 필요할 때만 설정 에셋에서 코어 동반 실행을 선택합니다.
[InitializeOnLoad]
public static class CoreScenePlayBootstrap
{
    private const string CorePath = "Assets/Scenes/CoreScene.unity";
    private const string SettingsPath = "Assets/Data/ScenePlaySettings.asset";
    static CoreScenePlayBootstrap() => EditorApplication.playModeStateChanged += OnPlayModeChanged;
    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            var previous = EditorSceneManager.playModeStartScene;
            SessionState.SetString("Catnip.PreviousPlayStart", previous != null ? AssetDatabase.GetAssetPath(previous) : "");
            string requested = SceneManager.GetActiveScene().path;
            var settings = AssetDatabase.LoadAssetAtPath<ScenePlaySettings>(SettingsPath);
            var mode = settings != null ? settings.playMode : ScenePlayMode.CurrentSceneOnly;
            SessionState.SetString("Catnip.SceneToPlay", SceneStartupPolicy.EditorCoreTarget(requested, mode));
            // 현재 씬 에셋을 지정하면 편집 중 여러 씬을 열어 뒀어도 선택한 씬만 Play합니다.
            string entry = SceneStartupPolicy.EditorEntry(requested, mode);
            string path = entry == GameSceneRoutes.Core ? CorePath : requested;
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            var path = SessionState.GetString("Catnip.PreviousPlayStart", "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            SessionState.EraseString("Catnip.SceneToPlay");
        }
    }
    [MenuItem("Catnip/Scenes/Select Play Settings")]
    private static void SelectSettings() => Selection.activeObject = AssetDatabase.LoadAssetAtPath<ScenePlaySettings>(SettingsPath);
    [MenuItem("Catnip/Scenes/Open Core Additively")]
    private static void OpenCore()
    {
        if (!SceneManager.GetSceneByName(GameSceneRoutes.Core).isLoaded)
        {
            var active = SceneManager.GetActiveScene();
            EditorSceneManager.OpenScene(CorePath, OpenSceneMode.Additive);
            if (active.IsValid()) SceneManager.SetActiveScene(active);
        }
    }
}
