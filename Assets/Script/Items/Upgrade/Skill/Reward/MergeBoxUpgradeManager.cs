using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Upgrade 씬의 구매 기록을 저장하고, 씬 오브젝트 없이 상자 레벨만 세션에 전달합니다.
[DefaultExecutionOrder(-50), DisallowMultipleComponent]
public sealed class MergeBoxUpgradeManager : MonoBehaviour
{
    [Serializable] private sealed class Progress
    {
        public List<string> unlockedSkillIds = new List<string>();
        public List<MergeBoxLevelSnapshot> boxLevels = new List<MergeBoxLevelSnapshot>();
    }

    public SkillTreeManager skillTree;
    public List<SkillNodeData> skillNodes = new List<SkillNodeData>();
    [SerializeField] private Progress progress = new Progress();
    private IMergeBoxUpgradeSaveStore store;
    private bool loaded, restoring, initialized;

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
        store = value; progress = new Progress(); loaded = initialized = false;
    }
    private void Load()
    {
        if (loaded) return;
        if (store == null) store = new MergeBoxUpgradeFileStore(Path.Combine(Application.persistentDataPath, "merge-box-upgrades.json"));
        string json = store.Read();
        if (!string.IsNullOrEmpty(json)) progress = JsonUtility.FromJson<Progress>(json) ?? new Progress();
        if (progress.unlockedSkillIds == null) progress.unlockedSkillIds = new List<string>();
        if (progress.boxLevels == null) progress.boxLevels = new List<MergeBoxLevelSnapshot>();
        loaded = true;
    }

    // UpgradeManager가 기본 해금/버프 복원을 끝낸 후 호출합니다. 비용을 다시 차감하지 않습니다.
    public void Initialize()
    {
        Load();
        if (initialized || skillTree == null) return;
        skillTree.mergeBoxUpgrades = this;
        restoring = true;
        try
        {
            var unlocked = skillNodes.FindAll(node => node != null && progress.unlockedSkillIds.Contains(node.dataId));
            skillTree.RestoreUnlockedSkills(unlocked);
            RebuildLevels();
            initialized = true;
        }
        finally { restoring = false; }
    }
    public void RegisterNode(SkillNodeData node)
    {
        if (node == null) return;
        if (!skillNodes.Contains(node)) skillNodes.Add(node);
        // 새로 작성한 UI 노드도 이전 구매 ID가 있으면 복원합니다.
        Load();
        if (!initialized || skillTree == null || skillTree.IsUnlocked(node.dataId) || !progress.unlockedSkillIds.Contains(node.dataId)) return;
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
        foreach (var node in skillNodes)
            if (node != null && skillTree.IsUnlocked(node.dataId) && node.rewards != null)
                foreach (var reward in node.rewards)
                    if (reward is MergeBoxLevelSkillReward box) ApplyReward(box.series, box.level);
    }
    public void SaveProgress()
    {
        if (restoring || skillTree == null) return;
        Load();
        foreach (var node in skillNodes)
        {
            if (node == null || string.IsNullOrEmpty(node.dataId)) continue;
            if (skillTree.IsUnlocked(node.dataId))
            { if (!progress.unlockedSkillIds.Contains(node.dataId)) progress.unlockedSkillIds.Add(node.dataId); }
            else progress.unlockedSkillIds.Remove(node.dataId);
        }
        RebuildLevels();
        store.Write(JsonUtility.ToJson(progress));
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
