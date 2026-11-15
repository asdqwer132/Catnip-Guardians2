public static class InventorySearchFilterUtil
{
    public static void Copy(InventorySearchFilter from, InventorySearchFilter to)
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