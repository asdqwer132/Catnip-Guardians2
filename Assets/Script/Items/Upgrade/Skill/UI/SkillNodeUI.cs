using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SkillNodeUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private SkillNodeData skillNodeData;

    [Header("UI")]
    [SerializeField] private GameObject[] lines;
    [SerializeField] private GameObject nodeRootObject;
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image lockImage;

    [Header("State Sprites")]
    [SerializeField] private Image stateImage;
    [SerializeField] private Sprite unlockedSprite;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private bool hideUnavailableNodes;

    private SkillTreeUI ownerUI;
    private Sprite authoredStateSprite;
    private bool stateSpriteCaptured;

    public SkillNodeData SkillNodeData => skillNodeData;
    public IReadOnlyList<GameObject> Lines => lines;

    public void SetData(SkillNodeData skillNodeData, GameObject[] lines)
    {
        this.skillNodeData = skillNodeData;
        this.lines = lines;
    }

    public void Init(SkillTreeUI owner)
    {
        ownerUI = owner;

        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);
            button.onClick.AddListener(OnClick);
        }

        if (lockImage != null)
            lockImage.raycastTarget = false;

        Refresh();
    }

    public void SetData(SkillNodeData data)
    {
        skillNodeData = data;
        Refresh();
    }

    private void OnClick()
    {
        if (skillNodeData == null)
            return;

        if (SkillTreeManager.Instance == null)
            return;

        bool success = SkillTreeManager.Instance.UnlockSkill(skillNodeData);

        if (success && ownerUI != null)
            ownerUI.RefreshAllNodes();
        else
            Refresh();
    }

    public void Refresh()
    {
        if (skillNodeData == null)
        {
            SetNodeVisible(false);
            return;
        }

        bool isUnlocked = false;
        bool canUnlock = false;

        if (SkillTreeManager.Instance != null)
        {
            isUnlocked = SkillTreeManager.Instance.IsUnlocked(skillNodeData.dataId);
            canUnlock = CurrencyManager.instance != null && SkillTreeManager.Instance.CanUnlock(skillNodeData);
        }

        bool isCannotClick = !isUnlocked && !canUnlock;
        bool isCanClick = !isUnlocked && canUnlock;
        SetLinesUnlocked(isUnlocked);
        if (hideUnavailableNodes && isCannotClick)
        {
            SetNodeVisible(false);
            return;
        }

        SetNodeVisible(true);

        if (iconImage != null)
        {
            Sprite icon = isUnlocked && skillNodeData.unlockedIcon != null
                ? skillNodeData.unlockedIcon : skillNodeData.icon;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }
        ApplyStateSprite(isUnlocked);

        if (button != null)
            button.interactable = isCanClick;

        if (lockImage != null)
            lockImage.gameObject.SetActive(isCanClick);

        if (isUnlocked)
        {
            if (button != null)
                button.interactable = false;

            if (lockImage != null)
                lockImage.gameObject.SetActive(false);

        }
    }

    private void ApplyStateSprite(bool unlocked)
    {
        if (stateImage == null) return;
        if (!stateSpriteCaptured) { authoredStateSprite = stateImage.sprite; stateSpriteCaptured = true; }
        Sprite sprite = unlocked ? unlockedSprite : lockedSprite;
        stateImage.sprite = sprite != null ? sprite : authoredStateSprite;
    }

    private void SetLinesUnlocked(bool unlocked)
    {
        if (lines == null) return;
        foreach (var line in lines)
        {
            if (line == null) continue;
            if (line.TryGetComponent<SkillTreeConnectionUI>(out var connection)) connection.SetUnlocked(unlocked);
            else if (line.TryGetComponent<SkillTreeLineUI>(out var segment)) segment.SetUnlocked(unlocked);
            else line.SetActive(unlocked); // 이전 씬/프리팹의 선 참조도 유지합니다.
        }
    }

    private void SetNodeVisible(bool visible)
    {
        if (nodeRootObject != null)
            nodeRootObject.SetActive(visible);

        if (button != null)
            button.interactable = false;

        if (lockImage != null)
            lockImage.gameObject.SetActive(false);
    }
}
