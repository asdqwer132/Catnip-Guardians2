using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Inventory selection and equipping are separate events. Each click requests one
// equipment action without replaying the crafting toggle's value-change logic.
[AddComponentMenu("UI/Potion Equip Inventory Toggle")]
public sealed class PotionEquipInventoryToggle : Toggle
{
    public event Action Clicked;

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) Activate();
    }

    public override void OnSubmit(BaseEventData eventData) => Activate();

    private void Activate()
    {
        if (!IsActive() || !IsInteractable()) return;
        isOn = true;
        Clicked?.Invoke();
    }
}
