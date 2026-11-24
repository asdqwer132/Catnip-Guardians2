using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Loading Character", menuName = "UI/Loading Character Animation")]
public sealed class LoadingCharacterAnimation : ScriptableObject
{
    [Tooltip("Run sprites in playback order. Uses the already sliced sprite sheet.")]
    public Sprite[] runFrames = Array.Empty<Sprite>();
    [Min(0.1f)] public float framesPerSecond = 12f;
    public bool flipHorizontal;

    public Sprite FrameAt(float seconds)
    {
        if (runFrames == null || runFrames.Length == 0) return null;
        return runFrames[Mathf.FloorToInt(seconds * Mathf.Max(0.1f, framesPerSecond)) % runFrames.Length];
    }
}
