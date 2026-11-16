using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;

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
        for (int i = 0; i < skillButtons.Length; i++)
        {
            var slot = skillButtons[i];
            if (slot == null) continue;
            bool visible = i < count;
            slot.gameObject.SetActive(visible);
            if (!visible || slot.button == null) continue;
            if (slot.sprite != null) slot.sprite.sprite = currentBox.GetSkillSprite(i);
            int skillIndex = i; // 람다에서 반복문의 최종 인덱스를 참조하지 않도록 복사합니다.
            if (slot.shortcutText != null) slot.shortcutText.text = currentBox.IsActiveSkill(i) && slot.shortcutKey != Key.None ? slot.shortcutKey.ToString() : "";
            if (currentBox.IsActiveSkill(i))
            {
                handlers[i] = () => UseSkill(skillIndex);
                slot.button.onClick.AddListener(handlers[i]);
            }
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
        if (currentBox != null) currentBox.TryUseActiveSkill(index);
        RefreshSkills();
    }
    private void Update()
    {
        currentBox?.TickSkills(Time.deltaTime);
        RefreshSkills();
        if (Keyboard.current == null || IsTyping()) return;
        for (int i = 0; i < skillButtons.Length; i++)
        {
            var slot = skillButtons[i];
            if (slot == null || !slot.isActiveAndEnabled || slot.button == null || !slot.button.IsInteractable() || slot.shortcutKey == Key.None) continue;
            if (Keyboard.current[slot.shortcutKey].wasPressedThisFrame)
            { if (TryUseShortcut(slot.shortcutKey)) break; }
        }
    }
    // 키보드와 추후 InputAction 입력이 같은 검증 경로를 사용합니다.
    public bool TryUseShortcut(Key key)
    {
        if (key == Key.None || currentBox == null || IsTyping()) return false;
        for (int i = 0; i < skillButtons.Length; i++)
        {
            var slot = skillButtons[i];
            if (slot == null || !slot.isActiveAndEnabled || slot.shortcutKey != key || slot.button == null || !slot.button.IsInteractable()) continue;
            if (!currentBox.TryUseActiveSkill(i)) continue;
            RefreshSkills();
            return true;
        }
        return false;
    }
    private static bool IsTyping()
    {
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return selected != null && (selected.GetComponent<TMP_InputField>() != null || selected.GetComponent<UnityEngine.UI.InputField>() != null);
    }
    private void RefreshSkills()
    {
        for (int i = 0; i < skillButtons.Length && currentBox != null && i < currentBox.SkillCount; i++)
        {
            var slot = skillButtons[i];
            if (slot == null) continue;
            float remaining = currentBox.GetSkillProgress(i);
            if (slot.button != null) slot.button.interactable = remaining <= 0f && currentBox.Running && currentBox.IsActiveSkill(i) && boxes != null && boxes.SelectedBoard != null && boxes.SelectedBoard.CanOperate;
            if (slot.Cool != null) slot.Cool.fillAmount = remaining;
        }
    }
}
