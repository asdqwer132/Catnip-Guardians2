using UnityEngine;
using UnityEngine.UI;

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

    private SkillTreeUI ownerUI;

    public SkillNodeData SkillNodeData => skillNodeData;

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

        foreach (var line in lines)
        {
            line.gameObject.SetActive(false);
        }
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
            canUnlock = SkillTreeManager.Instance.CanUnlock(skillNodeData);
        }

        bool isCannotClick = !isUnlocked && !canUnlock;
        bool isCanClick = !isUnlocked && canUnlock;
        bool isUnlockedState = isUnlocked;

        if (isCannotClick)
        {
            SetNodeVisible(false);
            return;
        }

        SetNodeVisible(true);

        if (iconImage != null)
        {
            iconImage.sprite = skillNodeData.icon;
            iconImage.enabled = skillNodeData.icon != null;
        }

        if (button != null)
            button.interactable = isCanClick;

        if (lockImage != null)
            lockImage.gameObject.SetActive(isCanClick);

        foreach (var line in lines)
        {
            line.gameObject.SetActive(false);
        }
        if (isUnlockedState)
        {
            if (button != null)
                button.interactable = false;

            if (lockImage != null)
                lockImage.gameObject.SetActive(false);

            foreach(var line in lines)
            {
                line.gameObject.SetActive(true);
            }
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