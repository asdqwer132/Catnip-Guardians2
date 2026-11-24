using System;
using System.Collections.Generic;
using UnityEngine;

public static class TutorialCompletionHistory
{
    public const string SaveKey = "TutorialPanelCompletions";
    [Serializable] private sealed class SaveData
    {
        public List<string> seenTutorialIds = new List<string>();
        public List<string> completedTutorialIds = new List<string>();
    }

    public static bool HasSeen(string scope, string id)
    {
        var data = Read();
        string key = Key(scope, id);
        return data.seenTutorialIds.Contains(key) || data.completedTutorialIds.Contains(key);
    }

    public static void RecordSeen(string scope, string id)
    {
        var data = Read();
        string key = Key(scope, id);
        if (data.seenTutorialIds.Contains(key)) return;
        data.seenTutorialIds.Add(key);
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static bool IsCompleted(string scope, string id) => Read().completedTutorialIds.Contains(Key(scope, id));

    public static void Complete(string scope, string id)
    {
        var data = Read();
        string key = Key(scope, id);
        if (data.completedTutorialIds.Contains(key)) return;
        data.completedTutorialIds.Add(key);
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.DeleteKey("TutorialProgress");
        PlayerPrefs.Save();
    }

    private static string Key(string scope, string id) => scope + "\n" + id;
    private static SaveData Read()
    {
        try
        {
            var data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey, "{}"));
            if (data?.completedTutorialIds != null)
            {
                if (data.seenTutorialIds == null) data.seenTutorialIds = new List<string>();
                return data;
            }
        }
        catch (ArgumentException) { }
        return new SaveData();
    }
}
