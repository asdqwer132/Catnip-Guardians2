using System;
using System.Collections.Generic;
using UnityEngine;

public enum language
{
    english,
    korean
}

[Serializable]
public class LanguageData
{
    public language language;
    public string dataName;

    [TextArea]
    public string description;
}

public class DefaultData : ScriptableObject, IUnlockable
{
    [Header("Basic Info")]
    public Sprite icon;
    public LanguageData[] data;

    [Header("Id Info")]
    public string dataId;
    public DataType dataType;
    public bool requireUnlock = false;

    public bool RequireUnlock => requireUnlock;
    public DataType UnlockType => dataType;
    public string UnlockId => dataId;

    private Dictionary<language, LanguageData> languageDataMap;
    public string GetDataName() => GetDataName(LanguageManager.instance.selectedLan);
    public string GetDataName(language targetLanguage)
    {
        LanguageData languageData = GetLanguageData(targetLanguage);

        if (languageData == null)
            return null;

        return languageData.dataName;
    }
    public string GetDescription() => GetDescription(LanguageManager.instance.selectedLan);

    public string GetDescription(language targetLanguage)
    {
        LanguageData languageData = GetLanguageData(targetLanguage);

        if (languageData == null)
            return null;

        return languageData.description;
    }

    public LanguageData GetLanguageData(language targetLanguage)
    {
        EnsureLanguageDataMap();

        if (languageDataMap.TryGetValue(targetLanguage, out LanguageData languageData))
            return languageData;

        return null;
    }

    private void EnsureLanguageDataMap()
    {
        if (languageDataMap != null)
            return;

        languageDataMap = new Dictionary<language, LanguageData>();

        if (data == null)
            return;

        for (int i = 0; i < data.Length; i++)
        {
            LanguageData languageData = data[i];

            if (languageData == null)
                continue;

            languageDataMap[languageData.language] = languageData;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(dataId))
            dataId = name;

        languageDataMap = null;
    }
#endif
}