using System;
using System.IO;
using UnityEngine;

// New Game clears player saves, never assets or unrelated files in persistentDataPath.
public static class GameSaveData
{
    private static readonly string[] JsonSaveFileNames =
    {
        CodexDiscoveryHistory.SaveFileName,
        PotionRecipeHistory.SaveFileName,
        MergeDiscoveryHistory.SaveFileName,
        MergeBoxUpgradeManager.SaveFileName
    };

    public static bool ResetAll()
    {
        try
        {
            DeleteJsonSaves(Application.persistentDataPath);
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            Debug.LogError("Could not reset saved game data: " + error.Message);
            return false;
        }

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        // Core services survive scene changes: deleting files alone leaves their old values alive.
        StoryProgressManager.Instance?.ReloadProgress();
        CodexDiscoveryHistory.Current.Reload();
        GameSession.Instance?.ClearAll();
        SceneDataCarrier.Clear();
        GameStatisticsManager.Instance?.ResetRound();
        CurrencyManager.instance?.ResetToInitialAmounts();
        SettingManager.instance?.ReloadSettings();
        SceneMoveManager.instance?.SetAcquiredCharacters(null);
        return true;
    }

    public static void DeleteJsonSaves(string directory)
    {
        foreach (string fileName in JsonSaveFileNames)
        {
            string path = Path.Combine(directory, fileName);
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
        }
    }
}
