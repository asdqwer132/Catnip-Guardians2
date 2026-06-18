using UnityEngine;

public interface ISearchable
{
    public ItemGrade GetGrade();
    public ItemSeries GetItemSeries();
    public ItemCategory GetItemCategory();
}
