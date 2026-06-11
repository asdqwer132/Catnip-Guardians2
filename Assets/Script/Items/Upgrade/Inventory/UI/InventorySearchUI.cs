using System;
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

    [Header("Button")]
    public Button resetButton;

    [Header("Reset Option")]
    public bool resetCategory = true;
    public bool resetSeries = true;
    public bool resetGrade = true;

    private bool isInitialized;
    private readonly InventorySearchFilter filter = new InventorySearchFilter();

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

        SetupDropdown<ItemCategory>(categoryDropdown);
        SetupDropdown<ItemSeries>(seriesDropdown);
        SetupDropdown<ItemGrade>(gradeDropdown);

        InitFilterFromInventoryUI();
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

    private void ApplyDropdownValueFromFilter()
    {
        SetDropdownValueWithoutNotify(
            categoryDropdown,
            filter.useCategory,
            ConvertEnumToDropdownIndex(filter.category)
        );

        SetDropdownValueWithoutNotify(
            seriesDropdown,
            filter.useSeries,
            ConvertEnumToDropdownIndex(filter.series)
        );

        SetDropdownValueWithoutNotify(
            gradeDropdown,
            filter.useGrade,
            ConvertEnumToDropdownIndex(filter.grade)
        );
    }

    private void SetDropdownValueWithoutNotify(
        TMP_Dropdown dropdown,
        bool useFilter,
        int enumIndex
    )
    {
        if (dropdown == null)
            return;

        int dropdownIndex = useFilter ? enumIndex + 1 : AllIndex;
        dropdownIndex = Mathf.Clamp(dropdownIndex, 0, dropdown.options.Count - 1);

        dropdown.SetValueWithoutNotify(dropdownIndex);
    }

    private int ConvertEnumToDropdownIndex<TEnum>(TEnum value)
        where TEnum : Enum
    {
        Array values = Enum.GetValues(typeof(TEnum));
        int index = Array.IndexOf(values, value);

        if (index < 0)
            return 0;

        return index;
    }

    private void SetupDropdown<TEnum>(TMP_Dropdown dropdown)
        where TEnum : Enum
    {
        if (dropdown == null)
            return;

        dropdown.ClearOptions();
        dropdown.options.Add(new TMP_Dropdown.OptionData("ALL"));

        string[] names = Enum.GetNames(typeof(TEnum));

        for (int i = 0; i < names.Length; i++)
            dropdown.options.Add(new TMP_Dropdown.OptionData(names[i]));

        dropdown.RefreshShownValue();
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
            filter.category = ConvertDropdownIndexToEnum<ItemCategory>(index);

        ApplyFilter();
    }

    private void OnSeriesChanged(int index)
    {
        filter.useSeries = index != AllIndex;

        if (filter.useSeries)
            filter.series = ConvertDropdownIndexToEnum<ItemSeries>(index);

        ApplyFilter();
    }

    private void OnGradeChanged(int index)
    {
        filter.useGrade = index != AllIndex;

        if (filter.useGrade)
            filter.grade = ConvertDropdownIndexToEnum<ItemGrade>(index);

        ApplyFilter();
    }

    private TEnum ConvertDropdownIndexToEnum<TEnum>(int dropdownIndex)
        where TEnum : Enum
    {
        Array values = Enum.GetValues(typeof(TEnum));

        int enumIndex = Mathf.Clamp(
            dropdownIndex - 1,
            0,
            values.Length - 1
        );

        return (TEnum)values.GetValue(enumIndex);
    }

    private void ApplyFilter()
    {
        if (inventoryUI == null)
            return;

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
            categoryDropdown.SetValueWithoutNotify(AllIndex);
    }

    private void ResetSeriesFilter()
    {
        if (!CanResetSeries())
            return;

        filter.useSeries = false;

        if (seriesDropdown != null)
            seriesDropdown.SetValueWithoutNotify(AllIndex);
    }

    private void ResetGradeFilter()
    {
        if (!CanResetGrade())
            return;

        filter.useGrade = false;

        if (gradeDropdown != null)
            gradeDropdown.SetValueWithoutNotify(AllIndex);
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