using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlantTooltipTrigger : ItemTooltipTrigger
{
    public Toggle infoToggle;
    public Toggle invenToggle;
    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        infoToggle.isOn = true;
    }
    public override void OnPointerExit(PointerEventData eventData)
    {
        invenToggle.isOn = true;

    }
}
