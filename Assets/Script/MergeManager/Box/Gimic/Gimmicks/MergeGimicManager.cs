using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;

[DefaultExecutionOrder(-90)]
public class MergeGimicManager : MonoBehaviour
{
    public const int MaxActiveModules = 3;
    [SerializeField] private MergeBoxGimic[] availableGimmicks = Array.Empty<MergeBoxGimic>();
    [SerializeField] private CoolButton[] skillButtons = Array.Empty<CoolButton>();
    [ReadOnly, SerializeField] private MergeBoxGimic currentBox;
    private readonly List<MergeBoxGimic> modules = new List<MergeBoxGimic>(MaxActiveModules);
    private readonly List<(MergeBoxGimic module, int index)> skills = new List<(MergeBoxGimic, int)>();
    private UnityAction[] handlers;
    private Color[] authoredSkillColors;
    public MergeBoxGimic CurrentGimic => currentBox;
    public IReadOnlyList<MergeBoxGimic> ActiveModules => modules;

    public void ApplyLoadout(MergegameManager board, UpgradeToGeneralData data)
    {
        var selected = new List<MergeBoxGimic>(MaxActiveModules);
        var equipment = data?.equipment;
        var indices = equipment?.board?.IsConfigured == true ? equipment.board.skillIndices : equipment?.equippedChests;
        if (indices != null)
            foreach (int number in indices)
                if (number >= 1 && number <= availableGimmicks.Length &&
                    UpgradeSystemOpener.IsBoxOpen(data.equipmentUnlockKeys, number))
                    selected.Add(availableGimmicks[number - 1]);

        InitializeModules(board, selected, (gimmick, index) =>
            UpgradeSystemOpener.IsSkillOpen(data.equipmentUnlockKeys, Array.IndexOf(availableGimmicks, gimmick) + 1, index + 1));
    }

    public void InitializeModules(MergegameManager board, IEnumerable<MergeBoxGimic> gimmicks, Func<MergeBoxGimic, int, bool> isSkillUnlocked = null)
    {
        StopGimic();
        if (board != null)
            foreach (var gimmick in gimmicks)
            {
                if (gimmick == null || modules.Contains(gimmick)) continue;
                gimmick.gameObject.SetActive(true);
                gimmick.Initialize(board, isSkillUnlocked == null ? null : index => isSkillUnlocked(gimmick, index));
                if (!gimmick.Running)
                {
                    gimmick.gameObject.SetActive(false);
                    continue;
                }
                modules.Add(gimmick);
                if (modules.Count == MaxActiveModules) break;
            }
        currentBox = modules.Count > 0 ? modules[0] : null;
        SetSkills();
    }

    public void StopGimic()
    {
        foreach (var module in modules)
            if (module != null)
            {
                module.GimicStop();
                module.gameObject.SetActive(false);
            }
        modules.Clear();
        currentBox = null;
        ClearSkills();
    }

    private void OnDisable() => StopGimic();

    public void SetSkills()
    {
        ClearSkills();
        foreach (var module in modules)
            for (int index = 0; index < module.SkillCount; index++)
                if (module.IsSkillEnabled(index)) skills.Add((module, index));
        handlers = new UnityAction[skillButtons.Length];
        authoredSkillColors = new Color[skillButtons.Length];
        for (int i = 0; i < skillButtons.Length; i++)
        {
            var slot = skillButtons[i];
            if (slot == null) continue;
            authoredSkillColors[i] = slot.sprite != null ? slot.sprite.color : Color.white;
            bool visible = i < skills.Count;
            slot.gameObject.SetActive(visible);
            if (!visible) continue;
            var skill = skills[i];
            if (slot.sprite != null) slot.sprite.sprite = skill.module.GetSkillSprite(skill.index);
            bool active = skill.module.IsActiveSkill(skill.index);
            if (slot.shortcutText != null) slot.shortcutText.text = active && slot.shortcutKey != Key.None ? slot.shortcutKey.ToString() : "";
            if (!active || slot.button == null) continue;
            int slotIndex = i;
            handlers[i] = () => UseSkill(slotIndex);
            slot.button.onClick.AddListener(handlers[i]);
        }
        RefreshSkills();
    }

    private void ClearSkills()
    {
        skills.Clear();
        for (int i = 0; i < skillButtons.Length; i++)
        {
            var slot = skillButtons[i];
            if (slot == null) continue;
            if (slot.button != null && handlers != null && handlers[i] != null)
                slot.button.onClick.RemoveListener(handlers[i]);
            if (slot.Cool != null) slot.Cool.fillAmount = 0f;
            if (slot.Duration != null) slot.Duration.fillAmount = 0f;
            if (slot.sprite != null && authoredSkillColors != null) slot.sprite.color = authoredSkillColors[i];
            slot.gameObject.SetActive(false);
        }
        handlers = null;
    }

    public void UseSkill(int index)
    {
        if (index < 0 || index >= skills.Count) return;
        var skill = skills[index];
        skill.module.TryUseActiveSkill(skill.index);
        RefreshSkills();
    }

    private void Update()
    {
        foreach (var module in modules) module.TickSkills(Time.deltaTime);
        RefreshSkills();
        if (Keyboard.current == null || IsTyping()) return;
        foreach (var slot in skillButtons)
            if (slot != null && slot.isActiveAndEnabled && slot.shortcutKey != Key.None &&
                Keyboard.current[slot.shortcutKey].wasPressedThisFrame && TryUseShortcut(slot.shortcutKey))
                break;
    }

    public bool TryUseShortcut(Key key)
    {
        if (key == Key.None || IsTyping()) return false;
        for (int i = 0; i < skillButtons.Length && i < skills.Count; i++)
        {
            var slot = skillButtons[i];
            if (slot == null || !slot.isActiveAndEnabled || slot.shortcutKey != key || slot.button == null || !slot.button.IsInteractable()) continue;
            var skill = skills[i];
            if (!skill.module.TryUseActiveSkill(skill.index)) continue;
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
        for (int i = 0; i < skillButtons.Length && i < skills.Count; i++)
        {
            var slot = skillButtons[i];
            if (slot == null) continue;
            var (module, index) = skills[i];
            if (slot.button != null) slot.button.interactable = module.CanActivateSkill(index);
            if (slot.Cool != null) slot.Cool.fillAmount = slot.Duration != null ? module.GetSkillProgress(index) : module.GetSkillDisplayProgress(index);
            if (slot.Duration != null) slot.Duration.fillAmount = module.GetSkillDurationProgress(index);
            if (slot.sprite != null && authoredSkillColors != null) slot.sprite.color = module.GetSkillDisplayColor(index) ?? authoredSkillColors[i];
        }
    }
}
