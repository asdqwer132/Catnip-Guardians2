using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySearchUI : MonoBehaviour
{
    [Header("Target")]
    public InventoryUI inventoryUI;

    [Header("Dropdown")]
    public TMP_Dropdown categoryDropdown;
    public TMP_Dropdown seriesDropdown;
    public TMP_Dropdown gradeDropdown;

    [Header("Search Mask")]
    public ItemCategory[] categoryMask;
    public ItemSeries[] seriesMask;
    public ItemGrade[] gradeMask;

    [Header("Button")]
    public Button resetButton;

    [Header("Reset Option")]
    public bool resetCategory = true;
    public bool resetSeries = true;
    public bool resetGrade = true;

    private bool isInitialized;
    private readonly InventorySearchFilter filter = new InventorySearchFilter();

    private readonly List<ItemCategory> categoryDropdownValues = new List<ItemCategory>();
    private readonly List<ItemSeries> seriesDropdownValues = new List<ItemSeries>();
    private readonly List<ItemGrade> gradeDropdownValues = new List<ItemGrade>();

    private const int AllIndex = 0;

    private void Awake()
    {
        Init();
    }

    private void OnEnable()
    {
        Init();
        ApplyFilter();
    }

    private void OnDestroy()
    {
        UnbindEvents();
    }

    public void Init()
    {
        if (isInitialized)
            return;

        if (inventoryUI == null)
            inventoryUI = GetComponentInParent<InventoryUI>();

        SetupCategoryDropdown();
        SetupSeriesDropdown();
        SetupGradeDropdown();

        InitFilterFromInventoryUI();
        RemoveMaskedFilterValue();
        ApplyDropdownValueFromFilter();

        BindEvents();

        isInitialized = true;

        ApplyFilter();
    }

    private void InitFilterFromInventoryUI()
    {
        if (inventoryUI == null)
        {
            filter.Clear();
            return;
        }

        InventorySearchFilter defaultFilter = inventoryUI.GetSearchFilter();

        if (defaultFilter == null)
        {
            filter.Clear();
            return;
        }

        filter.useCategory = defaultFilter.useCategory;
        filter.category = defaultFilter.category;

        filter.useSeries = defaultFilter.useSeries;
        filter.series = defaultFilter.series;

        filter.useGrade = defaultFilter.useGrade;
        filter.grade = defaultFilter.grade;
    }

    private void RemoveMaskedFilterValue()
    {
        if (filter.useCategory && IsCategoryMasked(filter.category))
            filter.useCategory = false;

        if (filter.useSeries && IsSeriesMasked(filter.series))
            filter.useSeries = false;

        if (filter.useGrade && IsGradeMasked(filter.grade))
            filter.useGrade = false;
    }

    private void SetupCategoryDropdown()
    {
        categoryDropdownValues.Clear();

        if (categoryDropdown == null)
            return;

        categoryDropdown.ClearOptions();
        categoryDropdown.options.Add(new TMP_Dropdown.OptionData("ALL"));

        Array values = Enum.GetValues(typeof(ItemCategory));

        for (int i = 0; i < values.Length; i++)
        {
            ItemCategory value = (ItemCategory)values.GetValue(i);

            if (IsCategoryMasked(value))
                continue;

            categoryDropdownValues.Add(value);
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData(value.ToString()));
        }

        categoryDropdown.RefreshShownValue();
    }

    private void SetupSeriesDropdown()
    {
        seriesDropdownValues.Clear();

        if (seriesDropdown == null)
            return;

        seriesDropdown.ClearOptions();
        seriesDropdown.options.Add(new TMP_Dropdown.OptionData("ALL"));

        Array values = Enum.GetValues(typeof(ItemSeries));

        for (int i = 0; i < values.Length; i++)
        {
            ItemSeries value = (ItemSeries)values.GetValue(i);

            if (IsSeriesMasked(value))
                continue;

            seriesDropdownValues.Add(value);
            seriesDropdown.options.Add(new TMP_Dropdown.OptionData(value.ToString()));
        }

        seriesDropdown.RefreshShownValue();
    }

    private void SetupGradeDropdown()
    {
        gradeDropdownValues.Clear();

        if (gradeDropdown == null)
            return;

        gradeDropdown.ClearOptions();
        gradeDropdown.options.Add(new TMP_Dropdown.OptionData("ALL"));

        Array values = Enum.GetValues(typeof(ItemGrade));

        for (int i = 0; i < values.Length; i++)
        {
            ItemGrade value = (ItemGrade)values.GetValue(i);

            if (IsGradeMasked(value))
                continue;

            gradeDropdownValues.Add(value);
            gradeDropdown.options.Add(new TMP_Dropdown.OptionData(value.ToString()));
        }

        gradeDropdown.RefreshShownValue();
    }

    private bool IsCategoryMasked(ItemCategory value)
    {
        if (categoryMask == null)
            return false;

        for (int i = 0; i < categoryMask.Length; i++)
        {
            if (categoryMask[i].Equals(value))
                return true;
        }

        return false;
    }

    private bool IsSeriesMasked(ItemSeries value)
    {
        if (seriesMask == null)
            return false;

        for (int i = 0; i < seriesMask.Length; i++)
        {
            if (seriesMask[i].Equals(value))
                return true;
        }

        return false;
    }

    private bool IsGradeMasked(ItemGrade value)
    {
        if (gradeMask == null)
            return false;

        for (int i = 0; i < gradeMask.Length; i++)
        {
            if (gradeMask[i].Equals(value))
                return true;
        }

        return false;
    }

    private void ApplyDropdownValueFromFilter()
    {
        SetCategoryDropdownValue();
        SetSeriesDropdownValue();
        SetGradeDropdownValue();
    }

    private void SetCategoryDropdownValue()
    {
        if (categoryDropdown == null)
            return;

        int index = AllIndex;

        if (filter.useCategory)
            index = FindCategoryDropdownIndex(filter.category);

        categoryDropdown.SetValueWithoutNotify(index);
        categoryDropdown.RefreshShownValue();
    }

    private void SetSeriesDropdownValue()
    {
        if (seriesDropdown == null)
            return;

        int index = AllIndex;

        if (filter.useSeries)
            index = FindSeriesDropdownIndex(filter.series);

        seriesDropdown.SetValueWithoutNotify(index);
        seriesDropdown.RefreshShownValue();
    }

    private void SetGradeDropdownValue()
    {
        if (gradeDropdown == null)
            return;

        int index = AllIndex;

        if (filter.useGrade)
            index = FindGradeDropdownIndex(filter.grade);

        gradeDropdown.SetValueWithoutNotify(index);
        gradeDropdown.RefreshShownValue();
    }

    private int FindCategoryDropdownIndex(ItemCategory value)
    {
        for (int i = 0; i < categoryDropdownValues.Count; i++)
        {
            if (categoryDropdownValues[i].Equals(value))
                return i + 1;
        }

        return AllIndex;
    }

    private int FindSeriesDropdownIndex(ItemSeries value)
    {
        for (int i = 0; i < seriesDropdownValues.Count; i++)
        {
            if (seriesDropdownValues[i].Equals(value))
                return i + 1;
        }

        return AllIndex;
    }

    private int FindGradeDropdownIndex(ItemGrade value)
    {
        for (int i = 0; i < gradeDropdownValues.Count; i++)
        {
            if (gradeDropdownValues[i].Equals(value))
                return i + 1;
        }

        return AllIndex;
    }

    private void BindEvents()
    {
        if (categoryDropdown != null)
            categoryDropdown.onValueChanged.AddListener(OnCategoryChanged);

        if (seriesDropdown != null)
            seriesDropdown.onValueChanged.AddListener(OnSeriesChanged);

        if (gradeDropdown != null)
            gradeDropdown.onValueChanged.AddListener(OnGradeChanged);

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetFilter);
    }

    private void UnbindEvents()
    {
        if (categoryDropdown != null)
            categoryDropdown.onValueChanged.RemoveListener(OnCategoryChanged);

        if (seriesDropdown != null)
            seriesDropdown.onValueChanged.RemoveListener(OnSeriesChanged);

        if (gradeDropdown != null)
            gradeDropdown.onValueChanged.RemoveListener(OnGradeChanged);

        if (resetButton != null)
            resetButton.onClick.RemoveListener(ResetFilter);
    }

    private void OnCategoryChanged(int index)
    {
        filter.useCategory = index != AllIndex;

        if (filter.useCategory)
            filter.category = GetCategoryByDropdownIndex(index);

        ApplyFilter();
    }

    private void OnSeriesChanged(int index)
    {
        filter.useSeries = index != AllIndex;

        if (filter.useSeries)
            filter.series = GetSeriesByDropdownIndex(index);

        ApplyFilter();
    }

    private void OnGradeChanged(int index)
    {
        filter.useGrade = index != AllIndex;

        if (filter.useGrade)
            filter.grade = GetGradeByDropdownIndex(index);

        ApplyFilter();
    }

    private ItemCategory GetCategoryByDropdownIndex(int dropdownIndex)
    {
        int valueIndex = Mathf.Clamp(dropdownIndex - 1, 0, categoryDropdownValues.Count - 1);
        return categoryDropdownValues[valueIndex];
    }

    private ItemSeries GetSeriesByDropdownIndex(int dropdownIndex)
    {
        int valueIndex = Mathf.Clamp(dropdownIndex - 1, 0, seriesDropdownValues.Count - 1);
        return seriesDropdownValues[valueIndex];
    }

    private ItemGrade GetGradeByDropdownIndex(int dropdownIndex)
    {
        int valueIndex = Mathf.Clamp(dropdownIndex - 1, 0, gradeDropdownValues.Count - 1);
        return gradeDropdownValues[valueIndex];
    }

    private void ApplyFilter()
    {
        if (inventoryUI == null)
            return;

        RemoveMaskedFilterValue();
        inventoryUI.SetSearchFilter(filter);
    }

    public void ResetFilter()
    {
        ResetCategoryFilter();
        ResetSeriesFilter();
        ResetGradeFilter();

        ApplyFilter();
    }

    private void ResetCategoryFilter()
    {
        if (!CanResetCategory())
            return;

        filter.useCategory = false;

        if (categoryDropdown != null)
        {
            categoryDropdown.SetValueWithoutNotify(AllIndex);
            categoryDropdown.RefreshShownValue();
        }
    }

    private void ResetSeriesFilter()
    {
        if (!CanResetSeries())
            return;

        filter.useSeries = false;

        if (seriesDropdown != null)
        {
            seriesDropdown.SetValueWithoutNotify(AllIndex);
            seriesDropdown.RefreshShownValue();
        }
    }

    private void ResetGradeFilter()
    {
        if (!CanResetGrade())
            return;

        filter.useGrade = false;

        if (gradeDropdown != null)
        {
            gradeDropdown.SetValueWithoutNotify(AllIndex);
            gradeDropdown.RefreshShownValue();
        }
    }

    private bool CanResetCategory()
    {
        return resetCategory;
    }

    private bool CanResetSeries()
    {
        return resetSeries;
    }

    private bool CanResetGrade()
    {
        return resetGrade;
    }
}