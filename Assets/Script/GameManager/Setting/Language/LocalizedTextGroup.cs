using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LocalizedTextGroup", menuName = "Settings/Language/Shared Text Group")]
public sealed class LocalizedTextGroup : ScriptableObject
{
    public string id;
    public LocalizedTextEntry[] entries = Array.Empty<LocalizedTextEntry>();
    private Dictionary<string, Dictionary<language, string>> lookup;
    private void OnValidate()
    {
        lookup = null;
        if (string.IsNullOrEmpty(id))
            id = name;
    }
    private void OnEnable() => lookup = null;

    internal bool TryGetText(string key, language selected, out string text)
    {
        lookup ??= LocalizedTextTable.BuildLookup(entries);
        text = null;
        return lookup.TryGetValue(key, out var values) && values.TryGetValue(selected, out text);
    }
}
