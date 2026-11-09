using System;
using NUnit.Framework;

public class StoryProgressSourceTests
{
    private sealed class Source : IStoryProgressSource
    {
        public event Action<StoryProgressEvent> ProgressReported;
        public void Send() => ProgressReported?.Invoke(new StoryProgressEvent("source.finished"));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void SourcesAndSubscribersCanRegisterInEitherOrderAndUnregisterWithoutDuplicates(bool sourceFirst)
    {
        var source = new Source(); int calls = 0;
        Action<StoryProgressEvent> listener = fact => { Assert.That(fact.EventId, Is.EqualTo("source.finished")); calls++; };
        try
        {
            if (sourceFirst) StoryProgressSources.Register(source);
            StoryProgressSources.Reported += listener;
            if (!sourceFirst) StoryProgressSources.Register(source);
            StoryProgressSources.Register(source);
            source.Send(); Assert.That(calls, Is.EqualTo(1));
            StoryProgressSources.Unregister(source); source.Send(); Assert.That(calls, Is.EqualTo(1));
            StoryProgressSources.Register(source); source.Send(); Assert.That(calls, Is.EqualTo(2));
            StoryProgressSources.Reported -= listener; source.Send(); Assert.That(calls, Is.EqualTo(2));
        }
        finally { StoryProgressSources.Unregister(source); StoryProgressSources.Reported -= listener; }
    }
}
