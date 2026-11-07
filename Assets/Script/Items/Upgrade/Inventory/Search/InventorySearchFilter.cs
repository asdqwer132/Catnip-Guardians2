using System;

[Serializable]
public class InventorySearchFilter
{
    public bool useCategory;
    public Category category;

    public bool useSeries;
    public ItemSeries series;

    public bool useGrade;
    public Grade grade;

    private Category[] categoryMask;
    private ItemSeries[] seriesMask;
    private Grade[] gradeMask;

    public bool IsMatch(ISearchable item)
    {
        if (item == null)
            return false;


        if (IsMasked(item))
            return false;

        if (useCategory && item.GetItemCategory() != category)
            return false;

        if (useSeries && item.GetItemSeries() != series)
            return false;

        if (useGrade && item.GetGrade() != grade)
            return false;

        return true;
    }

    public bool IsMasked(ISearchable data)
    {
        if (data == null)
            return true;

        if (IsCategoryMasked(data.GetItemCategory()))
            return true;

        if (IsSeriesMasked(data.GetItemSeries()))
            return true;

        if (IsGradeMasked(data.GetGrade()))
            return true;

        return false;
    }

    public bool IsCategoryMasked(Category value)
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

    public bool IsGradeMasked(Grade value)
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

    public void SetMask(
        Category[] categoryMask,
        ItemSeries[] seriesMask,
        Grade[] gradeMask
    )
    {
        this.categoryMask = categoryMask;
        this.seriesMask = seriesMask;
        this.gradeMask = gradeMask;
    }

    public void Clear()
    {
        useCategory = false;
        useSeries = false;
        useGrade = false;
    }
}