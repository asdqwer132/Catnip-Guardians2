using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class InventorySearchUI : MonoBehaviour
{
    [Header("Target")]
    [FormerlySerializedAs("inventoryUI")]
    public ItemSearchFilterTargetUI targetUI;

    [Header("Dropdown")]
    public TMP_Dropdown categoryDropdown;
    public TMP_Dropdown seriesDropdown;
    public TMP_Dropdown gradeDropdown;

    [Header("Button")]
    public Button resetButton;

    [Header("Reset Option")]
    public bool resetCategory = true;
    public bool resetSeries = true;
    public bool resetGrade = true;

    public event Action<InventorySearchUI, InventorySearchFilter> OnFilterChanged;

    private bool isInitialized;
    private bool isEventBound;
    private bool isApplyingExternalFilter;

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

        RebuildDropdowns();
        InitFilterFromTarget();
        RemoveMaskedFilterValue();
        ApplyDropdownValueFromFilter();

        ApplyFilter(false);
    }

    private void OnDestroy()
    {
        UnbindEvents();
    }

    public void Init()
    {
        if (isInitialized)
            return;

        ResolveTarget();

        RebuildDropdowns();

        InitFilterFromTarget();
        RemoveMaskedFilterValue();
        ApplyDropdownValueFromFilter();

        BindEvents();

        isInitialized = true;

        ApplyFilter(false);
    }

    public InventorySearchFilter GetCurrentFilterCopy()
    {
        InventorySearchFilter copy = new InventorySearchFilter();
        CopyFilter(filter, copy);
        return copy;
    }

    public void SetFilterFromExternal(InventorySearchFilter sourceFilter)
    {
        if (sourceFilter == null)
            return;

        Init();

        isApplyingExternalFilter = true;

        CopyFilter(sourceFilter, filter);

        ApplyFilter(false);

        isApplyingExternalFilter = false;
    }

    private void ResolveTarget()
    {
        if (targetUI != null)
            return;

        targetUI = GetComponentInParent<ItemSearchFilterTargetUI>(true);

        if (targetUI == null)
            Debug.LogWarning("[InventorySearchUI] ItemSearchFilterTargetUI를 찾지 못했습니다.", this);
    }

    private void RebuildDropdowns()
    {
        ResolveTarget();

        SetupCategoryDropdown();
        SetupSeriesDropdown();
        SetupGradeDropdown();
    }

    private void InitFilterFromTarget()
    {
        if (targetUI == null)
        {
            filter.Clear();
            return;
        }

        InventorySearchFilter defaultFilter = targetUI.GetSearchFilter();

        if (defaultFilter == null)
        {
            filter.Clear();
            return;
        }

        CopyFilter(defaultFilter, filter);
    }

    private void RemoveMaskedFilterValue()
    {
        if (targetUI == null)
            return;

        if (filter.useCategory && targetUI.IsCategoryMasked(filter.category))
            filter.useCategory = false;

        if (filter.useSeries && targetUI.IsSeriesMasked(filter.series))
            filter.useSeries = false;

        if (filter.useGrade && targetUI.IsGradeMasked(filter.grade))
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

            if (targetUI != null && targetUI.IsCategoryMasked(value))
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

            if (targetUI != null && targetUI.IsSeriesMasked(value))
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

            if (targetUI != null && targetUI.IsGradeMasked(value))
                continue;

            gradeDropdownValues.Add(value);
            gradeDropdown.options.Add(new TMP_Dropdown.OptionData(value.ToString()));
        }

        gradeDropdown.RefreshShownValue();
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
        if (isEventBound)
            return;

        if (categoryDropdown != null)
            categoryDropdown.onValueChanged.AddListener(OnCategoryChanged);

        if (seriesDropdown != null)
            seriesDropdown.onValueChanged.AddListener(OnSeriesChanged);

        if (gradeDropdown != null)
            gradeDropdown.onValueChanged.AddListener(OnGradeChanged);

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetFilter);

        isEventBound = true;
    }

    private void UnbindEvents()
    {
        if (!isEventBound)
            return;

        if (categoryDropdown != null)
            categoryDropdown.onValueChanged.RemoveListener(OnCategoryChanged);

        if (seriesDropdown != null)
            seriesDropdown.onValueChanged.RemoveListener(OnSeriesChanged);

        if (gradeDropdown != null)
            gradeDropdown.onValueChanged.RemoveListener(OnGradeChanged);

        if (resetButton != null)
            resetButton.onClick.RemoveListener(ResetFilter);

        isEventBound = false;
    }

    private void OnCategoryChanged(int index)
    {
        filter.useCategory = index != AllIndex;

        if (filter.useCategory)
            filter.category = GetCategoryByDropdownIndex(index);

        ApplyFilter(true);
    }

    private void OnSeriesChanged(int index)
    {
        filter.useSeries = index != AllIndex;

        if (filter.useSeries)
            filter.series = GetSeriesByDropdownIndex(index);

        ApplyFilter(true);
    }

    private void OnGradeChanged(int index)
    {
        filter.useGrade = index != AllIndex;

        if (filter.useGrade)
            filter.grade = GetGradeByDropdownIndex(index);

        ApplyFilter(true);
    }

    private ItemCategory GetCategoryByDropdownIndex(int dropdownIndex)
    {
        if (categoryDropdownValues.Count == 0)
            return default;

        int valueIndex = Mathf.Clamp(dropdownIndex - 1, 0, categoryDropdownValues.Count - 1);
        return categoryDropdownValues[valueIndex];
    }

    private ItemSeries GetSeriesByDropdownIndex(int dropdownIndex)
    {
        if (seriesDropdownValues.Count == 0)
            return default;

        int valueIndex = Mathf.Clamp(dropdownIndex - 1, 0, seriesDropdownValues.Count - 1);
        return seriesDropdownValues[valueIndex];
    }

    private ItemGrade GetGradeByDropdownIndex(int dropdownIndex)
    {
        if (gradeDropdownValues.Count == 0)
            return default;

        int valueIndex = Mathf.Clamp(dropdownIndex - 1, 0, gradeDropdownValues.Count - 1);
        return gradeDropdownValues[valueIndex];
    }

    private void ApplyFilter(bool notify)
    {
        ResolveTarget();

        if (targetUI == null)
            return;

        RemoveMaskedFilterValue();
        ApplyDropdownValueFromFilter();

        targetUI.SetSearchFilter(filter);

        if (notify && !isApplyingExternalFilter)
            OnFilterChanged?.Invoke(this, GetCurrentFilterCopy());
    }

    public void ResetFilter()
    {
        ResetCategoryFilter();
        ResetSeriesFilter();
        ResetGradeFilter();

        ApplyFilter(true);
    }

    private void ResetCategoryFilter()
    {
        if (!resetCategory)
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
        if (!resetSeries)
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
        if (!resetGrade)
            return;

        filter.useGrade = false;

        if (gradeDropdown != null)
        {
            gradeDropdown.SetValueWithoutNotify(AllIndex);
            gradeDropdown.RefreshShownValue();
        }
    }

    private void CopyFilter(InventorySearchFilter source, InventorySearchFilter target)
    {
        if (source == null || target == null)
            return;

        target.useCategory = source.useCategory;
        target.category = source.category;

        target.useSeries = source.useSeries;
        target.series = source.series;

        target.useGrade = source.useGrade;
        target.grade = source.grade;
    }
}