using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class CodexDiscoveryTests
{
    private sealed class MemoryStore : ICodexDiscoveryStore
    {
        public string json;
        public int writes;
        public bool failWrites;
        public Exception readFailure;
        public string Read()
        {
            if (readFailure != null) throw readFailure;
            return json;
        }
        public void Write(string value)
        {
            if (failWrites) throw new IOException("Test write failure");
            json = value; writes++;
        }
    }
    private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
    private T Data<T>(string id) where T : DefaultData
    {
        var value = ScriptableObject.CreateInstance<T>(); value.dataId = id; owned.Add(value); return value;
    }
    [TearDown]
    public void TearDown()
    {
        foreach (var value in owned) if (value != null) UnityEngine.Object.DestroyImmediate(value);
        owned.Clear();
    }

    [Test]
    public void ItemAndEnemyDiscoveryUseStableIdsAndDeduplicateWithinEachCategory()
    {
        var store = new MemoryStore(); var history = new CodexDiscoveryHistory(store);
        var item = Data<ItemData>("shared-id"); var enemy = Data<EnemyDataSet>("shared-id");
        int changes = 0; history.Changed += () => changes++;
        Assert.That(history.HasItem(item), Is.False); Assert.That(store.writes, Is.Zero);
        Assert.That(history.RecordItem(item), Is.True); Assert.That(history.HasEnemy(enemy), Is.False);
        Assert.That(history.RecordItem(Data<ItemData>("shared-id")), Is.False);
        Assert.That(history.RecordEnemy(enemy), Is.True); Assert.That(history.RecordEnemy(enemy), Is.False);
        Assert.That(history.ItemCount, Is.EqualTo(1)); Assert.That(history.EnemyCount, Is.EqualTo(1));
        Assert.That(store.writes, Is.EqualTo(2)); Assert.That(changes, Is.EqualTo(2));
        var reloaded = new CodexDiscoveryHistory(store);
        Assert.That(reloaded.HasItem(item) && reloaded.HasEnemy(enemy), Is.True);
    }

    [Test]
    public void FileStorePersistsBothKindsAndReloadReflectsDeletionWithoutRecreatingTheFile()
    {
        string directory = Path.Combine(Path.GetTempPath(), "catnip-codex-discovery-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, CodexDiscoveryHistory.SaveFileName);
        try
        {
            var history = new CodexDiscoveryHistory(new CodexDiscoveryFileStore(path));
            Assert.That(history.ItemCount, Is.Zero); Assert.That(Directory.Exists(directory), Is.False);
            var first = Data<ItemData>("item-z"); var second = Data<ItemData>("item-a"); var enemy = Data<EnemyDataSet>("enemy");
            history.RecordItem(first); history.RecordItem(second); history.RecordEnemy(enemy);
            var reloaded = new CodexDiscoveryHistory(new CodexDiscoveryFileStore(path));
            Assert.That(reloaded.HasItem(first) && reloaded.HasItem(second) && reloaded.HasEnemy(enemy), Is.True);
            string json = File.ReadAllText(path);
            Assert.That(json.IndexOf("item-a", StringComparison.Ordinal), Is.LessThan(json.IndexOf("item-z", StringComparison.Ordinal)));
            int changes = 0; history.Changed += () => changes++;
            File.Delete(path); history.Reload();
            Assert.That(history.ItemCount + history.EnemyCount, Is.Zero); Assert.That(changes, Is.EqualTo(1));
            Assert.That(File.Exists(path), Is.False);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Test]
    public void InvalidJsonAndEmptyIdsCannotCreateFalseDiscoveriesOrOverwriteOnRead()
    {
        var store = new MemoryStore { json = "{broken" };
        var history = new CodexDiscoveryHistory(store);
        LogAssert.Expect(LogType.Warning, new Regex(@"\[CodexDiscoveryHistory\] Could not load discoveries:"));
        Assert.That(history.ItemCount, Is.Zero); Assert.That(history.EnemyCount, Is.Zero);
        Assert.That(history.RecordItem(null), Is.False);
        Assert.That(history.RecordItem(Data<ItemData>(" ")), Is.False);
        Assert.That(history.RecordEnemy(Data<EnemyDataSet>("")), Is.False);
        Assert.That(store.writes, Is.Zero); Assert.That(store.json, Is.EqualTo("{broken"));
    }

    [Test]
    public void OtherCategoriesAndDuplicateStoredIdsSurviveAnItemDiscoverySave()
    {
        var store = new MemoryStore { json = "{\"discoveredItemIds\":[\"first\",\"first\",null,\"\"],\"discoveredEnemyIds\":[\"enemy\"],\"discoveredCharacterIds\":[\"character\"],\"discoveredMapIds\":[\"map\"]}" };
        var history = new CodexDiscoveryHistory(store);
        Assert.That(history.ItemCount, Is.EqualTo(1)); history.RecordItem(Data<ItemData>("second"));
        var reloaded = new CodexDiscoveryHistory(store);
        Assert.That(reloaded.ItemCount, Is.EqualTo(2));
        Assert.That(reloaded.Contains(CodexDiscoveryKind.Enemy, "enemy"), Is.True);
        Assert.That(reloaded.Contains(CodexDiscoveryKind.Character, "character"), Is.True);
        Assert.That(reloaded.Contains(CodexDiscoveryKind.Map, "map"), Is.True);
    }

    [Test]
    public void FailedDiskWriteCanRetryWithoutDuplicatingDiscoveryOrNotifications()
    {
        var store = new MemoryStore { failWrites = true }; var history = new CodexDiscoveryHistory(store);
        var item = Data<ItemData>("item"); int changes = 0; history.Changed += () => changes++;
        LogAssert.Expect(LogType.Warning, new Regex(@"\[CodexDiscoveryHistory\] Could not save discoveries:"));
        Assert.That(history.RecordItem(item), Is.True); Assert.That(history.HasItem(item), Is.True);
        store.failWrites = false;
        Assert.That(history.RecordItem(item), Is.False);
        Assert.That(store.writes, Is.EqualTo(1)); Assert.That(changes, Is.EqualTo(1));
        Assert.That(new CodexDiscoveryHistory(store).HasItem(item), Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TransientReadFailureBlocksWritesAndRetriesWithoutLosingExistingDiscoveries(bool unauthorized)
    {
        string original = "{\"discoveredItemIds\":[\"old-item\"],\"discoveredEnemyIds\":[\"old-enemy\"]}";
        var store = new MemoryStore
        {
            json = original,
            readFailure = unauthorized ? (Exception)new UnauthorizedAccessException("Test read failure") : new IOException("Test read failure")
        };
        var history = new CodexDiscoveryHistory(store);
        var added = Data<ItemData>("new-item");
        int changes = 0; history.Changed += () => changes++;
        LogAssert.Expect(LogType.Warning, new Regex(@"\[CodexDiscoveryHistory\] Could not load discoveries:"));
        Assert.That(history.RecordItem(added), Is.False);
        Assert.That(store.writes, Is.Zero); Assert.That(store.json, Is.EqualTo(original));
        Assert.That(changes, Is.Zero);

        store.readFailure = null;
        Assert.That(history.RecordItem(added), Is.True, "The next call must retry loading before saving new discoveries.");
        Assert.That(store.writes, Is.EqualTo(1)); Assert.That(changes, Is.EqualTo(1));
        var reloaded = new CodexDiscoveryHistory(store);
        Assert.That(reloaded.HasItem(Data<ItemData>("old-item")), Is.True);
        Assert.That(reloaded.HasEnemy(Data<EnemyDataSet>("old-enemy")), Is.True);
        Assert.That(reloaded.HasItem(added), Is.True);
        Assert.That(reloaded.ItemCount, Is.EqualTo(2)); Assert.That(reloaded.EnemyCount, Is.EqualTo(1));
    }

    [Test]
    public void ScopedCurrentAdaptersRestorePreviousHistoryWithoutTouchingDefaultStorage()
    {
        var original = CodexDiscoveryHistory.Current;
        var first = new CodexDiscoveryHistory(new MemoryStore());
        var second = new CodexDiscoveryHistory(new MemoryStore());
        using (CodexDiscoveryHistory.OverrideCurrent(first))
        {
            Assert.That(CodexDiscoveryHistory.Current, Is.SameAs(first));
            using (CodexDiscoveryHistory.OverrideCurrent(second))
                Assert.That(CodexDiscoveryHistory.Current, Is.SameAs(second));
            Assert.That(CodexDiscoveryHistory.Current, Is.SameAs(first));
        }
        Assert.That(CodexDiscoveryHistory.Current, Is.SameAs(original));
    }
}
