using UnityEngine;

public static class TutorialSave
{
    private const string TutorialProgressKey = "TutorialProgress";

    public static TutorialProgress GetProgress()
    {
        int value = PlayerPrefs.GetInt(
            TutorialProgressKey,
            (int)TutorialProgress.FirstEnemyAttack
        );

        return (TutorialProgress)value;
    }

    public static void SetProgress(TutorialProgress progress)
    {
        PlayerPrefs.SetInt(TutorialProgressKey, (int)progress);
        PlayerPrefs.Save();
    }

    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(TutorialProgressKey);
        PlayerPrefs.Save();
    }
}