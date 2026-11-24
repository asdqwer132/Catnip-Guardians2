using System.Collections.Generic;
using UnityEngine;

// Each overlay releases only its own pause. Input flags set by gameplay remain independent.
public static class GamePause
{
    private static readonly HashSet<object> timeOwners = new HashSet<object>();
    private static readonly HashSet<object> inputOwners = new HashSet<object>();
    private static float resumeTimeScale = 1f;
    public static bool IsPaused => timeOwners.Count > 0;
    public static bool IsGameplayBlocked => inputOwners.Count > 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        timeOwners.Clear();
        inputOwners.Clear();
        resumeTimeScale = 1f;
    }

    public static void Acquire(object owner, bool pauseTime = true)
    {
        if (owner == null) return;
        inputOwners.Add(owner);
        if (!pauseTime) return;
        if (timeOwners.Add(owner) && timeOwners.Count == 1) resumeTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

    public static void Release(object owner)
    {
        if (owner == null) return;
        inputOwners.Remove(owner);
        if (timeOwners.Remove(owner) && timeOwners.Count == 0) Time.timeScale = resumeTimeScale;
    }

    // Starting a new round/scene may reset the intended speed without unpausing an overlay.
    public static void SetResumeTimeScale(float value)
    {
        resumeTimeScale = float.IsNaN(value) || float.IsInfinity(value) ? 1f : Mathf.Clamp(value, 0f, 100f);
        Time.timeScale = IsPaused ? 0f : resumeTimeScale;
    }
}
