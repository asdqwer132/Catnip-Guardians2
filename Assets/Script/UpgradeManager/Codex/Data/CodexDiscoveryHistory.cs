using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public interface ICodexDiscoveryStore
{
    string Read();
    void Write(string json);
}

public sealed class CodexDiscoveryFileStore : ICodexDiscoveryStore
{
    private readonly string path;
    public CodexDiscoveryFileStore(string path) => this.path = path ?? throw new ArgumentNullException(nameof(path));
    public string Read() => File.Exists(path) ? File.ReadAllText(path) : null;
    public void Write(string json)
    {
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, json);
        if (File.Exists(path)) File.Replace(temporary, path, null);
        else File.Move(temporary, path);
    }
}

public enum CodexDiscoveryKind { Item, Enemy, Character, Map }

// Scene-independent discovery storage. These records describe what the player
// has discovered, independently of owned stock and crafted-only recipe histories.
public sealed class CodexDiscoveryHistory
{
    public const string SaveFileName = "codex-discoveries.json";
    [Serializable]
    private sealed class SaveData
    {
        public List<string> discoveredItemIds = new List<string>();
        public List<string> discoveredEnemyIds = new List<string>();
        public List<string> discoveredCharacterIds = new List<string>();
        public List<string> discoveredMapIds = new List<string>();
    }

    private static CodexDiscoveryHistory current;
    public static CodexDiscoveryHistory Current => current ?? (current = new CodexDiscoveryHistory(
        new CodexDiscoveryFileStore(Path.Combine(Application.persistentDataPath, SaveFileName))));

    private readonly ICodexDiscoveryStore store;
    private readonly HashSet<string> items = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> enemies = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> characters = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> maps = new HashSet<string>(StringComparer.Ordinal);
    private bool loaded, dirty;
    public event Action Changed;

    public CodexDiscoveryHistory(ICodexDiscoveryStore store)
        => this.store = store ?? throw new ArgumentNullException(nameof(store));

    public int ItemCount { get { EnsureLoaded(); return items.Count; } }
    public int EnemyCount { get { EnsureLoaded(); return enemies.Count; } }
    public bool HasItem(ItemData item) => item != null && Contains(CodexDiscoveryKind.Item, item.dataId);
    public bool HasEnemy(EnemyDataSet enemy) => enemy != null && Contains(CodexDiscoveryKind.Enemy, enemy.dataId);
    public bool RecordItem(ItemData item) => item != null && Record(CodexDiscoveryKind.Item, item.dataId);
    public bool RecordEnemy(EnemyDataSet enemy) => enemy != null && Record(CodexDiscoveryKind.Enemy, enemy.dataId);

    public bool Contains(CodexDiscoveryKind kind, string id)
    {
        return EnsureLoaded() && !string.IsNullOrWhiteSpace(id) && Collection(kind).Contains(id);
    }

    public bool Record(CodexDiscoveryKind kind, string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !EnsureLoaded()) return false;
        if (!Collection(kind).Add(id))
        {
            // Retry an earlier failed disk write without duplicating discovery notifications.
            if (dirty) Save();
            return false;
        }
        dirty = true;
        Save();
        Changed?.Invoke();
        return true;
    }

    // Used after an editor deletion, and when entering Play without a domain reload.
    // Keep the same object so enabled UI listeners are not left on an obsolete instance.
    public void Reload()
    {
        items.Clear(); enemies.Clear(); characters.Clear(); maps.Clear();
        loaded = dirty = false;
        EnsureLoaded();
        Changed?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReloadForSession()
    {
        if (current != null) current.Reload();
    }

    // A scoped save adapter also lets independent tests use memory or temporary files.
    public static IDisposable OverrideCurrent(CodexDiscoveryHistory value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        var previous = current;
        current = value;
        return new CurrentScope(previous);
    }

    private sealed class CurrentScope : IDisposable
    {
        private readonly CodexDiscoveryHistory previous;
        private bool disposed;
        public CurrentScope(CodexDiscoveryHistory previous) => this.previous = previous;
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            current = previous;
        }
    }

    private HashSet<string> Collection(CodexDiscoveryKind kind)
    {
        switch (kind)
        {
            case CodexDiscoveryKind.Item: return items;
            case CodexDiscoveryKind.Enemy: return enemies;
            case CodexDiscoveryKind.Character: return characters;
            case CodexDiscoveryKind.Map: return maps;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private bool EnsureLoaded()
    {
        if (loaded) return true;
        try
        {
            string json = store.Read();
            var data = string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<SaveData>(json);
            if (data != null)
            {
                ReadIds(items, data.discoveredItemIds);
                ReadIds(enemies, data.discoveredEnemyIds);
                ReadIds(characters, data.discoveredCharacterIds);
                ReadIds(maps, data.discoveredMapIds);
            }
            loaded = true;
            return true;
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
        {
            Debug.LogWarning("[CodexDiscoveryHistory] Could not load discoveries: " + error.Message);
            // A malformed document can start an empty history. An unreadable file may
            // still contain valid records: retry later and never overwrite it meanwhile.
            loaded = error is ArgumentException;
            return loaded;
        }
    }

    private static void ReadIds(HashSet<string> target, List<string> values)
    {
        if (values == null) return;
        foreach (string id in values) if (!string.IsNullOrWhiteSpace(id)) target.Add(id);
    }

    private static List<string> Sorted(HashSet<string> values)
    {
        var result = new List<string>(values); result.Sort(StringComparer.Ordinal); return result;
    }

    private void Save()
    {
        var data = new SaveData
        {
            discoveredItemIds = Sorted(items), discoveredEnemyIds = Sorted(enemies),
            discoveredCharacterIds = Sorted(characters), discoveredMapIds = Sorted(maps)
        };
        try { store.Write(JsonUtility.ToJson(data, true)); dirty = false; }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        { Debug.LogWarning("[CodexDiscoveryHistory] Could not save discoveries: " + error.Message); }
    }
}
