using UnityEngine;

public abstract class ItemSearchFilterTargetUI : MonoBehaviour
{
    [Header("Search")]
    [SerializeField] protected InventorySearchFilter searchFilter = new InventorySearchFilter();

    [Header("Search Mask")]
    public ItemCategory[] categoryMask;
    public ItemSeries[] seriesMask;
    public ItemGrade[] gradeMask;

    public InventorySearchFilter GetSearchFilter()
    {
        if (searchFilter == null)
            searchFilter = new InventorySearchFilter();

        ApplyMaskToSearchFilter();

        return searchFilter;
    }

    public virtual void SetSearchFilter(InventorySearchFilter filter)
    {
        if (searchFilter == null)
            searchFilter = new InventorySearchFilter();

        CopySearchFilter(filter, searchFilter);
        ApplyMaskToSearchFilter();
        RemoveMaskedFilterValue();

        OnSearchFilterChanged();
    }

    public virtual void ClearSearchFilter()
    {
        if (searchFilter == null)
            searchFilter = new InventorySearchFilter();

        searchFilter.Clear();
        ApplyMaskToSearchFilter();

        OnSearchFilterChanged();
    }

    protected abstract void OnSearchFilterChanged();

    public bool IsCategoryMasked(ItemCategory value)
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

    public bool IsSeriesMasked(ItemSeries value)
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

    public bool IsGradeMasked(ItemGrade value)
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

    protected void ApplyMaskToSearchFilter()
    {
        if (searchFilter == null)
            searchFilter = new InventorySearchFilter();

        searchFilter.SetMask(categoryMask, seriesMask, gradeMask);
    }

    protected void RemoveMaskedFilterValue()
    {
        if (searchFilter == null)
            return;

        if (searchFilter.useCategory && IsCategoryMasked(searchFilter.category))
            searchFilter.useCategory = false;

        if (searchFilter.useSeries && IsSeriesMasked(searchFilter.series))
            searchFilter.useSeries = false;

        if (searchFilter.useGrade && IsGradeMasked(searchFilter.grade))
            searchFilter.useGrade = false;
    }

    protected bool IsInventoryItemVisible(InventoryItem item)
    {
        if (item == null || item.itemData == null)
            return false;

        return IsItemDataVisible(item.itemData);
    }

    protected bool IsItemDataVisible(ItemData itemData)
    {
        if (itemData == null)
            return false;

        if (IsCategoryMasked(itemData.category))
            return false;

        if (IsSeriesMasked(itemData.series))
            return false;

        if (IsGradeMasked(itemData.grade))
            return false;

        if (searchFilter == null)
            return true;

        if (searchFilter.useCategory && itemData.category != searchFilter.category)
            return false;

        if (searchFilter.useSeries && itemData.series != searchFilter.series)
            return false;

        if (searchFilter.useGrade && itemData.grade != searchFilter.grade)
            return false;

        return true;
    }

    private void CopySearchFilter(InventorySearchFilter from, InventorySearchFilter to)
    {
        if (to == null)
            return;

        if (from == null)
        {
            to.Clear();
            return;
        }

        to.useCategory = from.useCategory;
        to.category = from.category;

        to.useSeries = from.useSeries;
        to.series = from.series;

        to.useGrade = from.useGrade;
        to.grade = from.grade;
    }
}