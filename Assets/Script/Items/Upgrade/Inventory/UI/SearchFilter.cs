using System;

[Serializable]
public class SearchFilter
{
    public bool useCategory;
    public ItemCategory category;

    public bool useSeries;
    public ItemSeries series;

    public bool useGrade;
    public ItemGrade grade;

    public ItemCategory[] categoryMask;
    public ItemSeries[] seriesMask;
    public ItemGrade[] gradeMask;

    public bool IsMatch(InventoryItem item)
    {
        if (item == null || item.itemData == null)
            return false;

        ItemData data = item.itemData;

        if (IsMasked(data))
            return false;

        if (useCategory && data.category != category)
            return false;

        if (useSeries && data.series != series)
            return false;

        if (useGrade && data.grade != grade)
            return false;

        return true;
    }

    public bool IsMasked(ItemData data)
    {
        if (data == null)
            return true;

        if (IsCategoryMasked(data.category))
            return true;

        if (IsSeriesMasked(data.series))
            return true;

        if (IsGradeMasked(data.grade))
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