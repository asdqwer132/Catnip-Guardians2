using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class GameScreenLayoutProfileSync
{
    static GameScreenLayoutProfileSync() => EditorSceneManager.sceneSaved += SaveProfile;

    private static void SaveProfile(Scene scene)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var controller in root.GetComponentsInChildren<GameGeneralSceneController>())
                controller.SaveUIRegions();
    }
}
