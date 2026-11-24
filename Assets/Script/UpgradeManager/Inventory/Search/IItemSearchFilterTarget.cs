public interface IItemSearchFilterTarget
{
    InventorySearchFilter GetSearchFilter();
    void SetSearchFilter(InventorySearchFilter filter);
    void ClearSearchFilter();
}