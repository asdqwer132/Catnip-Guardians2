using UnityEngine;

public interface ISearchable
{
    public DefaultData GetSearchData();
    public Grade GetGrade();
    public ItemSeries GetItemSeries();
    public DataType GetDataType();
}
