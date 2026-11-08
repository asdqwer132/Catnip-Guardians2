using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Gameplay reports facts; the receiving scene decides which authored tutorial to show.
public static class TutorialSignals
{
    public const string EnemySpawned = "enemy.spawned";
    public const string ItemDropped = "merge.item.dropped";
    public static event Action<Scene, string> Raised;
    private static readonly Dictionary<Scene, HashSet<string>> seen = new Dictionary<Scene, HashSet<string>>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        Raised = null;
        seen.Clear();
        SceneManager.sceneUnloaded -= ForgetScene;
        SceneManager.sceneUnloaded += ForgetScene;
    }

    private static void ForgetScene(Scene scene) => seen.Remove(scene);

    public static void Raise(Scene source, string eventId)
    {
        if (!source.IsValid() || !source.isLoaded || string.IsNullOrWhiteSpace(eventId)) return;
        if (!seen.TryGetValue(source, out var events)) seen.Add(source, events = new HashSet<string>());
        // Remember the first occurrence for late receivers; each receiver owns its once-only rule.
        events.Add(eventId);
        if (Raised == null) return;
        foreach (Action<Scene, string> listener in Raised.GetInvocationList())
            try { listener(source, eventId); }
            catch (Exception exception) { Debug.LogException(exception); }
    }

    // A General canvas loaded after its child scene still receives the first occurrence.
    public static void ReplaySeen(Action<Scene, string> listener)
    {
        foreach (var record in seen.ToArray())
            if (record.Key.IsValid() && record.Key.isLoaded)
                foreach (string eventId in record.Value.ToArray()) listener(record.Key, eventId);
    }
}
