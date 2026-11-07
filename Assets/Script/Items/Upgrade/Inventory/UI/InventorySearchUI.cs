using System;
using UnityEngine.UI;

// UI creation, layout and event wiring are authored in the Inspector.
// Event arguments are actual enum values, not dropdown indices or an automatically built list.
public class InventorySearchUI : ItemSearchUIBase
{
    public void OnCategoryChanged(int value)
    {
        if (value == 0)
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
        if (value == 0)
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
