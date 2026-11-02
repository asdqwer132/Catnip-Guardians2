using UnityEngine;

public class SubPlantMaterialSlotUI : ClickableItemSlotUI
{
    private ItemRecipeManager manager;
    private int materialIndex = -1;
    protected override bool AllowEmptySelection => true;
    protected override bool KeepSelectionOnRefresh => true;

    public void Bind(ItemRecipeManager owner, int index)
    {
        if (manager != owner || materialIndex != index)
        {
            Unbind();
            manager = owner;
            materialIndex = index;
            if (manager != null && isActiveAndEnabled) manager.MaterialSlotSelected += ApplySelection;
        }
        if (manager == null) return;
        manager.SetMaterialSlotLocked(materialIndex, IsLocked);
        ApplySelection(manager.SelectedMaterialSlot);
    }

    private void OnEnable()
    {
        if (manager == null) return;
        manager.MaterialSlotSelected -= ApplySelection;
        manager.MaterialSlotSelected += ApplySelection;
        ApplySelection(manager.SelectedMaterialSlot);
    }

    protected override void OnDisable()
    {
        if (manager != null && manager.SelectedMaterialSlot == materialIndex) manager.SelectMaterialSlot(-1);
        if (manager != null) manager.MaterialSlotSelected -= ApplySelection;
        base.OnDisable();
    }

    protected override void OnSelectionChanged(bool selected)
    {
        if (manager == null) return;
        if (selected) manager.SelectMaterialSlot(materialIndex);
        else if (manager.SelectedMaterialSlot == materialIndex)
        {
            // ToggleGroup turns the old slot off before notifying the new slot.
            // Leave that in-flight selection intact until its true callback arrives.
            if (slotToggle != null && slotToggle.group != null && slotToggle.group.AnyTogglesOn()) return;
            manager.SelectMaterialSlot(-1);
        }
    }

    private void ApplySelection(int index)
    {
        if (slotToggle != null) slotToggle.SetIsOnWithoutNotify(index == materialIndex && !IsLocked);
    }

    public override void LockSlot()
    {
        base.LockSlot();
        if (manager != null) manager.SetMaterialSlotLocked(materialIndex, true);
    }

    public override void ClearSlot()
    {
        base.ClearSlot();
        if (manager != null) manager.SetMaterialSlotLocked(materialIndex, false);
    }

    // Kept for existing remove-material buttons and UnityEvents. Toggle selection
    // only changes the destination; it does not return an occupied slot's item.
    public override void OnClickSlot()
    {
        if (manager != null && materialIndex >= 0) { manager.ReturnMaterialAt(materialIndex); return; }
        if (currentItem == null || currentItem.itemData == null) return;
        var panel = GetComponentInParent<ItemCombinationPanelUI>(true);
        ItemRecipeManager owner = panel != null ? panel.Manager : SubPlantManager.instance;
        if (owner != null) owner.ReturnMaterial(currentItem.itemData, 1);
    }

    private void Unbind()
    {
        if (manager == null) return;
        manager.MaterialSlotSelected -= ApplySelection;
        if (manager.SelectedMaterialSlot == materialIndex) manager.SelectMaterialSlot(-1);
    }

    protected override void OnDestroy() { Unbind(); base.OnDestroy(); }
}
