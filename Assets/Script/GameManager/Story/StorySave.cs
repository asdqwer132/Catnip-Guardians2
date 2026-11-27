using UnityEngine;

public static class StorySave
{
    public const string StoryChapterProgressKey = "StoryChapterProgress";

    private static int debugChapter;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetDebugChapter() => debugChapter = 0;

    public static int GetStoryChapterProgress()
    {
        return GameManager.DebugMode ? debugChapter : PlayerPrefs.GetInt(StoryChapterProgressKey, 0);
    }

    public static void SetStoryChapterProgress(int chapterIndex)
    {
        if (GameManager.DebugMode) { debugChapter = chapterIndex; return; }
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
        if (GameManager.DebugMode) { debugChapter = 0; return; }
        PlayerPrefs.DeleteKey(StoryChapterProgressKey);
        PlayerPrefs.Save();
    }
}