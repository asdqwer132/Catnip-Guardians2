using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

internal enum SavePreferenceKind { Integer, Float, Boolean, String }

internal interface ISavePreferenceDeletion
{
    void DeleteKey(string key);
    void DeleteAll();
    void Save();
}

internal sealed class UnitySavePreferenceDeletion : ISavePreferenceDeletion
{
    public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
    public void DeleteAll() => PlayerPrefs.DeleteAll();
    public void Save() => PlayerPrefs.Save();
}

internal interface ISavePreferenceReader
{
    bool HasKey(string key);
    int GetInt(string key, int fallback);
    float GetFloat(string key, float fallback);
    string GetString(string key, string fallback);
}

internal sealed class UnitySavePreferenceReader : ISavePreferenceReader
{
    public bool HasKey(string key) => PlayerPrefs.HasKey(key);
    public int GetInt(string key, int fallback) => PlayerPrefs.GetInt(key, fallback);
    public float GetFloat(string key, float fallback) => PlayerPrefs.GetFloat(key, fallback);
    public string GetString(string key, string fallback) => PlayerPrefs.GetString(key, fallback);
}

internal sealed class SavePreferenceSnapshot
{
    public string Group, Label, Key, Value, DefaultValue;
    public SavePreferenceKind Kind;
    public bool HasKey;
}

internal sealed class SaveJsonSnapshot
{
    public string Path, Json = "", Summary = "", Error = "";
    public bool Exists;
    public long Bytes;
    public DateTime LastWriteUtc;
}

// Reads the storage APIs directly. It neither creates managers nor modifies their cached state.
internal static class SaveDataInspection
{
    public static void DeleteJson(string directory, string path)
    {
        string root = System.IO.Path.GetFullPath(directory).TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
        string target = System.IO.Path.GetFullPath(path);
        var comparison = Application.platform == RuntimePlatform.WindowsEditor ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!target.StartsWith(root, comparison) || !target.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only JSON files inside the save directory can be deleted.");
        for (string current = target; current != null && current.StartsWith(root, comparison); current = System.IO.Path.GetDirectoryName(current))
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Deletion through a symbolic link is not supported: " + current);
        if (File.Exists(target)) File.Delete(target);
        if (File.Exists(target + ".tmp")) File.Delete(target + ".tmp");
        if (string.Equals(System.IO.Path.GetFullPath(Application.persistentDataPath).TrimEnd(System.IO.Path.DirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar, root, comparison)
            && System.IO.Path.GetFileName(target) == CodexDiscoveryHistory.SaveFileName)
            CodexDiscoveryHistory.Current.Reload();
    }

    public static void DeleteAllJson(string directory)
    {
        var paths = ListJsonFiles(directory, out string error);
        if (!string.IsNullOrEmpty(error)) throw new IOException(error);
        foreach (string path in paths) DeleteJson(directory, path);
    }

    public static void DeletePreference(string key, ISavePreferenceDeletion storage = null)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A PlayerPrefs key is required.");
        storage = storage ?? new UnitySavePreferenceDeletion();
        storage.DeleteKey(key);
        storage.Save();
    }

    public static void DeleteAllPreferences(ISavePreferenceDeletion storage = null)
    {
        storage = storage ?? new UnitySavePreferenceDeletion();
        storage.DeleteAll();
        storage.Save();
    }

    private sealed class Preference
    {
        public string Group, Label, Key;
        public SavePreferenceKind Kind;
        public object Fallback;
        public Preference(string group, string label, string key, SavePreferenceKind kind, object fallback)
        { Group = group; Label = label; Key = key; Kind = kind; Fallback = fallback; }
    }

    private static readonly Preference[] preferences =
    {
        new Preference("Settings", "Master volume", SettingManager.MASTER_VOLUME, SavePreferenceKind.Float, 1f),
        new Preference("Settings", "BGM volume", SettingManager.BGM_VOLUME, SavePreferenceKind.Float, 1f),
        new Preference("Settings", "SFX volume", SettingManager.SFX_VOLUME, SavePreferenceKind.Float, 1f),
        new Preference("Settings", "Cursor scale", SettingManager.CURSOR_SCALE, SavePreferenceKind.Float, 1f),
        new Preference("Settings", "Indicator sprite size", SettingManager.INDICATOR_SPRITE_SIZE, SavePreferenceKind.Integer, 1),
        new Preference("Settings", "Show damage popup", SettingManager.SHOW_DAMAGE_POPUP, SavePreferenceKind.Boolean, 1),
        new Preference("Settings", "Show health bar", SettingManager.SHOW_HEALTH_BAR, SavePreferenceKind.Boolean, 1),
        new Preference("Settings", "Show buffs", SettingManager.SHOW_BUFF, SavePreferenceKind.Boolean, 1),
        new Preference("Story", "Chapter progress", StorySave.StoryChapterProgressKey, SavePreferenceKind.Integer, 0),
        new Preference("Tutorial", "Seen and completed tutorial IDs", TutorialCompletionHistory.SaveKey, SavePreferenceKind.String, "{}")
    };

    [Serializable] private sealed class Discoveries
    {
        public List<string> craftedPotionIds = new List<string>();
        public List<string> craftedItemIds = new List<string>();
    }
    [Serializable] private sealed class Upgrades
    {
        public List<string> unlockedSkillIds = new List<string>();
        public List<MergeBoxLevelSnapshot> boxLevels = new List<MergeBoxLevelSnapshot>();
    }

    [Serializable] private sealed class CodexDiscoveries
    {
        public List<string> discoveredItemIds = new List<string>();
        public List<string> discoveredEnemyIds = new List<string>();
        public List<string> discoveredCharacterIds = new List<string>();
        public List<string> discoveredMapIds = new List<string>();
    }

