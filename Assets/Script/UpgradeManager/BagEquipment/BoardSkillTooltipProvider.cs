using UnityEngine;
using UnityEngine.EventSystems;

public sealed class BoardSkillTooltipProvider : TooltipProvider, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TooltipUI tooltip;
    private BoardGimmickSkillData data;

    public void SetSource(BoardGimmickSkillData value) => data = value;

    public override bool TryGetTooltipData(out TooltipData result)
    {
        result = data != null ? new TooltipData { sourceData = data, icon = data.icon, dataName = data.name } : null;
        return result != null;
    }

    public void OnPointerEnter(PointerEventData eventData) => tooltip?.Show(this);
    public void OnPointerExit(PointerEventData eventData) => tooltip?.Hide(this);
    private void OnDisable() => tooltip?.Hide(this);
}
