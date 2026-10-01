using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class Description
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
    public Description[] data;

    [Header("Id Info")]
    public string dataNumbering;
    public string dataId;
    public DataType dataType;
    public bool requireUnlock = false;


    public bool RequireUnlock => requireUnlock;
    public DataType UnlockType => dataType;
    public string UnlockId => dataId;

    private Dictionary<language, Description> languageDataMap;

    public string GetDataName() => GetDataName(LanguageManager.instance.selectedLan);

    public string GetDataName(language targetLanguage)
    {
        Description languageData = GetLanguageData(targetLanguage);

        if (languageData == null)
            return null;

        return languageData.dataName;
    }

    public string GetDescription() => GetDescription(LanguageManager.instance.selectedLan);

    public string GetDescription(language targetLanguage)
    {
        Description languageData = GetLanguageData(targetLanguage);

        if (languageData == null)
            return null;

        return languageData.description;
    }

    public Description GetLanguageData(language targetLanguage)
    {
        EnsureLanguageDataMap();

        if (languageDataMap.TryGetValue(targetLanguage, out Description languageData))
            return languageData;

        return null;
    }

    private void EnsureLanguageDataMap()
    {
        if (languageDataMap != null)
            return;

        languageDataMap = new Dictionary<language, Description>();

        if (data == null)
            return;

        for (int i = 0; i < data.Length; i++)
        {
            Description languageData = data[i];

            if (languageData == null)
                continue;

            languageDataMap[languageData.language] = languageData;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(dataId))
        {
            string text = dataNumbering + name;
            text = text.Replace(" ", "");
            dataId = text;
        }

        languageDataMap = null;
    }
#endif
}