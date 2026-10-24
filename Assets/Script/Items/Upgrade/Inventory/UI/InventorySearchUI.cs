using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySearchUI : ItemSearchUIBase
{
    [Header("Dropdown")]
    [SerializeField] private TMP_Dropdown categoryDropdown;
    [SerializeField] private TMP_Dropdown seriesDropdown;
    [SerializeField] private TMP_Dropdown gradeDropdown;

    [Header("Dropdown Option")]
    [SerializeField] private string allOptionText = "ALL";

    [Serializable]
    public class DropdownUITemplate
    {
        [Tooltip("The authored dropdown list root in the scene, usually under this dropdown. Leave empty to use the TMP_Dropdown's existing template.")]
        public RectTransform template;
        [Tooltip("The label on the template's item Toggle or one of its children.")]
        public TMP_Text itemText;
        public Image itemImage;

        public void ApplyTo(TMP_Dropdown dropdown)
        {
            if (dropdown == null || template == null || template == dropdown.transform)
                return;

            dropdown.template = template;
            dropdown.itemText = itemText;
            dropdown.itemImage = itemImage;
            template.gameObject.SetActive(false);
        }
    }

    [Header("Optional Dropdown UI Templates")]
    [SerializeField] private DropdownUITemplate categoryTemplate = new DropdownUITemplate();
    [SerializeField] private DropdownUITemplate seriesTemplate = new DropdownUITemplate();
    [SerializeField] private DropdownUITemplate gradeTemplate = new DropdownUITemplate();

    private const int AllIndex = 0;

    private readonly List<ItemCategory> categoryValues = new List<ItemCategory>();
    private readonly List<ItemSeries> seriesValues = new List<ItemSeries>();
    private readonly List<ItemGrade> gradeValues = new List<ItemGrade>();

    protected override void RebuildControls()
    {
        categoryTemplate?.ApplyTo(categoryDropdown);
        seriesTemplate?.ApplyTo(seriesDropdown);
        gradeTemplate?.ApplyTo(gradeDropdown);

        RebuildCategoryDropdown();
        RebuildSeriesDropdown();
        RebuildGradeDropdown();
    }

    protected override void BindControlEvents()
    {
        if (categoryDropdown != null)
            categoryDropdown.onValueChanged.AddListener(OnCategoryChanged);

        if (seriesDropdown != null)
            seriesDropdown.onValueChanged.AddListener(OnSeriesChanged);

        if (gradeDropdown != null)
            gradeDropdown.onValueChanged.AddListener(OnGradeChanged);
    }

    protected override void UnbindControlEvents()
    {
        if (categoryDropdown != null)
            categoryDropdown.onValueChanged.RemoveListener(OnCategoryChanged);

        if (seriesDropdown != null)
            seriesDropdown.onValueChanged.RemoveListener(OnSeriesChanged);

        if (gradeDropdown != null)
            gradeDropdown.onValueChanged.RemoveListener(OnGradeChanged);
    }

    protected override void ApplyFilterToControlsWithoutNotify()
    {
        ApplyCategoryDropdownValue();
        ApplySeriesDropdownValue();
        ApplyGradeDropdownValue();
    }

    private void RebuildCategoryDropdown()
    {
        categoryValues.Clear();

        if (categoryDropdown == null)
            return;

        List<string> options = new List<string> { allOptionText };
        Array values = Enum.GetValues(typeof(ItemCategory));

        for (int i = 0; i < values.Length; i++)
        {
            ItemCategory value = (ItemCategory)values.GetValue(i);

            if (IsCategoryMasked(value))
                continue;

            categoryValues.Add(value);
            options.Add(value.ToString());
        }

        categoryDropdown.ClearOptions();
        categoryDropdown.AddOptions(options);
        categoryDropdown.RefreshShownValue();
    }

    private void RebuildSeriesDropdown()
    {
        seriesValues.Clear();

        if (seriesDropdown == null)
            return;

        List<string> options = new List<string> { allOptionText };
        Array values = Enum.GetValues(typeof(ItemSeries));

        for (int i = 0; i < values.Length; i++)
        {
            ItemSeries value = (ItemSeries)values.GetValue(i);

            if (IsSeriesMasked(value))
                continue;

            seriesValues.Add(value);
            options.Add(value.ToString());
        }

        seriesDropdown.ClearOptions();
        seriesDropdown.AddOptions(options);
        seriesDropdown.RefreshShownValue();
    }

    private void RebuildGradeDropdown()
    {
        gradeValues.Clear();

        if (gradeDropdown == null)
            return;

        List<string> options = new List<string> { allOptionText };
        Array values = Enum.GetValues(typeof(ItemGrade));

        for (int i = 0; i < values.Length; i++)
        {
            ItemGrade value = (ItemGrade)values.GetValue(i);

            if (IsGradeMasked(value))
                continue;

            gradeValues.Add(value);
            options.Add(value.ToString());
        }

        gradeDropdown.ClearOptions();
        gradeDropdown.AddOptions(options);
        gradeDropdown.RefreshShownValue();
    }

    private void OnCategoryChanged(int dropdownIndex)
    {
        if (dropdownIndex == AllIndex)
        {
            ChangeCategoryFilter(false, default(ItemCategory));
            return;
        }

        int valueIndex = dropdownIndex - 1;

        if (valueIndex < 0 || valueIndex >= categoryValues.Count)
            return;

        ChangeCategoryFilter(true, categoryValues[valueIndex]);
    }

    private void OnSeriesChanged(int dropdownIndex)
    {
        if (dropdownIndex == AllIndex)
        {
            ChangeSeriesFilter(false, default(ItemSeries));
            return;
        }

        int valueIndex = dropdownIndex - 1;

        if (valueIndex < 0 || valueIndex >= seriesValues.Count)
            return;

        ChangeSeriesFilter(true, seriesValues[valueIndex]);
    }

    private void OnGradeChanged(int dropdownIndex)
    {
        if (dropdownIndex == AllIndex)
        {
            ChangeGradeFilter(false, default(ItemGrade));
            return;
        }

        int valueIndex = dropdownIndex - 1;

        if (valueIndex < 0 || valueIndex >= gradeValues.Count)
            return;

        ChangeGradeFilter(true, gradeValues[valueIndex]);
    }

    private void ApplyCategoryDropdownValue()
    {
        if (categoryDropdown == null)
            return;

        int dropdownIndex = AllIndex;

        if (filter != null && filter.useCategory)
        {
            int valueIndex = categoryValues.IndexOf(filter.category);

            if (valueIndex >= 0)
                dropdownIndex = valueIndex + 1;
        }

        categoryDropdown.SetValueWithoutNotify(dropdownIndex);
        categoryDropdown.RefreshShownValue();
    }

    private void ApplySeriesDropdownValue()
    {
        if (seriesDropdown == null)
            return;

        int dropdownIndex = AllIndex;

        if (filter != null && filter.useSeries)
        {
            int valueIndex = seriesValues.IndexOf(filter.series);

            if (valueIndex >= 0)
                dropdownIndex = valueIndex + 1;
        }

        seriesDropdown.SetValueWithoutNotify(dropdownIndex);
        seriesDropdown.RefreshShownValue();
    }

    private void ApplyGradeDropdownValue()
    {
        if (gradeDropdown == null)
            return;

        int dropdownIndex = AllIndex;

        if (filter != null && filter.useGrade)
        {
            int valueIndex = gradeValues.IndexOf(filter.grade);

            if (valueIndex >= 0)
                dropdownIndex = valueIndex + 1;
        }

        gradeDropdown.SetValueWithoutNotify(dropdownIndex);
        gradeDropdown.RefreshShownValue();
    }
}
