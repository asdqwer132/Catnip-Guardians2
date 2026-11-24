using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Upgrade 씬의 구매 기록을 저장하고, 씬 오브젝트 없이 상자 레벨만 세션에 전달합니다.
[DefaultExecutionOrder(-50), DisallowMultipleComponent]
public sealed class MergeBoxUpgradeManager : MonoBehaviour
{
    public const string SaveFileName = "merge-box-upgrades.json";
    private const string DevelopmentSeedKey = "MergeBox_SkillLedgerSeeded";
    [Serializable] private sealed class Progress
    {
        public List<string> unlockedSkillIds = new List<string>();
        public List<int> developmentUnlockedChests = new List<int>();
        public List<MergeBoxLevelSnapshot> boxLevels = new List<MergeBoxLevelSnapshot>();
    }

    public SkillTreeManager skillTree;
    public List<SkillNodeData> skillNodes = new List<SkillNodeData>();
    [SerializeField] private Progress progress = new Progress();
    private IMergeBoxUpgradeSaveStore store;
    private bool loaded, restoring, initialized;
    private bool usesDefaultStore;

    private void Awake() => Load();
    private void OnEnable()
    {
        if (skillTree == null) return;
        skillTree.mergeBoxUpgrades = this;
        skillTree.OnSkillUnlocked += RegisterNode;
        skillTree.OnSkillTreeChanged += SaveProgress;
    }
    private void OnDisable()
    {
        if (skillTree == null) return;
        skillTree.OnSkillUnlocked -= RegisterNode;
        skillTree.OnSkillTreeChanged -= SaveProgress;
    }
    public void SetSaveStore(IMergeBoxUpgradeSaveStore value)
    {
        store = value; usesDefaultStore = false; progress = new Progress(); loaded = initialized = false;
    }
    private void Load()
    {
        if (loaded) return;
        if (store == null)
        {
            store = DefaultStore();
            usesDefaultStore = true;
        }
        string json = store.Read();
        if (!string.IsNullOrEmpty(json)) progress = JsonUtility.FromJson<Progress>(json) ?? new Progress();
        if (progress.unlockedSkillIds == null) progress.unlockedSkillIds = new List<string>();
        if (progress.boxLevels == null) progress.boxLevels = new List<MergeBoxLevelSnapshot>();
        progress.developmentUnlockedChests ??= new List<int>();
        if (usesDefaultStore) SeedDevelopmentUnlocks(store, progress);
        loaded = true;
    }

    private static IMergeBoxUpgradeSaveStore DefaultStore()
        => new MergeBoxUpgradeFileStore(Path.Combine(Application.persistentDataPath, SaveFileName));

