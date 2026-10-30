using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public interface IMergeDiscoveryStore
{
    string Read();
    void Write(string json);
    void Delete();
}

// 제작 기록만 저장합니다. 재고, 레시피북, 씬 오브젝트는 저장하지 않습니다.
public sealed class MergeDiscoveryFileStore : IMergeDiscoveryStore
{
    private readonly string path;
    public MergeDiscoveryFileStore(string path) => this.path = path;
    public string Read() => File.Exists(path) ? File.ReadAllText(path) : null;
    public void Write(string json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, json);
        if (File.Exists(path)) File.Replace(temporary, path, null);
        else File.Move(temporary, path);
    }
    public void Delete()
    {
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
    }
}

[DisallowMultipleComponent]
public sealed class MergeDiscoveryHistory : MonoBehaviour
{
    [Serializable]
    private sealed class SaveData
    {
        public List<string> craftedItemIds = new List<string>();
    }

    private readonly HashSet<string> crafted = new HashSet<string>(StringComparer.Ordinal);
    private IMergeDiscoveryStore store;
    private bool loaded;
    public event Action Changed;
    public int Count { get { EnsureLoaded(); return crafted.Count; } }
    private IMergeDiscoveryStore Store => store ?? (store = new MergeDiscoveryFileStore(
        Path.Combine(Application.persistentDataPath, "merge-discoveries.json")));

    public void SetSaveStore(IMergeDiscoveryStore value)
    {
        store = value ?? throw new ArgumentNullException(nameof(value));
        crafted.Clear();
        loaded = false;
        Changed?.Invoke();
    }

    public bool HasCrafted(ItemData item)
    {
        EnsureLoaded();
        return item != null && !string.IsNullOrWhiteSpace(item.dataId) && crafted.Contains(item.dataId);
    }

    // 실제 합성 확정 후에만 호출합니다. 투입/풀 생성/아이템 선택은 제작으로 세지 않습니다.
    public bool RecordCrafted(ItemData item)
    {
        EnsureLoaded();
        if (item == null || string.IsNullOrWhiteSpace(item.dataId) || !crafted.Add(item.dataId)) return false;
        var data = new SaveData { craftedItemIds = new List<string>(crafted) };
        data.craftedItemIds.Sort(StringComparer.Ordinal);
        try { Store.Write(JsonUtility.ToJson(data, true)); }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        { Debug.LogWarning("[MergeDiscoveryHistory] Could not save crafting history: " + error.Message, this); }
        Changed?.Invoke();
        return true;
    }

    [ContextMenu("Delete Saved Crafting History")]
    public void ClearHistory()
    {
        try { Store.Delete(); }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            Debug.LogWarning("[MergeDiscoveryHistory] Could not delete crafting history: " + error.Message, this);
            return;
        }
        crafted.Clear();
        loaded = true;
        Changed?.Invoke();
    }

    private void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;
        try
        {
            string json = Store.Read();
            var data = string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<SaveData>(json);
            if (data?.craftedItemIds != null)
                foreach (string id in data.craftedItemIds)
                    if (!string.IsNullOrWhiteSpace(id)) crafted.Add(id);
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
        { Debug.LogWarning("[MergeDiscoveryHistory] Could not load crafting history: " + error.Message, this); }
    }
}
