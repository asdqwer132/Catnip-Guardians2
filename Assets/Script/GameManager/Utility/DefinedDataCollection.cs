using System;
using System.Collections.Generic;
using UnityEngine;

// 각 타입의 표시 정보를 같은 에셋 안에 직렬화합니다. 항목별 ScriptableObject는 만들지 않습니다.
[Serializable]
public sealed class DefinedDataEntry<TType> : DefinedData
{
    public TType type;
    public Sprite icon;
    public Description[] data = Array.Empty<Description>();
    public Sprite Icon => icon;
    private Description Read(language selected)
    {
        if (data != null) foreach (var value in data)
            if (value != null && value.language == selected) return value;
        return null;
    }
    public string GetDataName(language selected) => Read(selected)?.dataName;
    public string GetDescription(language selected) => Read(selected)?.description;
    public string GetDetailDescription(language selected) => Read(selected)?.detailDescription;
}

public abstract class DefinedDataCollection<TType> : ScriptableObject, IDefinedDataProvider<TType> where TType : struct, Enum
{
    public List<DefinedDataEntry<TType>> entries = new List<DefinedDataEntry<TType>>();
    public DefinedData GetData(TType type) => GetEntry(type);
    public DefinedDataEntry<TType> GetEntry(TType type)
    {
        if (entries != null) foreach (var entry in entries)
            if (entry != null && EqualityComparer<TType>.Default.Equals(entry.type, type)) return entry;
        return null;
    }
}

public static class DefinedDataUtility
{
    public static string GetName(DefinedData data, language selected, string fallback = "")
    {
        string name = data?.GetDataName(selected);
        if (string.IsNullOrEmpty(name)) name = data?.GetDataName(language.english);
        return string.IsNullOrEmpty(name) ? fallback : name;
    }

    public static string GetDescription(DefinedData data, language selected)
    {
        string value = data?.GetDescription(selected);
        if (string.IsNullOrEmpty(value)) value = data?.GetDescription(language.english);
        return value ?? "";
    }

    public static string GetDetailDescription(DefinedData data, language selected)
    {
        string value = data?.GetDetailDescription(selected);
        if (string.IsNullOrEmpty(value)) value = data?.GetDetailDescription(language.english);
        return value ?? "";
    }
}
