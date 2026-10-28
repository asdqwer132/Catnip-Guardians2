using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class LocalizedTextValue
{
    public language language;
    [TextArea] public string text;
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
    public LocalizedTextEntry[] entries = Array.Empty<LocalizedTextEntry>();
    private Dictionary<string, Dictionary<language, string>> lookup;
    private void OnEnable() => lookup = null;
    private void OnValidate() => lookup = null;
    public void Rebuild()
    {
        lookup = new Dictionary<string, Dictionary<language, string>>(StringComparer.Ordinal);
        if (entries == null) return;
        foreach (var entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.key) || entry.translations == null) continue;
            if (!lookup.TryGetValue(entry.key, out var values)) lookup.Add(entry.key, values = new Dictionary<language, string>());
            foreach (var value in entry.translations)
                if (value != null && !string.IsNullOrEmpty(value.text)) values[value.language] = value.text;
        }
    }
    public bool TryGetText(string key, language selected, out string text)
    {
        text = null;
        if (string.IsNullOrEmpty(key)) return false;
        if (lookup == null) Rebuild();
        return lookup.TryGetValue(key, out var values) && values.TryGetValue(selected, out text);
    }
}
