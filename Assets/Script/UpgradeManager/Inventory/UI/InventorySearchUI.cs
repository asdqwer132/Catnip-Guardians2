using System;
using TMPro;
using UnityEngine;

// Reuses the authored search controls and filters the inventory as the text changes.
// On...Changed takes enum values; OnGradeDropdownChanged maps authored row indices.
public class InventorySearchUI : ItemSearchUIBase
{
    [Header("Authored Grade Dropdown")]
    [Tooltip("Grade for each existing dropdown row. None means All. This maps values only; it does not create or change UI options.")]
    public Grade[] gradeDropdownValues = { Grade.None, Grade.Common, Grade.Rare, Grade.Epic, Grade.Legendary };

    [SerializeField] private TMP_InputField directSearchInput;

    public void OnGradeDropdownChanged(int index)
    {
        if (gradeDropdownValues != null && index >= 0 && index < gradeDropdownValues.Length)
            OnGradeChanged((int)gradeDropdownValues[index]);
    }

    public void OnCategoryChanged(int value)
    {
        if (value == (int)DataType.None)
        {
            ResetCategoryFilter();
            return;
        }
        if (Enum.IsDefined(typeof(DataType), value))
            ChangeCategoryFilter(true, (DataType)value);
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

    protected override void RebuildControls()
    {
        if (directSearchInput != null)
            return;

        var directSearch = transform.Find("DirectSearch");
        if (directSearch != null)
            directSearchInput = directSearch.GetComponentInChildren<TMP_InputField>(true);
    }

    protected override void BindControlEvents()
    {
        if (directSearchInput != null)
            directSearchInput.onValueChanged.AddListener(OnSearchTextChanged);
    }

    protected override void UnbindControlEvents()
    {
        if (directSearchInput != null)
            directSearchInput.onValueChanged.RemoveListener(OnSearchTextChanged);
    }

    protected override void ApplyFilterToControlsWithoutNotify()
    {
        if (directSearchInput != null && directSearchInput.text.Trim() != filter.query)
            directSearchInput.SetTextWithoutNotify(filter.query);
    }
}
