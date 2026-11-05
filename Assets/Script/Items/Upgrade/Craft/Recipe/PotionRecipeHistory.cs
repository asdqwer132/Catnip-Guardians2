using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public interface IPotionRecipeHistoryStore
{
    string Read();
    void Write(string json);
}

public sealed class PotionRecipeFileStore : IPotionRecipeHistoryStore
{
    private readonly string path;
    public PotionRecipeFileStore(string path) => this.path = path;
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

// Only successful potion discoveries are persisted; inventory and scene references stay elsewhere.
[DisallowMultipleComponent]
public sealed class PotionRecipeHistory : MonoBehaviour
{
    public const string SaveFileName = "potion-discoveries.json";
    [Serializable] private sealed class SaveData { public List<string> craftedPotionIds = new List<string>(); }
    private readonly HashSet<string> crafted = new HashSet<string>(StringComparer.Ordinal);
    private IPotionRecipeHistoryStore store;
    private bool loaded;
    public event Action Changed;
    public int Count { get { EnsureLoaded(); return crafted.Count; } }
    private IPotionRecipeHistoryStore Store => store ?? (store = new PotionRecipeFileStore(
        Path.Combine(Application.persistentDataPath, SaveFileName)));
    public void SetSaveStore(IPotionRecipeHistoryStore value)
    {
        store = value ?? throw new ArgumentNullException(nameof(value));
        crafted.Clear(); loaded = false; Changed?.Invoke();
    }
    public bool HasCrafted(ItemData item)
    {
        EnsureLoaded();
        return item != null && !string.IsNullOrWhiteSpace(item.dataId) && crafted.Contains(item.dataId);
    }
    public bool RecordCrafted(ItemData item)
    {
        EnsureLoaded();
        if (item == null || string.IsNullOrWhiteSpace(item.dataId) || !crafted.Add(item.dataId)) return false;
        var data = new SaveData { craftedPotionIds = new List<string>(crafted) };
        data.craftedPotionIds.Sort(StringComparer.Ordinal);
        try { Store.Write(JsonUtility.ToJson(data, true)); }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        { Debug.LogWarning("[PotionRecipeHistory] Could not save discoveries: " + error.Message, this); }
        Changed?.Invoke(); return true;
    }
    private void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;
        try
        {
            string json = Store.Read();
            var data = string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<SaveData>(json);
            if (data?.craftedPotionIds != null)
                foreach (string id in data.craftedPotionIds)
                    if (!string.IsNullOrWhiteSpace(id)) crafted.Add(id);
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
        { Debug.LogWarning("[PotionRecipeHistory] Could not load discoveries: " + error.Message, this); }
    }
}
