using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public sealed class LocalizedTextValue
{
    public language language;
    public string text;
}
[Serializable]
public sealed class LocalizedTextEntry
{
    public string key;
    public LocalizedTextValue[] translations = Array.Empty<LocalizedTextValue>();
}

[CreateAssetMenu(fileName = "LocalizedTextTable", menuName = "Settings/Language/Text Table")]
public sealed class LocalizedTextTable : ScriptableObject
{
    public string id;
    [Tooltip("공통 번역 묶음입니다. 같은 키는 씬 자체 항목이 우선하고, 묶음끼리는 배열 앞쪽부터 확인합니다.")]
    public LocalizedTextGroup[] sharedGroups = Array.Empty<LocalizedTextGroup>();
    public LocalizedTextEntry[] entries = Array.Empty<LocalizedTextEntry>();
    private Dictionary<string, Dictionary<language, string>> lookup;
    private void OnEnable() => lookup = null;
    public void Rebuild() => lookup = BuildLookup(entries);
    private void OnValidate()
    {
        lookup = null;
        if (string.IsNullOrEmpty(id))
            id = name;
    }
    internal static Dictionary<string, Dictionary<language, string>> BuildLookup(LocalizedTextEntry[] entries)
    {
        var lookup = new Dictionary<string, Dictionary<language, string>>(StringComparer.Ordinal);
        if (entries == null) return lookup;
        foreach (var entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.key) || entry.translations == null) continue;
            if (!lookup.TryGetValue(entry.key, out var values)) lookup.Add(entry.key, values = new Dictionary<language, string>());
            foreach (var value in entry.translations)
                if (value != null && !string.IsNullOrEmpty(value.text)) values[value.language] = value.text;
        }
        return lookup;
    }

    public bool TryGetText(string key, language selected, out string text)
    {
        text = null;
        if (string.IsNullOrEmpty(key)) return false;
        if (lookup == null) Rebuild();
        if (lookup.TryGetValue(key, out var values) && values.TryGetValue(selected, out text))
            return true;

        if (sharedGroups != null)
            foreach (var group in sharedGroups)
                if (group != null && group.TryGetText(key, selected, out text))
                    return true;

        return false;
    }
}
