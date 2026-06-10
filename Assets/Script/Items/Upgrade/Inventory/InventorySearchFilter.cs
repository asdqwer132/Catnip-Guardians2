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

    public bool IsMatch(InventoryItem item)
    {
        if (item == null || item.itemData == null)
            return false;

        ItemData data = item.itemData;

        if (useCategory && data.category != category)
            return false;

        if (useSeries && data.series != series)
            return false;

        if (useGrade && data.grade != grade)
            return false;

        return true;
    }

    public void Clear()
    {
        useCategory = false;
        useSeries = false;
        useGrade = false;
    }
}