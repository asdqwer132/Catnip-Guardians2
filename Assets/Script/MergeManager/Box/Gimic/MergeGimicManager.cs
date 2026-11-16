using System;
using UnityEngine;
using UnityEngine.Events;

// 아이템 씬의 라운드 진입점: 시리즈/레벨 선택, 기믹 수명, 스킬 쿨타임을 관리합니다.
[DefaultExecutionOrder(-90)]
public class MergeGimicManager : MonoBehaviour
{
    public MergeBoxSceneController boxes;
    [Min(0)] public int startingSeriesIndex;
    [Min(1)] public int startingLevel = 1;
    public bool useSessionLevels = true;
    [SerializeField] private CoolButton[] skillButtons = Array.Empty<CoolButton>();

    [ReadOnly]
    [SerializeField] private MergeBoxGimic currentBox;
    private UpgradeToGeneralData roundData;
    private UnityAction[] handlers;
    private float[] readyAt = Array.Empty<float>();
    public MergeBoxGimic CurrentGimic => currentBox;
    public int SelectedIndex { get; private set; } = -1;
    public int SelectedLevel => boxes != null && boxes.SelectedBoxManager != null ? boxes.SelectedBoxManager.CurrentLevel : startingLevel;

    private void Awake()
    {
        if (boxes != null) boxes.gameManager = this;
    }
    private void Start()
    {
        if (SelectedIndex >= 0) return;
        if (GameSession.Instance != null && GameSession.Instance.TryReadGeneralData(out var data)) roundData = data;
        StartRound(startingSeriesIndex, ResolveLevel(startingSeriesIndex));
    }
    private void OnDisable() { currentBox?.GimicStop(); ClearSkills(); }
    private void OnEnable()
    {
        if (SelectedIndex >= 0) StartRound(SelectedIndex, SelectedLevel);
    }
    public void InitializeRound(UpgradeToGeneralData data)
    {
        roundData = data;
        int index = SelectedIndex >= 0 ? SelectedIndex : startingSeriesIndex;
        StartRound(index, ResolveLevel(index));
    }
    private int ResolveLevel(int index)
    {
        int level = Mathf.Max(1, startingLevel);
        if (!useSessionLevels || roundData?.boxLevels == null || boxes == null || index < 0 || index >= boxes.boxes.Length) return level;
        var board = boxes.boxes[index]?.board;
        if (board == null) return level;
        level = 1;
        foreach (var snapshot in roundData.boxLevels)
            if (snapshot != null && snapshot.series == board.itemSeries) level = Mathf.Max(level, snapshot.level);
        return level;
    }
    public bool StartRound(int seriesIndex, int level)
    {
        if (boxes == null || seriesIndex < 0 || seriesIndex >= boxes.boxes.Length) return false;
        var entry = boxes.boxes[seriesIndex];
        if (entry == null || entry.board == null || entry.boxRoot == null || entry.levelManager == null) return false;
        if (!entry.levelManager.TrySetLevel(level)) return false;
        currentBox?.GimicStop(); ClearSkills();
        SelectedIndex = seriesIndex;
        foreach (var other in boxes.boxes)
        {
            if (other == null) continue;
            bool selected = other == entry;
            if (other.board != null) other.board.enabled = selected;
            if (other.boxRoot != null) other.boxRoot.gameObject.SetActive(selected);
        }
        boxes.ShowBox(seriesIndex);
        currentBox = entry.levelManager.CurrentBox.GetComponent<MergeBoxGimic>();
        if (currentBox != null) currentBox.Initialize(entry.board);
        SetSkills();
        return true;
    }
    public void SetBox(int index) => StartRound(index, ResolveLevel(index));
    public void SetCurrentBoxLevel(int level)
    {
        if (SelectedIndex >= 0) StartRound(SelectedIndex, level);
    }
    public bool SetBoxLevel(ItemSeries series, int level)
    {
        if (boxes == null) return false;
        for (int i = 0; i < boxes.boxes.Length; i++)
            if (boxes.boxes[i]?.board != null && boxes.boxes[i].board.itemSeries == series) return StartRound(i, level);
        return false;
    }
    public bool UpgradeCurrentBox()
    {
        var manager = boxes != null ? boxes.SelectedBoxManager : null;
        if (manager == null) return false;
        int next = int.MaxValue;
        foreach (var entry in manager.levels)
            if (entry?.box != null && entry.minimumLevel > manager.CurrentLevel) next = Mathf.Min(next, entry.minimumLevel);
        return next != int.MaxValue && StartRound(SelectedIndex, next);
    }
    public void SetSkills()
    {
        ClearSkills();
        handlers = new UnityAction[skillButtons.Length];
        int count = currentBox != null ? currentBox.SkillCount : 0;
        readyAt = new float[count];
        for (int i = 0; i < skillButtons.Length; i++)
        {
            var slot = skillButtons[i];
            if (slot == null) continue;
            bool visible = i < count;
            slot.gameObject.SetActive(visible);
            if (!visible || slot.button == null) continue;
            if (slot.sprite != null) slot.sprite.sprite = currentBox.GetSkillSprite(i);
            int skillIndex = i; // 람다에서 반복문의 최종 인덱스를 참조하지 않도록 복사합니다.
            handlers[i] = () => UseSkill(skillIndex);
            slot.button.onClick.AddListener(handlers[i]);
        }
        RefreshSkills();
    }
    private void ClearSkills()
    {
        for (int i = 0; i < skillButtons.Length; i++)
        {
            var slot = skillButtons[i];
            if (slot == null) continue;
            if (slot.button != null && handlers != null && i < handlers.Length && handlers[i] != null)
                slot.button.onClick.RemoveListener(handlers[i]);
            if (slot.Cool != null) slot.Cool.fillAmount = 0f;
            slot.gameObject.SetActive(false);
        }
        handlers = null;
    }
    public void UseSkill(int index)
    {
        var board = boxes != null ? boxes.SelectedBoard : null;
        if (currentBox == null || !currentBox.Running || board == null || !board.CanOperate || index < 0 || index >= readyAt.Length || readyAt[index] > 0f) return;
        readyAt[index] = currentBox.GetSkillStat(index).cooldown > 0f ? 1f : 0f;
        currentBox.UseBoxSkill(index);
        RefreshSkills();
    }
    private void Update()
    {
        if (currentBox != null && currentBox.Running)
            for (int i = 0; i < readyAt.Length; i++)
            {
                var stat = currentBox.GetSkillStat(i);
                readyAt[i] = stat.cooldown <= 0f ? 0f : Mathf.Max(0f, readyAt[i] - Time.deltaTime * stat.cooldownRecoveryRate / stat.cooldown);
            }
        RefreshSkills();
    }
    private void RefreshSkills()
    {
        for (int i = 0; i < skillButtons.Length && i < readyAt.Length; i++)
        {
            var slot = skillButtons[i];
            if (slot == null) continue;
            float remaining = Mathf.Max(0f, readyAt[i]);
            if (slot.button != null) slot.button.interactable = remaining <= 0f && currentBox != null && currentBox.Running;
            if (slot.Cool != null) slot.Cool.fillAmount = remaining;
        }
    }
}
