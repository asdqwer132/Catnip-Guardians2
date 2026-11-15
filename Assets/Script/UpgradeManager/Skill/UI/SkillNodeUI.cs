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
    [SerializeField] private SkillLevelSelecter levelSelector;

    [Header("Visibility")]
    [SerializeField] private bool hideUnavailableNodes;

    private SkillTreeUI ownerUI;
    private CanvasGroup visibilityGroup;
    private float visibleAlpha;
    private bool visibleInteractable, visibleRaycasts;
    public event System.Action VisibilityChanged;

    public SkillNodeData SkillNodeData => skillNodeData;
    public IReadOnlyList<GameObject> Lines => lines;
    public bool IsVisible { get; private set; } = true;

    public void SetData(SkillNodeData skillNodeData, GameObject[] lines)
    {
        this.skillNodeData = skillNodeData;
        this.lines = lines;
        ApplyLevel();
        ApplyIcon();
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
        Refresh();
    }

    public void ApplyIcon()
    {
        if (iconImage == null || skillNodeData == null) return;
        bool unlocked = Application.isPlaying && SkillTreeManager.Instance != null && SkillTreeManager.Instance.IsUnlocked(skillNodeData);
        var sprite = unlocked && skillNodeData.unlockedIcon != null ? skillNodeData.unlockedIcon : skillNodeData.icon;

        iconImage.sprite = sprite; 
        iconImage.enabled = sprite != null;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(iconImage);
            if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(iconImage)) UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(iconImage);
        }
#endif
    }

    public void ApplyLevel()
    {
        if (levelSelector == null) levelSelector = GetComponentInChildren<SkillLevelSelecter>(true);
        if (levelSelector != null && skillNodeData != null) {

            Debug.Log(name + "." + skillNodeData.level);
            levelSelector.SetNodeLevel(Mathf.Max(1, skillNodeData.level));
        }
    }

    public void Refresh()
    {
        ApplyLevel();
        if (skillNodeData == null)
        {
            SetNodeVisible(false);
            SetLinesUnlocked(false);
            return;
        }

        bool isUnlocked = false;
        bool canUnlock = false;
        bool hasRequiredSkills = false;

        if (SkillTreeManager.Instance != null)
        {
            isUnlocked = SkillTreeManager.Instance.IsUnlocked(skillNodeData);
            hasRequiredSkills = SkillTreeManager.Instance.HasRequiredSkills(skillNodeData);
            canUnlock = CurrencyManager.instance != null && SkillTreeManager.Instance.CanUnlock(skillNodeData);
        }

        bool isCanClick = !isUnlocked && canUnlock;
        SetNodeVisible(!hideUnavailableNodes || isUnlocked || hasRequiredSkills);
        SetLinesUnlocked(isUnlocked);
        if (!IsVisible) return;

        if (iconImage != null)
        {
            Sprite icon = isUnlocked && skillNodeData.unlockedIcon != null
                ? skillNodeData.unlockedIcon : skillNodeData.icon;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

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

    private void SetLinesUnlocked(bool unlocked)
    {
        if (lines == null) return;
        foreach (var line in lines)
        {
            if (line == null) continue;
            if (line.TryGetComponent<SkillTreeConnectionUI>(out var connection))
            {
                connection.SetUnlocked(unlocked);
                connection.RefreshVisibility();
            }
            else if (line.TryGetComponent<SkillTreeLineUI>(out var segment))
            {
                line.SetActive(IsVisible && unlocked);
                segment.SetUnlocked(false);
            }
            else line.SetActive(IsVisible && unlocked);
        }
    }

    private void SetNodeVisible(bool visible)
    {
        bool changed = IsVisible != visible;
        IsVisible = visible;
        // Keep the controller alive, but hide every visual child (including hover/checkmark siblings).
        if (Application.isPlaying)
        {
            if (visibilityGroup == null)
            {
                visibilityGroup = GetComponent<CanvasGroup>();
                if (visibilityGroup == null) visibilityGroup = gameObject.AddComponent<CanvasGroup>();
                visibleAlpha = visibilityGroup.alpha;
                visibleInteractable = visibilityGroup.interactable;
                visibleRaycasts = visibilityGroup.blocksRaycasts;
            }
            visibilityGroup.alpha = visible ? visibleAlpha : 0;
            visibilityGroup.interactable = visible && visibleInteractable;
            visibilityGroup.blocksRaycasts = visible && visibleRaycasts;
        }
        if (nodeRootObject != null && nodeRootObject != gameObject)
            nodeRootObject.SetActive(visible);

        if (button != null)
            button.interactable = false;

        if (lockImage != null)
            lockImage.gameObject.SetActive(false);
        if (changed) VisibilityChanged?.Invoke();
    }
}
