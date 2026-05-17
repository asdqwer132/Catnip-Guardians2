using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillNodeUI : MonoBehaviour
{
    [Header("InteractUI")]
    public Button button;

    [Header("InfoUI")]
    public Image background;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI nameText;

    [Header("PreviousUI")]
    public GameObject line;

    private SkillNode node;

    public void Init(SkillNode skillNode)
    {
        node = skillNode;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(node.TryUnlock);
        }

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (node == null)
            return;

        gameObject.SetActive(true);

        if (node.IsUnlocked())
        {
            SetUnlockedUI();
        }
        else if (node.IsLockedByRequiredNode())
        {
            SetLockedUI();
        }
        else
        {
            SetOpenUI();
        }

        if (costText != null)
            costText.text = node.IsUnlocked() ? "Unlocked" : node.GetCostText();

        if (nameText != null)
            nameText.text = node.skillName;
    }

    void SetUnlockedUI()
    {
        if (line != null)
            line.SetActive(true);

        if (background != null)
            background.color = Color.green;

        if (button != null)
            button.interactable = false;
    }

    void SetLockedUI()
    {
        if (line != null)
            line.SetActive(false);

        if (background != null)
            background.color = Color.gray;

        if (button != null)
            button.interactable = false;
    }

    void SetOpenUI()
    {
        if (line != null)
            line.SetActive(true);

        if (background != null)
            background.color = Color.white;

        if (button != null)
            button.interactable = true;
    }
}