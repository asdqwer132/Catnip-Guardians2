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
    [SerializeField] private TMP_Dropdown seriesDropdown;
    [SerializeField] private TMP_Dropdown categoryDropdown;
    [SerializeField] private DataType[] categoryDropdownValues = { DataType.None, DataType.ItemMaterial, DataType.Pin };

    private void OnCategoryDropdownChanged(int index)
    {
        if (index < 0 || index >= categoryDropdownValues.Length) return;
        DataType category = categoryDropdownValues[index];
        if (category == DataType.Pin && filter.useSeries) ResetSeriesFilter();
        OnCategoryChanged((int)category);
    }

    public void SetSeriesControlAvailable(bool available)
    {
        if (seriesDropdown != null) seriesDropdown.interactable = available;
        if (!available) ResetSeriesFilter();
    }

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
        if (categoryDropdown != null) categoryDropdown.onValueChanged.AddListener(OnCategoryDropdownChanged);
        if (directSearchInput != null)
            directSearchInput.onValueChanged.AddListener(OnSearchTextChanged);
    }

    protected override void UnbindControlEvents()
    {
        if (categoryDropdown != null) categoryDropdown.onValueChanged.RemoveListener(OnCategoryDropdownChanged);
        if (directSearchInput != null)
            directSearchInput.onValueChanged.RemoveListener(OnSearchTextChanged);
    }

    protected override void ApplyFilterToControlsWithoutNotify()
    {
        if (directSearchInput != null && directSearchInput.text.Trim() != filter.query)
            directSearchInput.SetTextWithoutNotify(filter.query);
        if (categoryDropdown != null)
        {
            int option = filter.useCategory ? Array.IndexOf(categoryDropdownValues, filter.category) : 0;
            categoryDropdown.SetValueWithoutNotify(Mathf.Max(0, option));
        }
        if (seriesDropdown != null)
        {
            if (categoryDropdown != null)
                seriesDropdown.interactable = !filter.useCategory || filter.category != DataType.Pin;
            if (!filter.useSeries) seriesDropdown.SetValueWithoutNotify(0);
        }
    }
}
