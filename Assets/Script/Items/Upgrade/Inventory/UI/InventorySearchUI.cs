using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySearchUI : InventoryToggleSearchUI
{
    [Header("Dropdown")]
    [SerializeField] private TMP_Dropdown categoryDropdown;
    [SerializeField] private TMP_Dropdown seriesDropdown;
    [SerializeField] private TMP_Dropdown gradeDropdown;

    [Header("Dropdown Option")]
    [SerializeField] private string allOptionText = "ALL";

    [Header("Authored Dropdown Items")]
    [Tooltip("Use the existing Category/Series/Grade Toggle Options instead of generating TMP_Dropdown entries. Assign each authored Toggle and its filter value above.")]
    [SerializeField] private bool useAssignedItems;

    [Serializable]
    public class AssignedDropdownPanel
    {
        public Button openButton;
        public GameObject optionsPanel;
        public TMP_Text selectedText;
    }

    [SerializeField] private AssignedDropdownPanel categoryPanel = new AssignedDropdownPanel();
    [SerializeField] private AssignedDropdownPanel seriesPanel = new AssignedDropdownPanel();
    [SerializeField] private AssignedDropdownPanel gradePanel = new AssignedDropdownPanel();

    private readonly Dictionary<TMP_Dropdown, bool> originalDropdownStates = new Dictionary<TMP_Dropdown, bool>();

    private const int AllIndex = 0;

    private readonly List<ItemCategory> categoryValues = new List<ItemCategory>();
    private readonly List<ItemSeries> seriesValues = new List<ItemSeries>();
    private readonly List<ItemGrade> gradeValues = new List<ItemGrade>();

    protected override void RebuildControls()
    {
        SetAutomaticDropdownEnabled(categoryDropdown);
        SetAutomaticDropdownEnabled(seriesDropdown);
        SetAutomaticDropdownEnabled(gradeDropdown);
        CloseAssignedPanels();
        if (useAssignedItems)
        {
            base.RebuildControls();
            return;
        }

        RebuildCategoryDropdown();
        RebuildSeriesDropdown();
        RebuildGradeDropdown();
    }

    protected override void BindControlEvents()
    {
        if (useAssignedItems)
        {
            base.BindControlEvents();
            categoryPanel?.openButton?.onClick.AddListener(ToggleCategoryPanel);
            seriesPanel?.openButton?.onClick.AddListener(ToggleSeriesPanel);
            gradePanel?.openButton?.onClick.AddListener(ToggleGradePanel);
            OnFilterChanged += CloseAssignedPanels;
            return;
        }

        if (categoryDropdown != null)
            categoryDropdown.onValueChanged.AddListener(OnCategoryChanged);

        if (seriesDropdown != null)
            seriesDropdown.onValueChanged.AddListener(OnSeriesChanged);

        if (gradeDropdown != null)
            gradeDropdown.onValueChanged.AddListener(OnGradeChanged);
    }

    protected override void UnbindControlEvents()
    {
        base.UnbindControlEvents();
        categoryPanel?.openButton?.onClick.RemoveListener(ToggleCategoryPanel);
        seriesPanel?.openButton?.onClick.RemoveListener(ToggleSeriesPanel);
        gradePanel?.openButton?.onClick.RemoveListener(ToggleGradePanel);
        OnFilterChanged -= CloseAssignedPanels;

        if (categoryDropdown != null)
            categoryDropdown.onValueChanged.RemoveListener(OnCategoryChanged);

        if (seriesDropdown != null)
            seriesDropdown.onValueChanged.RemoveListener(OnSeriesChanged);

        if (gradeDropdown != null)
            gradeDropdown.onValueChanged.RemoveListener(OnGradeChanged);
    }

    protected override void ApplyFilterToControlsWithoutNotify()
    {
        if (useAssignedItems)
        {
            base.ApplyFilterToControlsWithoutNotify();
            UpdateAssignedCaption(categoryPanel, categoryAllToggle, categoryOptions?.Select(option => option?.toggle));
            UpdateAssignedCaption(seriesPanel, seriesAllToggle, seriesOptions?.Select(option => option?.toggle));
            UpdateAssignedCaption(gradePanel, gradeAllToggle, gradeOptions?.Select(option => option?.toggle));
            return;
        }

        ApplyCategoryDropdownValue();
        ApplySeriesDropdownValue();
        ApplyGradeDropdownValue();
    }

    protected override void NormalizeControlSpecificFilter()
    {
        if (useAssignedItems)
            base.NormalizeControlSpecificFilter();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        CloseAssignedPanels();
    }

    private void SetAutomaticDropdownEnabled(TMP_Dropdown dropdown)
    {
        if (dropdown == null)
            return;
        if (useAssignedItems)
        {
            if (!originalDropdownStates.ContainsKey(dropdown))
                originalDropdownStates.Add(dropdown, dropdown.enabled);
            dropdown.enabled = false;
        }
        else if (originalDropdownStates.TryGetValue(dropdown, out bool wasEnabled))
        {
            dropdown.enabled = wasEnabled;
            originalDropdownStates.Remove(dropdown);
        }
    }

    private static void UpdateAssignedCaption(AssignedDropdownPanel panel, Toggle allToggle, IEnumerable<Toggle> options)
    {
        if (panel?.selectedText == null)
            return;
        Toggle selected = allToggle != null && allToggle.isOn ? allToggle :
            options?.FirstOrDefault(toggle => toggle != null && toggle.isOn);
        TMP_Text text = selected != null ? selected.GetComponentInChildren<TMP_Text>(true) : null;
        if (text != null)
            panel.selectedText.text = text.text;
    }

    private void ToggleCategoryPanel() => ToggleAssignedPanel(categoryPanel);
    private void ToggleSeriesPanel() => ToggleAssignedPanel(seriesPanel);
    private void ToggleGradePanel() => ToggleAssignedPanel(gradePanel);

    private void ToggleAssignedPanel(AssignedDropdownPanel panel)
    {
        if (panel?.optionsPanel == null)
            return;
        bool open = !panel.optionsPanel.activeSelf;
        CloseAssignedPanels();
        panel.optionsPanel.SetActive(open);
    }

    private void CloseAssignedPanels()
    {
        if (categoryPanel?.optionsPanel != null) categoryPanel.optionsPanel.SetActive(false);
        if (seriesPanel?.optionsPanel != null) seriesPanel.optionsPanel.SetActive(false);
        if (gradePanel?.optionsPanel != null) gradePanel.optionsPanel.SetActive(false);
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
