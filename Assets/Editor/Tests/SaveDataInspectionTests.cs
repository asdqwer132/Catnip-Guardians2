using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class SaveDataInspectionTests
{
    private sealed class Preferences : ISavePreferenceReader
    {
        public readonly Dictionary<string, object> values = new Dictionary<string, object>();
        public bool HasKey(string key) => values.ContainsKey(key);
        public int GetInt(string key, int fallback) => values.TryGetValue(key, out var value) ? (int)value : fallback;
        public float GetFloat(string key, float fallback) => values.TryGetValue(key, out var value) ? (float)value : fallback;
        public string GetString(string key, string fallback) => values.TryGetValue(key, out var value) ? (string)value : fallback;
    }
    private string directory;

    [SetUp]
    public void SetUp() => directory = Path.Combine(Path.GetTempPath(), "catnip-save-viewer-" + Guid.NewGuid().ToString("N"));
    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }
    private string Write(string name, string contents)
    {
        var path = Path.Combine(directory, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, contents);
        return path;
    }

    [Test]
    public void MissingSavesAreListedWithoutCreatingFilesOrDirectories()
    {
        var paths = SaveDataInspection.ListJsonFiles(directory, out string error);
        Assert.That(error, Is.Empty); Assert.That(paths.Count, Is.EqualTo(3));
        foreach (string path in paths)
        {
            var data = SaveDataInspection.ReadJson(path);
            Assert.That(data.Exists, Is.False); Assert.That(data.Json, Is.Empty); Assert.That(data.Error, Is.Empty);
        }
        Assert.That(Directory.Exists(directory), Is.False);
    }

    [TestCase(PotionRecipeHistory.SaveFileName, "craftedPotionIds")]
    [TestCase(MergeDiscoveryHistory.SaveFileName, "craftedItemIds")]
    public void DiscoveryFilesShowExactIdsAndRawContentsWithoutChangingStoredBytes(string file, string field)
    {
        string contents = "{\"" + field + "\":[\"Potion A\",\"한글 아이템\"]}";
        string path = Write(file, contents);
        var before = File.ReadAllBytes(path); DateTime modified = File.GetLastWriteTimeUtc(path);
        var data = SaveDataInspection.ReadJson(path);
        Assert.That(data.Exists, Is.True); Assert.That(data.Error, Is.Empty); Assert.That(data.Json, Is.EqualTo(contents));
        Assert.That(data.Summary, Does.Contain("Crafted items: 2").And.Contain("Potion A").And.Contain("한글 아이템"));
        Assert.That(data.Bytes, Is.EqualTo(before.Length));
        Assert.That(File.ReadAllBytes(path), Is.EqualTo(before)); Assert.That(File.GetLastWriteTimeUtc(path), Is.EqualTo(modified));
    }

    [Test]
    public void UpgradeSaveShowsUnlockedSkillsAndNamedSeriesLevels()
    {
        string path = Write(MergeBoxUpgradeManager.SaveFileName,
            "{\"unlockedSkillIds\":[\"skill-x\"],\"boxLevels\":[{\"series\":1,\"level\":3},{\"series\":2,\"level\":2}]}");
        var data = SaveDataInspection.ReadJson(path);
        Assert.That(data.Error, Is.Empty);
        Assert.That(data.Summary, Does.Contain("skill-x").And.Contain("Weapon: 3").And.Contain("Food: 2"));
    }

    [Test]
    public void AdditionalJsonFilesAreListedIncludingSubfoldersWithoutDuplicatingKnownFiles()
    {
        Write(PotionRecipeHistory.SaveFileName, "{}");
        string additional = Write("additional.json", "{\"coins\":12}");
        string nested = Write(Path.Combine("slot-2", "other.json"), "[]");
        var paths = SaveDataInspection.ListJsonFiles(directory, out string error);
        Assert.That(error, Is.Empty); Assert.That(paths.Count, Is.EqualTo(5)); Assert.That(paths.Distinct().Count(), Is.EqualTo(5));
        Assert.That(paths, Does.Contain(additional).And.Contain(nested));
        Assert.That(SaveDataInspection.ReadJson(additional).Json, Does.Contain("12"));
    }

    [Test]
    public void CorruptKnownSaveStillExposesRawContentsWithoutWritingOverIt()
    {
        string path = Write(PotionRecipeHistory.SaveFileName, "{broken");
        var data = SaveDataInspection.ReadJson(path);
        Assert.That(data.Error, Is.Not.Empty); Assert.That(data.Json, Is.EqualTo("{broken"));
        Assert.That(File.ReadAllText(path), Is.EqualTo("{broken"));
        Assert.That(SaveDataInspection.ReadPreferences(new Preferences()).Count, Is.EqualTo(10));
    }

    [Test]
    public void ProjectPreferenceCatalogCoversRuntimeKeysAndClearlyShowsMissingDefaults()
    {
        var data = SaveDataInspection.ReadPreferences(new Preferences());
        var runtimeKeys = typeof(SettingManager).GetFields(BindingFlags.Static | BindingFlags.Public)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string)).Select(f => (string)f.GetRawConstantValue())
            .Concat(new[] { StorySave.StoryChapterProgressKey, TutorialSave.TutorialProgressKey });
        Assert.That(data.Select(row => row.Key), Is.EquivalentTo(runtimeKeys));
        Assert.That(data.All(row => !row.HasKey && row.Value == row.DefaultValue), Is.True);
        Assert.That(data.Single(row => row.Key == SettingManager.MASTER_VOLUME).Value, Is.EqualTo("1"));
        Assert.That(data.Single(row => row.Key == SettingManager.SHOW_BUFF).Value, Is.EqualTo("On (1)"));
        Assert.That(data.Single(row => row.Key == TutorialSave.TutorialProgressKey).Value,
            Is.EqualTo(((int)TutorialProgress.FirstEnemyAttack).ToString(CultureInfo.InvariantCulture)));
    }

    [Test]
    public void TypedPreferencesDistinguishStoredZeroFromMissingAndCustomKeysCanBeInspected()
    {
        var prefs = new Preferences();
        prefs.values[SettingManager.MASTER_VOLUME] = 0.25f;
        prefs.values[SettingManager.SHOW_BUFF] = 0;
        prefs.values[StorySave.StoryChapterProgressKey] = 3;
        prefs.values["custom-string"] = "saved text";
        var data = SaveDataInspection.ReadPreferences(prefs);
        var volume = data.Single(row => row.Key == SettingManager.MASTER_VOLUME);
        Assert.That(volume.HasKey, Is.True); Assert.That(volume.Value, Is.EqualTo("0.25"));
        var buff = data.Single(row => row.Key == SettingManager.SHOW_BUFF);
        Assert.That(buff.HasKey, Is.True); Assert.That(buff.Value, Is.EqualTo("Off (0)"));
        Assert.That(data.Single(row => row.Key == SettingManager.BGM_VOLUME).HasKey, Is.False);
        Assert.That(data.Single(row => row.Key == StorySave.StoryChapterProgressKey).Value, Is.EqualTo("3"));
        Assert.That(SaveDataInspection.ReadCustomPreference("custom-string", SavePreferenceKind.String, prefs).Value, Is.EqualTo("saved text"));
        Assert.That(prefs.values.Count, Is.EqualTo(4), "Inspection cannot create missing keys.");
    }

    [Test]
    public void ViewerCanRefreshWithoutLoadingGameplayScenesOrManagers()
    {
        var window = ScriptableObject.CreateInstance<SaveDataViewerWindow>();
        try { Assert.DoesNotThrow(window.RefreshData); }
        finally { UnityEngine.Object.DestroyImmediate(window); }
    }
}
