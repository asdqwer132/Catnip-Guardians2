using System;

[Serializable]
public class InventorySearchFilter
{
    public bool useCategory;
    public DataType category;

    public bool useSeries;
    public ItemSeries series;

    public bool useGrade;
    public Grade grade;

    public string query = string.Empty;

    private DataType[] categoryMask;
    private ItemSeries[] seriesMask;
    private Grade[] gradeMask;

    public bool IsMatch(ISearchable item)
    {
        if (item == null)
            return false;


        if (IsMasked(item))
            return false;

        if (useCategory && item.GetDataType() != category)
            return false;

        if (useSeries && item.GetItemSeries() != series)
            return false;

        if (useGrade && item.GetGrade() != grade)
            return false;

        return MatchesName(item.GetSearchData());
    }

    public bool MatchesName(DefaultData data)
    {
        if (string.IsNullOrEmpty(query))
            return true;

        if (data == null)
            return false;

        string name = DefinedDataUtility.GetName(data, LanguageManager.CurrentLanguage, data.name);
        return name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public bool IsMasked(ISearchable data)
    {
        if (data == null)
            return true;

        if (IsCategoryMasked(data.GetDataType()))
            return true;

        if (IsSeriesMasked(data.GetItemSeries()))
            return true;

        if (IsGradeMasked(data.GetGrade()))
            return true;

        return false;
    }

    public bool IsCategoryMasked(DataType value)
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
        DataType[] categoryMask,
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
        query = string.Empty;
        useCategory = false;
        useSeries = false;
        useGrade = false;
    }
}