    public static List<string> ListJsonFiles(string directory, out string error)
    {
        error = "";
        var paths = new List<string>
        {
            System.IO.Path.Combine(directory, CodexDiscoveryHistory.SaveFileName),
            System.IO.Path.Combine(directory, PotionRecipeHistory.SaveFileName),
            System.IO.Path.Combine(directory, MergeDiscoveryHistory.SaveFileName),
            System.IO.Path.Combine(directory, MergeBoxUpgradeManager.SaveFileName)
        };
        try
        {
            if (Directory.Exists(directory))
                foreach (string path in Directory.GetFiles(directory, "*.json", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
                    if (!paths.Contains(path)) paths.Add(path);
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        { error = exception.Message; }
        return paths;
    }

    public static string JsonLabel(string path)
    {
        switch (System.IO.Path.GetFileName(path))
        {
            case CodexDiscoveryHistory.SaveFileName: return "Codex discoveries";
            case PotionRecipeHistory.SaveFileName: return "Potion recipe discoveries";
            case MergeDiscoveryHistory.SaveFileName: return "Merge item discoveries";
            case MergeBoxUpgradeManager.SaveFileName: return "Skills and merge box upgrades";
            default: return System.IO.Path.GetFileName(path);
        }
    }

    public static SaveJsonSnapshot ReadJson(string path)
    {
        var snapshot = new SaveJsonSnapshot { Path = path, Exists = File.Exists(path) };
        if (!snapshot.Exists) return snapshot;
        try
        {
            var file = new FileInfo(path);
            snapshot.Bytes = file.Length;
            snapshot.LastWriteUtc = file.LastWriteTimeUtc;
            snapshot.Json = File.ReadAllText(path);
            snapshot.Summary = DescribeJson(System.IO.Path.GetFileName(path), snapshot.Json);
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
        { snapshot.Error = exception.Message; }
        return snapshot;
    }

    private static string DescribeJson(string file, string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return "Empty file";
        if (file == CodexDiscoveryHistory.SaveFileName)
        {
            var data = JsonUtility.FromJson<CodexDiscoveries>(json);
            return DescribeIds("Discovered items", data?.discoveredItemIds) + "\n\n" +
                DescribeIds("Defeated enemies", data?.discoveredEnemyIds) + "\n\n" +
                DescribeIds("Characters", data?.discoveredCharacterIds) + "\n\n" +
                DescribeIds("Maps", data?.discoveredMapIds);
        }
        if (file == PotionRecipeHistory.SaveFileName || file == MergeDiscoveryHistory.SaveFileName)
        {
            var data = JsonUtility.FromJson<Discoveries>(json);
            var ids = file == PotionRecipeHistory.SaveFileName ? data?.craftedPotionIds : data?.craftedItemIds;
            return DescribeIds("Crafted items", ids);
        }
        if (file == MergeBoxUpgradeManager.SaveFileName)
        {
            var data = JsonUtility.FromJson<Upgrades>(json);
            var summary = new StringBuilder(DescribeIds("Unlocked skills", data?.unlockedSkillIds));
            summary.Append("\n\nBox levels");
            if (data?.boxLevels != null)
                foreach (var entry in data.boxLevels)
                    summary.Append(entry == null ? "\n<null>" : "\n" + entry.series + ": " + entry.level);
            return summary.ToString();
        }
        return "Additional JSON save. See the raw contents below.";
    }

    private static string DescribeIds(string title, List<string> ids)
    {
        var summary = new StringBuilder(title + ": " + (ids?.Count ?? 0));
        if (ids != null) foreach (var id in ids) summary.Append("\n" + (id ?? "<null>"));
        return summary.ToString();
    }

    public static List<SavePreferenceSnapshot> ReadPreferences(ISavePreferenceReader reader = null)
    {
        reader = reader ?? new UnitySavePreferenceReader();
        return preferences.Select(pref => ReadPreference(pref, reader)).ToList();
    }

    // PlayerPrefs has no cross-platform key enumeration API. Project keys are
    // listed above, and this lookup can inspect any additional explicitly named key.
    public static SavePreferenceSnapshot ReadCustomPreference(string key, SavePreferenceKind kind, ISavePreferenceReader reader = null)
    {
        object fallback = kind == SavePreferenceKind.String ? (object)"" : kind == SavePreferenceKind.Float ? (object)0f : 0;
        return ReadPreference(new Preference("Custom", "Custom key", key, kind, fallback), reader ?? new UnitySavePreferenceReader());
    }

    private static SavePreferenceSnapshot ReadPreference(Preference pref, ISavePreferenceReader reader)
    {
        string value, fallback;
        if (pref.Kind == SavePreferenceKind.Float)
        {
            fallback = ((float)pref.Fallback).ToString("R", CultureInfo.InvariantCulture);
            value = reader.GetFloat(pref.Key, (float)pref.Fallback).ToString("R", CultureInfo.InvariantCulture);
        }
        else if (pref.Kind == SavePreferenceKind.String)
        {
            fallback = (string)pref.Fallback;
            value = reader.GetString(pref.Key, fallback);
        }
        else
        {
            int result = reader.GetInt(pref.Key, (int)pref.Fallback);
            fallback = FormatInt((int)pref.Fallback, pref.Kind);
            value = FormatInt(result, pref.Kind);
        }
        return new SavePreferenceSnapshot
        {
            Group = pref.Group, Label = pref.Label, Key = pref.Key, Kind = pref.Kind,
            Value = value, DefaultValue = fallback, HasKey = reader.HasKey(pref.Key)
        };
    }

    private static string FormatInt(int value, SavePreferenceKind kind)
        => kind == SavePreferenceKind.Boolean ? (value == 1 ? "On" : "Off") + " (" + value + ")" : value.ToString(CultureInfo.InvariantCulture);
}
