public static class SceneStartupPolicy
{
    public static string EditorEntry(string currentScene, ScenePlayMode mode)
        => mode == ScenePlayMode.CurrentSceneOnly ? GameSceneRoutes.Resolve(currentScene) : GameSceneRoutes.Core;

    public static string EditorCoreTarget(string currentScene, ScenePlayMode mode)
    {
        string current = GameSceneRoutes.Resolve(currentScene);
        return mode == ScenePlayMode.CurrentSceneWithCore && current != GameSceneRoutes.Core ? current : "";
    }

    public static string CoreDestination(string configuredScene, bool autoLoad, bool coreIsActive, bool onlyWhenCoreIsActive, string editorTarget)
    {
        string target = GameSceneRoutes.Resolve(editorTarget);
        if (string.IsNullOrEmpty(target))
        {
            if (!autoLoad || onlyWhenCoreIsActive && !coreIsActive) return "";
            target = GameSceneRoutes.Resolve(configuredScene);
        }
        return target == GameSceneRoutes.Core ? "" : target;
    }
}
