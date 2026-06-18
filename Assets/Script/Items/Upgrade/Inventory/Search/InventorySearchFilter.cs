using System;

[Serializable]
public class InventorySearchFilter
{
    public bool useCategory;
    public ItemCategory category;

    public bool useSeries;
    public ItemSeries series;

    public bool useGrade;
    public ItemGrade grade;

    private ItemCategory[] categoryMask;
    private ItemSeries[] seriesMask;
    private ItemGrade[] gradeMask;

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

    public void SetMask(
        ItemCategory[] categoryMask,
        ItemSeries[] seriesMask,
        ItemGrade[] gradeMask
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