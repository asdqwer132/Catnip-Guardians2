using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class InventoryToggleSearchUI : ItemSearchUIBase
{
    [Serializable]
    public class CategoryToggleOption
    {
        public Toggle toggle;
        public ItemCategory value;
    }

    [Serializable]
    public class SeriesToggleOption
    {
        public Toggle toggle;
        public ItemSeries value;
    }

    [Serializable]
    public class GradeToggleOption
    {
        public Toggle toggle;
        public ItemGrade value;
    }

    [Header("Category Toggle")]
    [SerializeField] private ToggleGroup categoryToggleGroup;
    [SerializeField] private Toggle categoryAllToggle;
    [SerializeField] private CategoryToggleOption[] categoryOptions;

    [Header("Series Toggle")]
    [SerializeField] private ToggleGroup seriesToggleGroup;
    [SerializeField] private Toggle seriesAllToggle;
    [SerializeField] private SeriesToggleOption[] seriesOptions;

    [Header("Grade Toggle")]
    [SerializeField] private ToggleGroup gradeToggleGroup;
    [SerializeField] private Toggle gradeAllToggle;
    [SerializeField] private GradeToggleOption[] gradeOptions;

    [Header("Masked Toggle")]
    [Tooltip("체크하면 마스크된 값의 토글 GameObject를 끕니다. 체크하지 않으면 비활성화만 합니다.")]
    [SerializeField] private bool hideMaskedToggles = true;

    private UnityAction<bool> categoryAllAction;
    private UnityAction<bool> seriesAllAction;
    private UnityAction<bool> gradeAllAction;

    private UnityAction<bool>[] categoryActions;
    private UnityAction<bool>[] seriesActions;
    private UnityAction<bool>[] gradeActions;

    protected override void RebuildControls()
    {
        ResolveToggleGroups();
        SetupToggleGroups();
        ApplyCategoryMasks();
        ApplySeriesMasks();
        ApplyGradeMasks();
    }

    protected override void NormalizeControlSpecificFilter()
    {
        if (filter == null)
            return;

        if (filter.useCategory && !HasUsableCategoryOption(filter.category))
            filter.useCategory = false;

        if (filter.useSeries && !HasUsableSeriesOption(filter.series))
            filter.useSeries = false;

        if (filter.useGrade && !HasUsableGradeOption(filter.grade))
            filter.useGrade = false;
    }

    protected override void BindControlEvents()
    {
        BindCategoryEvents();
        BindSeriesEvents();
        BindGradeEvents();
    }

    protected override void UnbindControlEvents()
    {
        UnbindCategoryEvents();
        UnbindSeriesEvents();
        UnbindGradeEvents();
    }

    protected override void ApplyFilterToControlsWithoutNotify()
    {
        ApplyCategoryToggleValue();
        ApplySeriesToggleValue();
        ApplyGradeToggleValue();
    }

    private void ResolveToggleGroups()
    {
        if (categoryToggleGroup == null && categoryAllToggle != null)
            categoryToggleGroup = categoryAllToggle.group;

        if (seriesToggleGroup == null && seriesAllToggle != null)
            seriesToggleGroup = seriesAllToggle.group;

        if (gradeToggleGroup == null && gradeAllToggle != null)
            gradeToggleGroup = gradeAllToggle.group;
    }

    private void SetupToggleGroups()
    {
        SetupCategoryToggleGroup();
        SetupSeriesToggleGroup();
        SetupGradeToggleGroup();
    }

    private void SetupCategoryToggleGroup()
    {
        if (categoryToggleGroup == null)
            return;

        categoryToggleGroup.allowSwitchOff = false;

        if (categoryAllToggle != null)
            categoryAllToggle.group = categoryToggleGroup;

        if (categoryOptions == null)
            return;

        for (int i = 0; i < categoryOptions.Length; i++)
        {
            if (categoryOptions[i] != null && categoryOptions[i].toggle != null)
                categoryOptions[i].toggle.group = categoryToggleGroup;
        }
    }

    private void SetupSeriesToggleGroup()
    {
        if (seriesToggleGroup == null)
            return;

        seriesToggleGroup.allowSwitchOff = false;

        if (seriesAllToggle != null)
            seriesAllToggle.group = seriesToggleGroup;

        if (seriesOptions == null)
            return;

        for (int i = 0; i < seriesOptions.Length; i++)
        {
            if (seriesOptions[i] != null && seriesOptions[i].toggle != null)
                seriesOptions[i].toggle.group = seriesToggleGroup;
        }
    }

    private void SetupGradeToggleGroup()
    {
        if (gradeToggleGroup == null)
            return;

        gradeToggleGroup.allowSwitchOff = false;

        if (gradeAllToggle != null)
            gradeAllToggle.group = gradeToggleGroup;

        if (gradeOptions == null)
            return;

        for (int i = 0; i < gradeOptions.Length; i++)
        {
            if (gradeOptions[i] != null && gradeOptions[i].toggle != null)
                gradeOptions[i].toggle.group = gradeToggleGroup;
        }
    }

    private void ApplyCategoryMasks()
    {
        if (categoryOptions == null)
            return;

        for (int i = 0; i < categoryOptions.Length; i++)
        {
            CategoryToggleOption option = categoryOptions[i];

            if (option == null || option.toggle == null)
                continue;

            ApplyMaskedState(option.toggle, IsCategoryMasked(option.value));
        }
    }

    private void ApplySeriesMasks()
    {
        if (seriesOptions == null)
            return;

        for (int i = 0; i < seriesOptions.Length; i++)
        {
            SeriesToggleOption option = seriesOptions[i];

            if (option == null || option.toggle == null)
                continue;

            ApplyMaskedState(option.toggle, IsSeriesMasked(option.value));
        }
    }

    private void ApplyGradeMasks()
    {
        if (gradeOptions == null)
            return;

        for (int i = 0; i < gradeOptions.Length; i++)
        {
            GradeToggleOption option = gradeOptions[i];

            if (option == null || option.toggle == null)
                continue;

            ApplyMaskedState(option.toggle, IsGradeMasked(option.value));
        }
    }

    private void ApplyMaskedState(Toggle toggle, bool isMasked)
    {
        if (toggle == null)
            return;

        if (isMasked)
            toggle.SetIsOnWithoutNotify(false);

        if (hideMaskedToggles)
        {
            toggle.gameObject.SetActive(!isMasked);
        }
        else
        {
            toggle.gameObject.SetActive(true);
            toggle.interactable = !isMasked;
        }
    }

    private void BindCategoryEvents()
    {
        categoryAllAction = OnCategoryAllChanged;

        if (categoryAllToggle != null)
            categoryAllToggle.onValueChanged.AddListener(categoryAllAction);

        if (categoryOptions == null)
            return;

        categoryActions = new UnityAction<bool>[categoryOptions.Length];

        for (int i = 0; i < categoryOptions.Length; i++)
        {
            int index = i;
            CategoryToggleOption option = categoryOptions[index];

            if (option == null || option.toggle == null)
                continue;

            categoryActions[index] = isOn => OnCategoryOptionChanged(index, isOn);
            option.toggle.onValueChanged.AddListener(categoryActions[index]);
        }
    }

    private void BindSeriesEvents()
    {
        seriesAllAction = OnSeriesAllChanged;

        if (seriesAllToggle != null)
            seriesAllToggle.onValueChanged.AddListener(seriesAllAction);

        if (seriesOptions == null)
            return;

        seriesActions = new UnityAction<bool>[seriesOptions.Length];

        for (int i = 0; i < seriesOptions.Length; i++)
        {
            int index = i;
            SeriesToggleOption option = seriesOptions[index];

            if (option == null || option.toggle == null)
                continue;

            seriesActions[index] = isOn => OnSeriesOptionChanged(index, isOn);
            option.toggle.onValueChanged.AddListener(seriesActions[index]);
        }
    }

    private void BindGradeEvents()
    {
        gradeAllAction = OnGradeAllChanged;

        if (gradeAllToggle != null)
            gradeAllToggle.onValueChanged.AddListener(gradeAllAction);

        if (gradeOptions == null)
            return;

        gradeActions = new UnityAction<bool>[gradeOptions.Length];

        for (int i = 0; i < gradeOptions.Length; i++)
        {
            int index = i;
            GradeToggleOption option = gradeOptions[index];

            if (option == null || option.toggle == null)
                continue;

            gradeActions[index] = isOn => OnGradeOptionChanged(index, isOn);
            option.toggle.onValueChanged.AddListener(gradeActions[index]);
        }
    }

    private void UnbindCategoryEvents()
    {
        if (categoryAllToggle != null && categoryAllAction != null)
            categoryAllToggle.onValueChanged.RemoveListener(categoryAllAction);

        if (categoryOptions != null && categoryActions != null)
        {
            int count = Mathf.Min(categoryOptions.Length, categoryActions.Length);

            for (int i = 0; i < count; i++)
            {
                if (categoryOptions[i] == null ||
                    categoryOptions[i].toggle == null ||
                    categoryActions[i] == null)
                    continue;

                categoryOptions[i].toggle.onValueChanged.RemoveListener(categoryActions[i]);
            }
        }

        categoryAllAction = null;
        categoryActions = null;
    }

    private void UnbindSeriesEvents()
    {
        if (seriesAllToggle != null && seriesAllAction != null)
            seriesAllToggle.onValueChanged.RemoveListener(seriesAllAction);

        if (seriesOptions != null && seriesActions != null)
        {
            int count = Mathf.Min(seriesOptions.Length, seriesActions.Length);

            for (int i = 0; i < count; i++)
            {
                if (seriesOptions[i] == null ||
                    seriesOptions[i].toggle == null ||
                    seriesActions[i] == null)
                    continue;

                seriesOptions[i].toggle.onValueChanged.RemoveListener(seriesActions[i]);
            }
        }

        seriesAllAction = null;
        seriesActions = null;
    }

    private void UnbindGradeEvents()
    {
        if (gradeAllToggle != null && gradeAllAction != null)
            gradeAllToggle.onValueChanged.RemoveListener(gradeAllAction);

        if (gradeOptions != null && gradeActions != null)
        {
            int count = Mathf.Min(gradeOptions.Length, gradeActions.Length);

            for (int i = 0; i < count; i++)
            {
                if (gradeOptions[i] == null ||
                    gradeOptions[i].toggle == null ||
                    gradeActions[i] == null)
                    continue;

                gradeOptions[i].toggle.onValueChanged.RemoveListener(gradeActions[i]);
            }
        }

        gradeAllAction = null;
        gradeActions = null;
    }

    private void OnCategoryAllChanged(bool isOn)
    {
        if (!isOn)
            return;

        ApplyCategoryToggleVisual(false, default(ItemCategory));
        ChangeCategoryFilter(false, default(ItemCategory));
    }

    private void OnSeriesAllChanged(bool isOn)
    {
        if (!isOn)
            return;

        ApplySeriesToggleVisual(false, default(ItemSeries));
        ChangeSeriesFilter(false, default(ItemSeries));
    }

    private void OnGradeAllChanged(bool isOn)
    {
        if (!isOn)
            return;

        ApplyGradeToggleVisual(false, default(ItemGrade));
        ChangeGradeFilter(false, default(ItemGrade));
    }

    private void OnCategoryOptionChanged(int index, bool isOn)
    {
        if (!isOn || categoryOptions == null || index < 0 || index >= categoryOptions.Length)
            return;

        CategoryToggleOption option = categoryOptions[index];

        if (option == null || option.toggle == null || IsCategoryMasked(option.value))
            return;

        ApplyCategoryToggleVisual(true, option.value);
        ChangeCategoryFilter(true, option.value);
    }

    private void OnSeriesOptionChanged(int index, bool isOn)
    {
        if (!isOn || seriesOptions == null || index < 0 || index >= seriesOptions.Length)
            return;

        SeriesToggleOption option = seriesOptions[index];

        if (option == null || option.toggle == null || IsSeriesMasked(option.value))
            return;

        ApplySeriesToggleVisual(true, option.value);
        ChangeSeriesFilter(true, option.value);
    }

    private void OnGradeOptionChanged(int index, bool isOn)
    {
        if (!isOn || gradeOptions == null || index < 0 || index >= gradeOptions.Length)
            return;

        GradeToggleOption option = gradeOptions[index];

        if (option == null || option.toggle == null || IsGradeMasked(option.value))
            return;

        ApplyGradeToggleVisual(true, option.value);
        ChangeGradeFilter(true, option.value);
    }

    private void ApplyCategoryToggleValue()
    {
        bool useValue = filter != null && filter.useCategory;
        ItemCategory value = useValue ? filter.category : default(ItemCategory);
        ApplyCategoryToggleVisual(useValue, value);
    }

    private void ApplySeriesToggleValue()
    {
        bool useValue = filter != null && filter.useSeries;
        ItemSeries value = useValue ? filter.series : default(ItemSeries);
        ApplySeriesToggleVisual(useValue, value);
    }

    private void ApplyGradeToggleValue()
    {
        bool useValue = filter != null && filter.useGrade;
        ItemGrade value = useValue ? filter.grade : default(ItemGrade);
        ApplyGradeToggleVisual(useValue, value);
    }

    private void ApplyCategoryToggleVisual(bool useValue, ItemCategory selectedValue)
    {
        if (categoryAllToggle != null)
            categoryAllToggle.SetIsOnWithoutNotify(!useValue);

        if (categoryOptions == null)
            return;

        for (int i = 0; i < categoryOptions.Length; i++)
        {
            CategoryToggleOption option = categoryOptions[i];

            if (option == null || option.toggle == null)
                continue;

            bool selected = useValue &&
                            !IsCategoryMasked(option.value) &&
                            option.value.Equals(selectedValue);

            option.toggle.SetIsOnWithoutNotify(selected);
        }
    }

    private void ApplySeriesToggleVisual(bool useValue, ItemSeries selectedValue)
    {
        if (seriesAllToggle != null)
            seriesAllToggle.SetIsOnWithoutNotify(!useValue);

        if (seriesOptions == null)
            return;

        for (int i = 0; i < seriesOptions.Length; i++)
        {
            SeriesToggleOption option = seriesOptions[i];

            if (option == null || option.toggle == null)
                continue;

            bool selected = useValue &&
                            !IsSeriesMasked(option.value) &&
                            option.value.Equals(selectedValue);

            option.toggle.SetIsOnWithoutNotify(selected);
        }
    }

    private void ApplyGradeToggleVisual(bool useValue, ItemGrade selectedValue)
    {
        if (gradeAllToggle != null)
            gradeAllToggle.SetIsOnWithoutNotify(!useValue);

        if (gradeOptions == null)
            return;

        for (int i = 0; i < gradeOptions.Length; i++)
        {
            GradeToggleOption option = gradeOptions[i];

            if (option == null || option.toggle == null)
                continue;

            bool selected = useValue &&
                            !IsGradeMasked(option.value) &&
                            option.value.Equals(selectedValue);

            option.toggle.SetIsOnWithoutNotify(selected);
        }
    }

    private bool HasUsableCategoryOption(ItemCategory value)
    {
        if (categoryOptions == null)
            return false;

        for (int i = 0; i < categoryOptions.Length; i++)
        {
            CategoryToggleOption option = categoryOptions[i];

            if (option != null &&
                option.toggle != null &&
                option.value.Equals(value) &&
                !IsCategoryMasked(option.value))
                return true;
        }

        return false;
    }

    private bool HasUsableSeriesOption(ItemSeries value)
    {
        if (seriesOptions == null)
            return false;

        for (int i = 0; i < seriesOptions.Length; i++)
        {
            SeriesToggleOption option = seriesOptions[i];

            if (option != null &&
                option.toggle != null &&
                option.value.Equals(value) &&
                !IsSeriesMasked(option.value))
                return true;
        }

        return false;
    }

    private bool HasUsableGradeOption(ItemGrade value)
    {
        if (gradeOptions == null)
            return false;

        for (int i = 0; i < gradeOptions.Length; i++)
        {
            GradeToggleOption option = gradeOptions[i];

            if (option != null &&
                option.toggle != null &&
                option.value.Equals(value) &&
                !IsGradeMasked(option.value))
                return true;
        }

        return false;
    }
}
