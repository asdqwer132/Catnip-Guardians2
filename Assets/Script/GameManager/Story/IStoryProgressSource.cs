using System;

public interface IStoryProgressSource
{
    event Action<StoryProgressEvent> ProgressReported;
}
