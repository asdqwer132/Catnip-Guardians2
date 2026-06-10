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

    [Header("Button")]
    public Button resetButton;

    private bool isInitialized;
    private readonly InventorySearchFilter filter = new InventorySearchFilter();

    private const int AllIndex = 0;

    private void Awake()
    {
        Init();
    }

    private void OnEnable()
    {
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

        BindEvents();

        isInitialized = true;

        ApplyFilter();
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
            resetButton.onClick.AddListener(ResetSearch);
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
            resetButton.onClick.RemoveListener(ResetSearch);
    }

    private void SetupDropdown<TEnum>(TMP_Dropdown dropdown) where TEnum : Enum
    {
        if (dropdown == null)
            return;

        dropdown.ClearOptions();

        List<string> options = new List<string>();
        options.Add("ALL");

        Array values = Enum.GetValues(typeof(TEnum));

        for (int i = 0; i < values.Length; i++)
            options.Add(values.GetValue(i).ToString());

        dropdown.AddOptions(options);
        dropdown.SetValueWithoutNotify(AllIndex);
        dropdown.RefreshShownValue();
    }

    private void OnCategoryChanged(int index)
    {
        filter.useCategory = index != AllIndex;

        if (filter.useCategory)
            filter.category = GetEnumByDropdownIndex<ItemCategory>(index);

        ApplyFilter();
    }

    private void OnSeriesChanged(int index)
    {
        filter.useSeries = index != AllIndex;

        if (filter.useSeries)
            filter.series = GetEnumByDropdownIndex<ItemSeries>(index);

        ApplyFilter();
    }

    private void OnGradeChanged(int index)
    {
        filter.useGrade = index != AllIndex;

        if (filter.useGrade)
            filter.grade = GetEnumByDropdownIndex<ItemGrade>(index);

        ApplyFilter();
    }

    private TEnum GetEnumByDropdownIndex<TEnum>(int dropdownIndex) where TEnum : Enum
    {
        Array values = Enum.GetValues(typeof(TEnum));

        int enumIndex = dropdownIndex - 1;
        enumIndex = Mathf.Clamp(enumIndex, 0, values.Length - 1);

        return (TEnum)values.GetValue(enumIndex);
    }

    private void ApplyFilter()
    {
        if (inventoryUI == null)
            return;

        inventoryUI.SetSearchFilter(filter);
    }

    public void ResetSearch()
    {
        filter.Clear();

        if (categoryDropdown != null)
            categoryDropdown.SetValueWithoutNotify(AllIndex);

        if (seriesDropdown != null)
            seriesDropdown.SetValueWithoutNotify(AllIndex);

        if (gradeDropdown != null)
            gradeDropdown.SetValueWithoutNotify(AllIndex);

        if (categoryDropdown != null)
            categoryDropdown.RefreshShownValue();

        if (seriesDropdown != null)
            seriesDropdown.RefreshShownValue();

        if (gradeDropdown != null)
            gradeDropdown.RefreshShownValue();

        ApplyFilter();
    }
}