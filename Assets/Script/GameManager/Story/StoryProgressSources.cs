using System;
using System.Collections.Generic;
using UnityEngine;

// Sources register on enable. Subscribers can appear before or after any source.
public static class StoryProgressSources
{
    private static readonly HashSet<IStoryProgressSource> sources = new HashSet<IStoryProgressSource>();
    public static event Action<StoryProgressEvent> Reported;

    public static void Register(IStoryProgressSource source)
    {
        if (source != null && sources.Add(source)) source.ProgressReported += Forward;
    }
    public static void Unregister(IStoryProgressSource source)
    {
        if (source != null && sources.Remove(source)) source.ProgressReported -= Forward;
    }
    private static void Forward(StoryProgressEvent fact) => Reported?.Invoke(fact);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        foreach (var source in sources) source.ProgressReported -= Forward;
        sources.Clear();
        Reported = null;
    }
}
