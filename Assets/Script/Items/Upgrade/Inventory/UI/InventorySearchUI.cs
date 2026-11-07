using System;
using UnityEngine;

// UI creation, layout and event wiring are authored in the Inspector.
// On...Changed takes enum values; OnGradeDropdownChanged maps authored row indices.
public class InventorySearchUI : ItemSearchUIBase
{
    [Header("Authored Grade Dropdown")]
    [Tooltip("Grade for each existing dropdown row. None means All. This maps values only; it does not create or change UI options.")]
    public Grade[] gradeDropdownValues = { Grade.None, Grade.Common, Grade.Rare, Grade.Epic, Grade.Legendary };

    public void OnGradeDropdownChanged(int index)
    {
        if (gradeDropdownValues != null && index >= 0 && index < gradeDropdownValues.Length)
            OnGradeChanged((int)gradeDropdownValues[index]);
    }

    public void OnCategoryChanged(int value)
    {
        if (value == (int)Category.None)
        {
            ResetCategoryFilter();
            return;
        }
        if (Enum.IsDefined(typeof(Category), value))
            ChangeCategoryFilter(true, (Category)value);
    }

    public void OnSeriesChanged(int value)
    {
        if(value == 0)
        {
            ResetSeriesFilter();
            return;
        }
        if (Enum.IsDefined(typeof(ItemSeries), value))
            ChangeSeriesFilter(true, (ItemSeries)value);
    }

    public void OnGradeChanged(int value)
    {
        if (value == (int)Grade.None)
        {
            ResetGradeFilter();
            return;
        }
        if (Enum.IsDefined(typeof(Grade), value))
            ChangeGradeFilter(true, (Grade)value);
    }

    // Keep the shared filter/target/synchronization lifecycle without controlling any UI.
    protected override void RebuildControls() { }
    protected override void BindControlEvents() { }
    protected override void UnbindControlEvents() { }
    protected override void ApplyFilterToControlsWithoutNotify() { }
}
