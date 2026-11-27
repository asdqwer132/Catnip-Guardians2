using UnityEngine;
using UnityEngine.EventSystems;

public sealed class BoardSkillTooltipProvider : TooltipProvider, IPointerEnterHandler, IPointerExitHandler
{
    private TooltipUI tooltip;
    private BoardGimmickSkillData data;

    public void Bind(BoardGimmickSkillData value, TooltipUI window)
    {
        if ((data != value || tooltip != window) && tooltip != null) tooltip.Hide(this);
        data = value;
        tooltip = window;
    }

    public override bool TryGetTooltipData(out TooltipData result)
    {
        result = data != null ? new TooltipData { sourceData = data, icon = data.icon, dataName = data.name } : null;
        return result != null;
    }

    public void OnPointerEnter(PointerEventData eventData) => tooltip?.Show(this);
    public void OnPointerExit(PointerEventData eventData) => tooltip?.Hide(this);
    private void OnDisable() => tooltip?.Hide(this);
}
