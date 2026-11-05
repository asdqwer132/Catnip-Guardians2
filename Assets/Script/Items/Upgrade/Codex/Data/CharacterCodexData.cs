using System;
using System.Collections.Generic;
using UnityEngine;

public enum CharacterPortraitState { Idle, Attack, Emotion, FullBody, Custom }

[Serializable]
public sealed class CharacterCodexPortrait : DefinedData
{
    public CharacterPortraitState state;
    [Tooltip("Stable identifier for this pose or expression. Multiple entries can use the same state.")]
    public string stateId;
    public Sprite sprite;
    public Description[] data = Array.Empty<Description>();
    public Sprite Icon => sprite;

    private Description Read(language selected)
    {
        Description result = null;
        if (data != null)
            foreach (var entry in data)
                if (entry != null && entry.language == selected) result = entry;
        return result;
    }
    public string GetDataName() => GetDataName(LanguageManager.CurrentLanguage);
    public string GetDescription() => GetDescription(LanguageManager.CurrentLanguage);
    public string GetDetailDescription() => GetDetailDescription(LanguageManager.CurrentLanguage);
    public string GetDataName(language selected) => Read(selected)?.dataName;
    public string GetDescription(language selected) => Read(selected)?.description;
    public string GetDetailDescription(language selected) => Read(selected)?.detailDescription;
}

[CreateAssetMenu(fileName = "Character Codex", menuName = "03.Upgrade/Codex/Character")]
public sealed class CharacterCodexData : DefaultData
{
    [Header("Extra Data")]
    [Tooltip("Each sprite and its localized information are stored in one entry; pose and text cannot drift between parallel arrays.")]
    public List<CharacterCodexPortrait> portraits = new List<CharacterCodexPortrait>();

    public CharacterCodexPortrait GetPortrait(int index)
        => portraits != null && index >= 0 && index < portraits.Count ? portraits[index] : null;

    public CharacterCodexPortrait GetPortrait(string stateId)
    {
        if (portraits == null || string.IsNullOrEmpty(stateId)) return null;
        return portraits.Find(p => p != null && string.Equals(p.stateId, stateId, StringComparison.Ordinal));
    }
}
