using System;
using UnityEngine;
using UnityEngine.UI;

public abstract class ItemSearchUIBase : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] protected ItemSearchFilterTargetUI targetUI;

    [Header("Reset")]
    [SerializeField] protected Button resetButton;
    public bool resetCategory = true;
    public bool resetSeries = true;
    public bool resetGrade = true;

    [Header("Runtime Filter")]
    [SerializeField] protected InventorySearchFilter filter = new InventorySearchFilter();

    public event Action OnFilterChanged;

    private bool isEventBound;

    protected virtual void Awake()
    {
        ResolveTarget();
        EnsureFilter();
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
        EnsureFilter();
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
        EnsureFilter();
        UnbindEvents();

        RebuildControls();
        CopyFilterFromTarget();
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
        EnsureFilter();
        InventorySearchFilterUtil.Copy(externalFilter, filter);

        NormalizeFilter();
        ApplyFilterToControlsWithoutNotify();
        ApplyFilter(notifyFilterChanged);
    }

    public void ResetFilter()
    {
        EnsureFilter();

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

        EnsureFilter();
        filter.useCategory = false;
        ApplyFilterToControlsWithoutNotify();
        ApplyFilter(true);
    }

    public void ResetSeriesFilter()
    {
        if (!resetSeries)
            return;

        EnsureFilter();
        filter.useSeries = false;
        ApplyFilterToControlsWithoutNotify();
        ApplyFilter(true);
    }

    public void ResetGradeFilter()
    {
        if (!resetGrade)
            return;

        EnsureFilter();
        filter.useGrade = false;
        ApplyFilterToControlsWithoutNotify();
        ApplyFilter(true);
    }

    protected void ChangeCategoryFilter(bool useCategory, ItemCategory category)
    {
        EnsureFilter();

        if (useCategory && IsCategoryMasked(category))
            useCategory = false;

        filter.useCategory = useCategory;

        if (useCategory)
            filter.category = category;

        ApplyFilter(true);
    }

    protected void ChangeSeriesFilter(bool useSeries, ItemSeries series)
    {
        EnsureFilter();

        if (useSeries && IsSeriesMasked(series))
            useSeries = false;

        filter.useSeries = useSeries;

        if (useSeries)
            filter.series = series;

        ApplyFilter(true);
    }

    protected void ChangeGradeFilter(bool useGrade, ItemGrade grade)
    {
        EnsureFilter();

        if (useGrade && IsGradeMasked(grade))
            useGrade = false;

        filter.useGrade = useGrade;

        if (useGrade)
            filter.grade = grade;

        ApplyFilter(true);
    }

    protected bool IsCategoryMasked(ItemCategory value)
    {
        return targetUI != null && targetUI.IsCategoryMasked(value);
    }

    protected bool IsSeriesMasked(ItemSeries value)
    {
        return targetUI != null && targetUI.IsSeriesMasked(value);
    }

    protected bool IsGradeMasked(ItemGrade value)
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

    private void EnsureFilter()
    {
        if (filter == null)
            filter = new InventorySearchFilter();
    }

    private void CopyFilterFromTarget()
    {
        if (targetUI == null)
            return;

        InventorySearchFilterUtil.Copy(targetUI.GetSearchFilter(), filter);
    }

    private void NormalizeFilter()
    {
        EnsureFilter();

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
        EnsureFilter();
        NormalizeFilter();

        if (targetUI != null)
        {
            targetUI.SetSearchFilter(filter);
            InventorySearchFilterUtil.Copy(targetUI.GetSearchFilter(), filter);
        }

        ApplyFilterToControlsWithoutNotify();

        if (notifyFilterChanged)
            OnFilterChanged?.Invoke();
    }

    private void BindEvents()
    {
        if (isEventBound)
            return;

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetFilter);

        BindControlEvents();
        isEventBound = true;
    }

    private void UnbindEvents()
    {
        if (!isEventBound)
            return;

        if (resetButton != null)
            resetButton.onClick.RemoveListener(ResetFilter);

        UnbindControlEvents();
        isEventBound = false;
    }
}
