using UnityEngine;

public enum ScenePlayMode
{
    CurrentSceneOnly,
    CurrentSceneWithCore,
    CoreStartupScene
}

[CreateAssetMenu(menuName = "01.Game/Scene Play Settings", fileName = "ScenePlaySettings")]
public sealed class ScenePlaySettings : ScriptableObject
{
    [Tooltip("Current Scene Only: 현재 씬만 / With Core: 코어와 현재 씬만 / Core Startup Scene: 코어의 시작 설정 사용")]
    public ScenePlayMode playMode = ScenePlayMode.CurrentSceneOnly;
}
