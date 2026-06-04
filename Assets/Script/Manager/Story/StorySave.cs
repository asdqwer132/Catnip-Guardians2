using UnityEngine;

public static class StorySave
{
    private const string StoryChapterProgressKey = "StoryChapterProgress";

    public static int GetStoryChapterProgress()
    {
        return PlayerPrefs.GetInt(StoryChapterProgressKey, 0);
    }

    public static void SetStoryChapterProgress(int chapterIndex)
    {
        PlayerPrefs.SetInt(StoryChapterProgressKey, chapterIndex);
        PlayerPrefs.Save();
    }

    public static void MoveToNextChapter()
    {
        int chapterIndex = GetStoryChapterProgress();
        SetStoryChapterProgress(chapterIndex + 1);
    }

    public static bool HasSeenPrologue()
    {
        return GetStoryChapterProgress() >= 1;
    }

    public static void ResetStoryProgress()
    {
        PlayerPrefs.DeleteKey(StoryChapterProgressKey);
        PlayerPrefs.Save();
    }
}