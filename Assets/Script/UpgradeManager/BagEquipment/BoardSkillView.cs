using UnityEngine;
using UnityEngine.UI;

public sealed class BoardSkillView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private BoardSkillTooltipProvider tooltipProvider;

    public void Show(BoardGimmickSkillData skill, TooltipUI tooltip)
    {
        icon.sprite = skill != null ? skill.icon : null;
        icon.enabled = icon.sprite != null;
        tooltipProvider.Bind(skill, tooltip);
        gameObject.SetActive(skill != null);
    }
}
