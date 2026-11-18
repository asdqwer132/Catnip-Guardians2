using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;

// 선택된 상자의 기믹 수명과 스킬 버튼만 관리합니다. 라운드/재고/상자 선택은 MergeGeneralManager가 담당합니다.
[DefaultExecutionOrder(-90)]
public class MergeGimicManager : MonoBehaviour
{
    [SerializeField] private CoolButton[] skillButtons = Array.Empty<CoolButton>();
    [ReadOnly, SerializeField] private MergeBoxGimic currentBox;
    private UnityAction[] handlers;
    private Color[] authoredSkillColors;
    public MergeBoxGimic CurrentGimic => currentBox;

    public void InitializeGimic(MergegameManager board, MergeBoxBindings box)
    {
        StopGimic();
        currentBox = box != null ? box.GetComponent<MergeBoxGimic>() : null;
        if (currentBox != null && board != null) currentBox.Initialize(board);
        SetSkills();
    }

    public void StopGimic()
    {
        currentBox?.GimicStop();
        ClearSkills();
    }
    private void OnDisable() => StopGimic();
    public void SetSkills()
    {
        ClearSkills();
        handlers = new UnityAction[skillButtons.Length];
        authoredSkillColors = new Color[skillButtons.Length];
        int count = currentBox != null ? currentBox.SkillCount : 0;
        for (int i = 0; i < skillButtons.Length; i++)
        {
            var slot = skillButtons[i];
            if (slot == null) continue;
            authoredSkillColors[i] = slot.sprite != null ? slot.sprite.color : Color.white;
            bool visible = i < count && currentBox.IsSkillEnabled(i);
            slot.gameObject.SetActive(visible);
            if (!visible || slot.button == null) continue;
            if (slot.sprite != null) { authoredSkillColors[i] = slot.sprite.color; slot.sprite.sprite = currentBox.GetSkillSprite(i); }
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
            if (slot.Duration != null) slot.Duration.fillAmount = 0f;
            if (slot.sprite != null && authoredSkillColors != null && i < authoredSkillColors.Length) slot.sprite.color = authoredSkillColors[i];
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
            if (slot.button != null) slot.button.interactable = currentBox.CanActivateSkill(i);
            if (slot.Cool != null) slot.Cool.fillAmount = slot.Duration!=null ? currentBox.GetSkillProgress(i) : currentBox.GetSkillDisplayProgress(i);
            if (slot.Duration != null) slot.Duration.fillAmount=currentBox.GetSkillDurationProgress(i);
            if (slot.sprite != null && authoredSkillColors != null) slot.sprite.color = currentBox.GetSkillDisplayColor(i) ?? authoredSkillColors[i];
        }
    }
}
