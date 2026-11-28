using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Upgrade 씬에서 구매한 스킬 노드를 저장하고 복원합니다.
[DefaultExecutionOrder(-50), DisallowMultipleComponent]
public sealed class SkillTreeProgressManager : MonoBehaviour
{
    public const string SaveFileName = "merge-box-upgrades.json";
    [Serializable] private sealed class Progress
    {
        public List<string> unlockedSkillIds = new List<string>();
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
        skillTree.skillProgress = this;
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
        if (store == null)
        {
            store = DefaultStore();
        }
        string json = GameManager.DebugMode ? null : store.Read();
        if (!string.IsNullOrEmpty(json)) progress = JsonUtility.FromJson<Progress>(json) ?? new Progress();
        if (progress.unlockedSkillIds == null) progress.unlockedSkillIds = new List<string>();
        loaded = true;
    }

    private static IMergeBoxUpgradeSaveStore DefaultStore()
        => new MergeBoxUpgradeFileStore(Path.Combine(Application.persistentDataPath, SaveFileName));

    // UpgradeManager가 기본 해금/버프 복원을 끝낸 후 호출합니다. 비용을 다시 차감하지 않습니다.
    public void Initialize()
    {
        Load();
        if (initialized || skillTree == null) return;
        skillTree.skillProgress = this;
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
            var unlocked = skillNodes.FindAll(node => node != null && (GameManager.DebugMode || progress.unlockedSkillIds.Contains(node.ProgressKey)));
            skillTree.RestoreUnlockedSkills(unlocked);
            if (GameManager.DebugMode)
                foreach (var node in unlocked)
                    if (!progress.unlockedSkillIds.Contains(node.ProgressKey)) progress.unlockedSkillIds.Add(node.ProgressKey);
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
        if (skillTree.IsUnlocked(node)) return;
        if (!GameManager.DebugMode && !progress.unlockedSkillIds.Contains(node.ProgressKey)) return;
        if (!progress.unlockedSkillIds.Contains(node.ProgressKey)) progress.unlockedSkillIds.Add(node.ProgressKey);
        restoring = true;
        try { skillTree.RestoreUnlockedSkills(new[] { node }); }
        finally { restoring = false; }
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
        if (!GameManager.DebugMode) store.Write(JsonUtility.ToJson(progress));
    }
    public List<SkillNodeData> CaptureUnlockedNodes()
    {
        Initialize();
        return skillNodes.FindAll(node => node != null && skillTree != null && skillTree.IsUnlocked(node));
    }
}