    private static void SeedDevelopmentUnlocks(IMergeBoxUpgradeSaveStore store, Progress progress)
    {
        if (PlayerPrefs.HasKey(DevelopmentSeedKey)) return;
        var asset = Resources.Load<TextAsset>("Data/MergeBoxDevelopmentUnlocks");
        var seed = asset != null ? JsonUtility.FromJson<Progress>(asset.text) : null;
        if (seed?.developmentUnlockedChests == null) return;
        foreach (int number in seed.developmentUnlockedChests)
            if (number >= 2 && number <= 8 && !progress.developmentUnlockedChests.Contains(number))
                progress.developmentUnlockedChests.Add(number);
        try
        {
            store.Write(JsonUtility.ToJson(progress, true));
            SkipDevelopmentSeed();
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        { Debug.LogWarning("[MergeBoxUnlock] " + error.Message); }
    }

    public static void SkipDevelopmentSeed()
    {
        PlayerPrefs.SetInt(DevelopmentSeedKey, 1);
        PlayerPrefs.Save();
    }

    public static List<int> CaptureChestUnlocks(IEnumerable<SkillNodeData> nodes)
    {
        var store = DefaultStore();
        string json = store.Read();
        var progress = string.IsNullOrEmpty(json) ? new Progress() : JsonUtility.FromJson<Progress>(json) ?? new Progress();
        progress.developmentUnlockedChests ??= new List<int>();
        SeedDevelopmentUnlocks(store, progress);
        var result = new List<int> { 1 };
        foreach (int number in progress.developmentUnlockedChests) Add(number);
        if (nodes != null) foreach (var node in nodes)
        {
            if (node == null || node.rewards == null) continue;
            foreach (var reward in node.rewards)
                if (reward is UnlockSkillReward unlock && unlock.UnlockType == DataType.MergeBox &&
                    int.TryParse(unlock.UnlockId, out int number) && number.ToString() == unlock.UnlockId) Add(number);
        }
        return result;

        void Add(int number)
        {
            if (number >= 2 && number <= 8 && !result.Contains(number)) result.Add(number);
        }
    }

    // UpgradeManager가 기본 해금/버프 복원을 끝낸 후 호출합니다. 비용을 다시 차감하지 않습니다.
    public void Initialize()
    {
        Load();
        if (initialized || skillTree == null) return;
        skillTree.mergeBoxUpgrades = this;
        // Old saves stored only node IDs. Preserve their original first-match interpretation,
        // then use map-qualified identities for every subsequent purchase.
        for (int i = 0; i < progress.unlockedSkillIds.Count; i++)
        {
            string key = progress.unlockedSkillIds[i];
            if (skillNodes.Exists(node => node != null && node.ProgressKey == key)) continue;
            var legacy = skillNodes.Find(node => node != null && node.dataId == key);
            if (legacy != null) progress.unlockedSkillIds[i] = legacy.ProgressKey;
        }
        restoring = true;
        try
        {
            var unlocked = skillNodes.FindAll(node => node != null && progress.unlockedSkillIds.Contains(node.ProgressKey));
            skillTree.RestoreUnlockedSkills(unlocked);
            foreach (int number in progress.developmentUnlockedChests)
                UnlockManager.Instance?.Unlock(DataType.MergeBox, number.ToString());
            RebuildLevels();
            initialized = true;
        }
        finally { restoring = false; }
    }
    public void RegisterNode(SkillNodeData node)
    {
        if (node == null) return;
        // Scene nodes are authoritative. Replace stale catalog entries with the same ID
        // instead of allowing Find() to select an unrelated older asset's rewards.
        int replaced = skillNodes.RemoveAll(value => value != null && value != node && value.ProgressKey == node.ProgressKey);
        if (!skillNodes.Contains(node)) skillNodes.Add(node);
        if (replaced > 0)
            Debug.Log("[SkillTransfer] Replaced stale registered node with active node: " + node.name + " / " + node.ProgressKey, node);
        // 새로 작성한 UI 노드도 이전 구매 ID가 있으면 복원합니다.
        Load();
        if (!initialized || skillTree == null) return;
        if (skillTree.IsUnlocked(node)) { RebuildLevels(); return; }
        if (!progress.unlockedSkillIds.Contains(node.ProgressKey)) return;
        restoring = true;
        try { skillTree.RestoreUnlockedSkills(new[] { node }); RebuildLevels(); }
        finally { restoring = false; }
    }
    public void ApplyReward(ItemSeries series, int level)
    {
        Load();
        if (!MergeBoxLevelSnapshot.IsBoxSeries(series)) return;
        var entry = progress.boxLevels.Find(value => value.series == series);
        if (entry == null) progress.boxLevels.Add(new MergeBoxLevelSnapshot { series = series, level = Math.Max(1, level) });
        else entry.level = Math.Max(entry.level, level);
    }
    private void RebuildLevels()
    {
        progress.boxLevels.Clear();
        foreach (ItemSeries series in Enum.GetValues(typeof(ItemSeries)))
            if (MergeBoxLevelSnapshot.IsBoxSeries(series)) ApplyReward(series, 1);
        foreach (string id in progress.unlockedSkillIds)
        {
            var node = skillNodes.Find(value => value != null && value.ProgressKey == id);
            if (node == null || !skillTree.IsUnlocked(node) || node.rewards == null) continue;
            foreach (var reward in node.rewards)
            {
                if (reward is MergeBoxLevelSkillReward box) ApplyReward(box.series, box.level);
            }
        }
    }
    public void SaveProgress()
    {
        if (restoring || skillTree == null) return;
        Load();
        foreach (var node in skillNodes)
        {
            if (node == null || string.IsNullOrEmpty(node.dataId)) continue;
            if (skillTree.IsUnlocked(node))
            { if (!progress.unlockedSkillIds.Contains(node.ProgressKey)) progress.unlockedSkillIds.Add(node.ProgressKey); }
            else progress.unlockedSkillIds.Remove(node.ProgressKey);
        }
        RebuildLevels();
        store.Write(JsonUtility.ToJson(progress));
    }
    public List<SkillNodeData> CaptureUnlockedNodes()
    {
        Initialize();
        return skillNodes.FindAll(node => node != null && skillTree != null && skillTree.IsUnlocked(node));
    }

    public List<MergeBoxLevelSnapshot> CaptureLevels()
    {
        Initialize();
        return progress.boxLevels.ConvertAll(entry => entry.Copy());
    }
    public int GetLevel(ItemSeries series)
    {
        Initialize();
        return progress.boxLevels.Find(entry => entry.series == series)?.level ?? 1;
    }
}
