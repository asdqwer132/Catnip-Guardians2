using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// 어느 씬에서 Play해도 코어를 먼저 초기화한 뒤 그 화면으로 이동합니다. 편집 중인 씬 배치는 변경하지 않습니다.
[InitializeOnLoad]
public static class CoreScenePlayBootstrap
{
    private const string CorePath = "Assets/Scenes/CoreScene.unity";
    static CoreScenePlayBootstrap() => EditorApplication.playModeStateChanged += OnPlayModeChanged;
    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            var previous = EditorSceneManager.playModeStartScene;
            SessionState.SetString("Catnip.PreviousPlayStart", previous != null ? AssetDatabase.GetAssetPath(previous) : "");
            var requested = previous != null ? AssetDatabase.GetAssetPath(previous) : SceneManager.GetActiveScene().path;
            SessionState.SetString("Catnip.SceneToPlay", GameSceneRoutes.Resolve(requested));
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(CorePath);
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            var path = SessionState.GetString("Catnip.PreviousPlayStart", "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            SessionState.EraseString("Catnip.SceneToPlay");
        }
    }
    [MenuItem("Catnip/Scenes/Open Core Additively")]
    private static void OpenCore()
    {
        if (!SceneManager.GetSceneByName(GameSceneRoutes.Core).isLoaded)
            EditorSceneManager.OpenScene(CorePath, OpenSceneMode.Additive);
    }
}
