using System.Collections.Generic;
using UnityEngine;

public class UnlockManager : MonoBehaviour
{
    public static UnlockManager Instance { get; private set; }

    [Header("Default Unlock Rewards")]
    [Tooltip("게임 시작 시 기본으로 해금할 UnlockSkillReward 에셋들을 넣으세요.")]
    [SerializeField]
    private List<UnlockSkillReward> defaultUnlockRewards =
        new List<UnlockSkillReward>();

    [Header("Runtime Unlocked List")]
    [Tooltip("플레이 중 현재 해금된 목록 확인용입니다. 실제 로직에서는 사용하지 마세요.")]
    [SerializeField]
    private List<UnlockedDebugInfo> unlockedDebugList =
        new List<UnlockedDebugInfo>();

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private readonly Dictionary<DataType, HashSet<string>> unlockedByType =
        new Dictionary<DataType, HashSet<string>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ApplyDefaultUnlockRewards();
        RefreshDebugList();
    }

    private void ApplyDefaultUnlockRewards()
    {
        for (int i = 0; i < defaultUnlockRewards.Count; i++)
        {
            UnlockSkillReward reward = defaultUnlockRewards[i];

            if (reward == null)
                continue;

            Unlock(reward.UnlockType, reward.UnlockId);
        }
    }

    public void Unlock(DataType type, string unlockId)
    {
        if (string.IsNullOrEmpty(unlockId))
        {
            Debug.LogWarning("해금 ID가 비어있습니다. Type: " + type);
            return;
        }

        if (!unlockedByType.TryGetValue(type, out HashSet<string> ids))
        {
            ids = new HashSet<string>();
            unlockedByType.Add(type, ids);
        }

        bool added = ids.Add(unlockId);

        RefreshDebugList();

        if (debugLog)
        {
            if (added)
                Debug.Log("해금됨: " + type + " / " + unlockId);
            else
                Debug.Log("이미 해금됨: " + type + " / " + unlockId);
        }
    }

    public void Lock(DataType type, string unlockId)
    {
        if (string.IsNullOrEmpty(unlockId))
            return;

        if (!unlockedByType.TryGetValue(type, out HashSet<string> ids))
            return;

        bool removed = ids.Remove(unlockId);

        if (ids.Count == 0)
        {
            unlockedByType.Remove(type);
        }

        RefreshDebugList();

        if (debugLog && removed)
        {
            Debug.Log("잠금됨: " + type + " / " + unlockId);
        }
    }

    public bool IsUnlocked(DataType type, string unlockId)
    {
        if (string.IsNullOrEmpty(unlockId))
            return false;

        if (!unlockedByType.TryGetValue(type, out HashSet<string> ids))
            return false;

        return ids.Contains(unlockId);
    }

    public bool IsLocked(DataType type, string unlockId)
    {
        return !IsUnlocked(type, unlockId);
    }

    public bool HasAnyUnlocked(DataType type)
    {
        if (!unlockedByType.TryGetValue(type, out HashSet<string> ids))
            return false;

        return ids.Count > 0;
    }

    public List<string> GetUnlockedIds(DataType type)
    {
        if (!unlockedByType.TryGetValue(type, out HashSet<string> ids))
            return new List<string>();

        return new List<string>(ids);
    }

    public Dictionary<DataType, List<string>> GetAllUnlockedByType()
    {
        Dictionary<DataType, List<string>> result =
            new Dictionary<DataType, List<string>>();

        foreach (KeyValuePair<DataType, HashSet<string>> pair in unlockedByType)
        {
            result.Add(pair.Key, new List<string>(pair.Value));
        }

        return result;
    }

    public void ClearAllUnlocks()
    {
        unlockedByType.Clear();
        ApplyDefaultUnlockRewards();
        RefreshDebugList();
    }

    private void RefreshDebugList()
    {
        unlockedDebugList.Clear();

        foreach (KeyValuePair<DataType, HashSet<string>> pair in unlockedByType)
        {
            DataType type = pair.Key;
            HashSet<string> ids = pair.Value;

            foreach (string id in ids)
            {
                unlockedDebugList.Add(new UnlockedDebugInfo
                {
                    unlockType = type,
                    unlockId = id
                });
            }
        }
    }
}