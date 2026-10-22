using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ToggleTooltipTrigger : ItemTooltipTrigger
{
    public Toggle infoToggle;
    public Toggle invenToggle;
    public bool isExitUse = false;
    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        if (infoToggle != null)
            infoToggle.isOn = true;
    }
    public override void OnPointerExit(PointerEventData eventData)
    {
        if (!isExitUse && invenToggle != null)
            invenToggle.isOn = true;
        else if (infoToggle == null && invenToggle == null)
            base.OnPointerExit(eventData);

    }
}
