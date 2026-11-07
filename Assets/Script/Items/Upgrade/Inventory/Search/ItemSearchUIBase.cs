using System;
using UnityEngine;

public abstract class ItemSearchUIBase : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] protected ItemSearchFilterTargetUI targetUI;

    [Header("Reset")]
    public bool resetCategory = true;
    public bool resetSeries = true;
    public bool resetGrade = true;

    private readonly InventorySearchFilter localFilter = new InventorySearchFilter();
    // A connected search UI reads the target's live filter, including its masks.
    protected InventorySearchFilter filter
    {
        get
        {
            ResolveTarget();
            return targetUI != null ? targetUI.GetSearchFilter() : localFilter;
        }
    }

    public event Action OnFilterChanged;

    private bool isEventBound;

    protected virtual void Awake()
    {
        ResolveTarget();
    }

    protected virtual void OnEnable()
    {
        RefreshSearchUI();
    }

    protected virtual void OnDisable()
    {
        UnbindEvents();
    }

    public InventorySearchFilter GetCurrentFilter()
    {
        return filter;
    }

    // 기존 동기화 코드와 연결하기 위한 별칭입니다.
    public InventorySearchFilter GetFilter()
    {
        return GetCurrentFilter();
    }

    public InventorySearchFilter GetSearchFilter()
    {
        return GetCurrentFilter();
    }

    public void Init()
    {
        RefreshSearchUI();
    }

    public void ResetSearch()
    {
        ResetFilter();
    }

    public void ClearSearchFilter()
    {
        ResetFilter();
    }

    public ItemSearchFilterTargetUI GetTargetUI()
    {
        return targetUI;
    }

    public void SetTargetUI(ItemSearchFilterTargetUI newTargetUI)
    {
        if (targetUI == newTargetUI)
            return;

        UnbindEvents();
        targetUI = newTargetUI;

        if (isActiveAndEnabled)
            RefreshSearchUI();
    }

    public void RefreshSearchUI()
    {
        ResolveTarget();
        UnbindEvents();

        RebuildControls();
        NormalizeFilter();
        ApplyFilterToControlsWithoutNotify();

        BindEvents();
        ApplyFilter(false);
    }

    public void SetFilterFromExternal(InventorySearchFilter externalFilter)
    {
        SetFilterFromExternal(externalFilter, true);
    }

    public void SetFilterFromExternal(
        InventorySearchFilter externalFilter,
        bool notifyFilterChanged
    )
    {
        InventorySearchFilterUtil.Copy(externalFilter, filter);

        NormalizeFilter();
        ApplyFilterToControlsWithoutNotify();
        ApplyFilter(notifyFilterChanged);
    }

    public void ResetFilter()
    {
        if (resetCategory)
            filter.useCategory = false;

        if (resetSeries)
            filter.useSeries = false;

        if (resetGrade)
            filter.useGrade = false;

        NormalizeFilter();
        ApplyFilterToControlsWithoutNotify();
        ApplyFilter(true);
    }

    public void ResetCategoryFilter()
    {
        if (!resetCategory)
            return;

        filter.useCategory = false;
        ApplyFilterToControlsWithoutNotify();
        ApplyFilter(true);
    }

    public void ResetSeriesFilter()
    {
        if (!resetSeries)
            return;

        filter.useSeries = false;
        ApplyFilterToControlsWithoutNotify();
        ApplyFilter(true);
    }

    public void ResetGradeFilter()
    {
        if (!resetGrade)
            return;

        filter.useGrade = false;
        ApplyFilterToControlsWithoutNotify();
        ApplyFilter(true);
    }

    protected void ChangeCategoryFilter(bool useCategory, Category category)
    {
        if (useCategory && IsCategoryMasked(category))
            useCategory = false;

        filter.useCategory = useCategory;

        if (useCategory)
            filter.category = category;

        ApplyFilter(true);
    }

    protected void ChangeSeriesFilter(bool useSeries, ItemSeries series)
    {
        if (useSeries && IsSeriesMasked(series))
            useSeries = false;

        filter.useSeries = useSeries;

        if (useSeries)
            filter.series = series;

        ApplyFilter(true);
    }

    protected void ChangeGradeFilter(bool useGrade, Grade grade)
    {
        if (useGrade && IsGradeMasked(grade))
            useGrade = false;

        filter.useGrade = useGrade;

        if (useGrade)
            filter.grade = grade;

        ApplyFilter(true);
    }

    protected bool IsCategoryMasked(Category value)
    {
        return targetUI != null && targetUI.IsCategoryMasked(value);
    }

    protected bool IsSeriesMasked(ItemSeries value)
    {
        return targetUI != null && targetUI.IsSeriesMasked(value);
    }

    protected bool IsGradeMasked(Grade value)
    {
        return targetUI != null && targetUI.IsGradeMasked(value);
    }

    protected virtual void NormalizeControlSpecificFilter()
    {
    }

    protected abstract void RebuildControls();
    protected abstract void BindControlEvents();
    protected abstract void UnbindControlEvents();
    protected abstract void ApplyFilterToControlsWithoutNotify();

    private void ResolveTarget()
    {
        if (targetUI != null)
            return;

        targetUI = GetComponent<ItemSearchFilterTargetUI>();

        if (targetUI == null)
            targetUI = GetComponentInParent<ItemSearchFilterTargetUI>();
    }

    private void NormalizeFilter()
    {
        if (filter.useCategory && IsCategoryMasked(filter.category))
            filter.useCategory = false;

        if (filter.useSeries && IsSeriesMasked(filter.series))
            filter.useSeries = false;

        if (filter.useGrade && IsGradeMasked(filter.grade))
            filter.useGrade = false;

        NormalizeControlSpecificFilter();
    }

    private void ApplyFilter(bool notifyFilterChanged)
    {
        NormalizeFilter();

        if (targetUI != null) targetUI.SetSearchFilter(filter);

        ApplyFilterToControlsWithoutNotify();

        if (notifyFilterChanged)
            OnFilterChanged?.Invoke();
    }

    private void BindEvents()
    {
        if (isEventBound)
            return;

        BindControlEvents();
        isEventBound = true;
    }

    private void UnbindEvents()
    {
        if (!isEventBound)
            return;

        UnbindControlEvents();
        isEventBound = false;
    }
}
