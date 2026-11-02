using UnityEngine;

public interface DefinedData
{
    Sprite Icon { get; }
    string GetDataName(language selected);
    string GetDescription(language selected);
    string GetDetailDescription(language selected);
}

public interface IDefinedDataProvider<TType>
{
    DefinedData GetData(TType type);
}
