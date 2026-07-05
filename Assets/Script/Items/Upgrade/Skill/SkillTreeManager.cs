using System;
using System.Collections.Generic;
using UnityEngine;

public class SkillTreeManager : MonoBehaviour
{
    public static SkillTreeManager Instance { get; private set; }

    public BuffSkillManager buffSkillManager;

    public event Action OnSkillTreeChanged;
    public event Action<SkillNodeData> OnSkillUnlocked;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    // 전체 스킬 트리에서 해금된 모든 스킬 ID
    private readonly HashSet<string> unlockedSkillIds
        = new HashSet<string>();

    // 스킬 맵별 해금된 스킬 ID
    // Key   : SkillMapData.dataId
    // Value : 해당 맵에서 해금된 SkillNodeData.dataId 목록
    private readonly Dictionary<string, HashSet<string>> unlockedSkillIdsByMap
        = new Dictionary<string, HashSet<string>>();

    private SkillApplyContext context;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        CreateContext();
    }

    private void CreateContext()
    {
        context = new SkillApplyContext
        {
            skillTreeManager = this,
            unlockManager = UnlockManager.Instance,
            buffSkillManager = buffSkillManager
        };
    }

    private void RefreshContext()
    {
        if (context == null)
        {
            context = new SkillApplyContext();
        }

        context.skillTreeManager = this;
        context.unlockManager = UnlockManager.Instance;
        context.buffSkillManager = buffSkillManager;
    }

    public bool CanUnlock(SkillNodeData node)
    {
        if (node == null)
            return false;

        if (string.IsNullOrEmpty(node.dataId))
        {
            Debug.LogWarning("스킬 ID가 비어있습니다: " + node.name);
            return false;
        }

        if (IsUnlocked(node.dataId))
            return false;

        if (!HasRequiredSkills(node))
            return false;

        if (!HasEnoughCost(node))
            return false;

        return true;
    }

    public bool UnlockSkill(SkillNodeData node)
    {
        if (!CanUnlock(node))
        {
            if (debugLog)
                Debug.Log("스킬 해금 실패: " + GetSkillName(node));

            return false;
        }

        if (!SpendCost(node))
        {
            if (debugLog)
                Debug.Log("스킬 비용 소모 실패: " + GetSkillName(node));

            return false;
        }

        // 전체 해금 목록에 등록
        unlockedSkillIds.Add(node.dataId);

        // 해당 스킬 맵의 해금 목록에도 등록
        AddUnlockedSkillToMap(node);

        RefreshContext();
        ApplyRewards(node);

        OnSkillUnlocked?.Invoke(node);
        OnSkillTreeChanged?.Invoke();

        RefreshBroadcaster.Instance?.Broadcast(
            RefreshType.SkillTree |
            RefreshType.Unlock |
            RefreshType.Currency |
            RefreshType.Shop |
            RefreshType.Inventory |
            RefreshType.Bag |
            RefreshType.Equipment
        );

        if (debugLog)
        {
            Debug.Log(
                $"스킬 해금 성공: {node.GetDataName()} " +
                $"/ 맵 해금 개수: {GetUnlockedSkillCount(node.skillMap)}"
            );
        }

        return true;
    }

    private void AddUnlockedSkillToMap(SkillNodeData node)
    {
        if (node == null)
            return;

        if (node.skillMap == null)
        {
            if (debugLog)
            {
                Debug.LogWarning(
                    $"스킬 노드에 스킬 맵이 지정되지 않았습니다: {node.name}",
                    node
                );
            }

            return;
        }

        string mapId = node.skillMap.dataId;

        if (string.IsNullOrEmpty(mapId))
        {
            Debug.LogWarning(
                $"스킬 맵 ID가 비어있습니다. " +
                $"Node: {node.name}, Map: {node.skillMap.name}",
                node.skillMap
            );

            return;
        }

        if (!unlockedSkillIdsByMap.TryGetValue(
                mapId,
                out HashSet<string> mapSkillIds))
        {
            mapSkillIds = new HashSet<string>();
            unlockedSkillIdsByMap.Add(mapId, mapSkillIds);
        }

        mapSkillIds.Add(node.dataId);
    }

    private bool HasEnoughCost(SkillNodeData node)
    {
        if (node == null)
            return false;

        if (CurrencyManager.instance == null)
        {
            Debug.LogWarning("CurrencyManager.instance가 없습니다.");
            return false;
        }

        return CurrencyManager.instance.HasCurrencies(node.costs);
    }

    private bool SpendCost(SkillNodeData node)
    {
        if (node == null)
            return false;

        if (CurrencyManager.instance == null)
        {
            Debug.LogWarning("CurrencyManager.instance가 없습니다.");
            return false;
        }

        return CurrencyManager.instance.SpendCurrencies(node.costs);
    }

    private bool HasRequiredSkills(SkillNodeData node)
    {
        if (node == null)
            return false;

        // 요구 스킬이 없으면 시작 노드이므로 해금 가능
        if (node.requiredSkills == null || node.requiredSkills.Count == 0)
            return true;

        bool hasValidRequiredSkill = false;

        for (int i = 0; i < node.requiredSkills.Count; i++)
        {
            SkillNodeData requiredSkill = node.requiredSkills[i];

            if (requiredSkill == null)
                continue;

            if (string.IsNullOrEmpty(requiredSkill.dataId))
                continue;

            hasValidRequiredSkill = true;

            // 하나라도 해금되어 있으면 통과
            if (IsUnlocked(requiredSkill.dataId))
                return true;
        }

        // 리스트는 있지만 유효한 요구 스킬이 없으면 시작 노드처럼 처리
        if (!hasValidRequiredSkill)
            return true;

        return false;
    }

    private void ApplyRewards(SkillNodeData node)
    {
        if (node == null || node.rewards == null)
            return;

        for (int i = 0; i < node.rewards.Count; i++)
        {
            SkillRewardData reward = node.rewards[i];

            if (reward == null)
                continue;

            reward.Apply(context);
        }
    }

    // 전체 스킬 트리 기준 해금 여부
    public bool IsUnlocked(string skillId)
    {
        if (string.IsNullOrEmpty(skillId))
            return false;

        return unlockedSkillIds.Contains(skillId);
    }

    // 특정 스킬 맵 기준 해금 여부
    public bool IsUnlockedInMap(
        SkillMapData skillMap,
        string skillId)
    {
        if (skillMap == null)
            return false;

        return IsUnlockedInMap(skillMap.dataId, skillId);
    }

    public bool IsUnlockedInMap(
        string mapId,
        string skillId)
    {
        if (string.IsNullOrEmpty(mapId))
            return false;

        if (string.IsNullOrEmpty(skillId))
            return false;

        if (!unlockedSkillIdsByMap.TryGetValue(
                mapId,
                out HashSet<string> mapSkillIds))
        {
            return false;
        }

        return mapSkillIds.Contains(skillId);
    }

    // 특정 스킬 맵에서 해금된 노드 개수
    public int GetUnlockedSkillCount(SkillMapData skillMap)
    {
        if (skillMap == null)
            return 0;


        return GetUnlockedSkillCount(skillMap.dataId);
    }

    public int GetUnlockedSkillCount(string mapId)
    {
        if (string.IsNullOrEmpty(mapId))
            return 0;

        if (!unlockedSkillIdsByMap.TryGetValue(
                mapId,
                out HashSet<string> mapSkillIds))
        {
            return 0;
        }

        return mapSkillIds.Count;
    }

    // 특정 스킬 맵에서 해금된 노드 ID 목록
    // 외부에서 내부 HashSet을 변경하지 못하도록 복사본 반환
    public HashSet<string> GetUnlockedSkillIds(SkillMapData skillMap)
    {
        if (skillMap == null)
            return new HashSet<string>();

        return GetUnlockedSkillIds(skillMap.dataId);
    }

    public HashSet<string> GetUnlockedSkillIds(string mapId)
    {
        if (string.IsNullOrEmpty(mapId))
            return new HashSet<string>();

        if (!unlockedSkillIdsByMap.TryGetValue(
                mapId,
                out HashSet<string> mapSkillIds))
        {
            return new HashSet<string>();
        }

        return new HashSet<string>(mapSkillIds);
    }

    // 특정 맵의 해금 정보만 초기화
    public void ClearSkillsInMap(SkillMapData skillMap)
    {
        if (skillMap == null)
            return;

        ClearSkillsInMap(skillMap.dataId);
    }

    public void ClearSkillsInMap(string mapId)
    {
        if (string.IsNullOrEmpty(mapId))
            return;

        if (!unlockedSkillIdsByMap.TryGetValue(
                mapId,
                out HashSet<string> mapSkillIds))
        {
            return;
        }

        // 전체 해금 목록에서도 해당 맵의 스킬들을 제거
        foreach (string skillId in mapSkillIds)
        {
            unlockedSkillIds.Remove(skillId);
        }

        unlockedSkillIdsByMap.Remove(mapId);

        OnSkillTreeChanged?.Invoke();

        if (debugLog)
            Debug.Log($"스킬 맵 해금 정보 초기화: {mapId}");
    }

    public void ClearAllSkills()
    {
        unlockedSkillIds.Clear();
        unlockedSkillIdsByMap.Clear();

        OnSkillTreeChanged?.Invoke();

        if (debugLog)
            Debug.Log("모든 스킬 해금 정보 초기화");
    }

    private string GetSkillName(SkillNodeData node)
    {
        if (node == null)
            return "NULL";

        if (!string.IsNullOrEmpty(node.GetDataName()))
            return node.GetDataName();

        return node.name;
    }
}