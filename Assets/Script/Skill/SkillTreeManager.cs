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

    private readonly HashSet<string> unlockedSkillIds = new HashSet<string>();

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

        if (string.IsNullOrEmpty(node.skillId))
        {
            Debug.LogWarning("스킬 ID가 비어있습니다: " + node.name);
            return false;
        }

        if (IsUnlocked(node.skillId))
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

        unlockedSkillIds.Add(node.skillId);

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
            Debug.Log("스킬 해금 성공: " + node.skillName);

        return true;
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
        if (node.requiredSkills == null)
            return true;

        for (int i = 0; i < node.requiredSkills.Count; i++)
        {
            SkillNodeData requiredSkill = node.requiredSkills[i];

            if (requiredSkill == null)
                continue;

            if (string.IsNullOrEmpty(requiredSkill.skillId))
                continue;

            if (!IsUnlocked(requiredSkill.skillId))
                return false;
        }

        return true;
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

    public bool IsUnlocked(string skillId)
    {
        if (string.IsNullOrEmpty(skillId))
            return false;

        return unlockedSkillIds.Contains(skillId);
    }

    public void ClearAllSkills()
    {
        unlockedSkillIds.Clear();

        OnSkillTreeChanged?.Invoke();

        if (debugLog)
            Debug.Log("모든 스킬 해금 정보 초기화");
    }

    private string GetSkillName(SkillNodeData node)
    {
        if (node == null)
            return "NULL";

        if (!string.IsNullOrEmpty(node.skillName))
            return node.skillName;

        return node.name;
    }
}