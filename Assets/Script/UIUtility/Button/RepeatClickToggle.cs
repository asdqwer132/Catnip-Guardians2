using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Keeps the selection indicator on and repeats the selected action on each click.
// Programmatic changes and ToggleGroup deselection still behave like a normal Toggle.
[AddComponentMenu("UI/Repeat Click Toggle")]
public class RepeatClickToggle : Toggle
{
    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (!RepeatSelection()) base.OnPointerClick(eventData);
    }

    public override void OnSubmit(BaseEventData eventData)
    {
        if (!RepeatSelection()) base.OnSubmit(eventData);
    }

    private bool RepeatSelection()
    {
        if (!IsActive() || !IsInteractable() || !isOn) return false;
        onValueChanged.Invoke(true);
        return true;
    }
}
