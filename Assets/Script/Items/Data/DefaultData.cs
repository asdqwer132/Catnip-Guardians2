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

#if UNITY_EDITOR
    [Header("Editor Icon Search")]
    [SerializeField] private string iconSearchFolderPath = "Assets/Art/Icons/Items";
    [SerializeField] private bool findIconOnRebuild = true;
#endif

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

    [ContextMenu("Rebuild ID by Name")]
    public void Rebuild()
    {
        dataId = name;
        languageDataMap = null;

#if UNITY_EDITOR
        if (findIconOnRebuild)
            FindAndSetIconById();

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
#endif
    }

#if UNITY_EDITOR
    [ContextMenu("Find Icon By ID")]
    private void FindAndSetIconById()
    {
        Sprite foundIcon = FindSpriteById(iconSearchFolderPath, dataId);

        if (foundIcon == null)
        {
            Debug.LogWarning(
                $"Icon not found. Data: {name}, ID: {dataId}, Normalized ID: {NormalizeKey(dataId)}, Folder: {iconSearchFolderPath}",
                this);

            return;
        }

        icon = foundIcon;

        EditorUtility.SetDirty(this);

        Debug.Log(
            $"Icon set. Data: {name}, ID: {dataId}, Sprite: {foundIcon.name}",
            this);
    }

    private static Sprite FindSpriteById(string folderPath, string targetId)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return null;

        if (string.IsNullOrWhiteSpace(targetId))
            return null;

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogWarning($"Icon folder not found: {folderPath}");
            return null;
        }

        string normalizedTargetId = NormalizeKey(targetId);

        // Sprite Mode = Multiple인 스프라이트 시트는 Texture2D 안에 Sub Sprite로 들어있음
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });

        for (int i = 0; i < textureGuids.Length; i++)
        {
            string texturePath = AssetDatabase.GUIDToAssetPath(textureGuids[i]);

            Sprite sprite = FindSpriteInAssetPath(texturePath, normalizedTargetId);

            if (sprite != null)
                return sprite;
        }

        // 혹시 독립 Sprite 에셋이 섞여 있을 경우 대비
        string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });

        for (int i = 0; i < spriteGuids.Length; i++)
        {
            string spritePath = AssetDatabase.GUIDToAssetPath(spriteGuids[i]);

            Sprite sprite = FindSpriteInAssetPath(spritePath, normalizedTargetId);

            if (sprite != null)
                return sprite;
        }

        return null;
    }

    private static Sprite FindSpriteInAssetPath(string assetPath, string normalizedTargetId)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
            return null;

        // Multiple Sprite Sheet의 잘린 스프라이트들은 여기에 들어옴
        UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);

        for (int i = 0; i < subAssets.Length; i++)
        {
            Sprite sprite = subAssets[i] as Sprite;

            if (sprite == null)
                continue;

            if (NormalizeKey(sprite.name) == normalizedTargetId)
                return sprite;
        }

        // Single Sprite 또는 메인 에셋 쪽에 잡히는 경우 대비
        UnityEngine.Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

        for (int i = 0; i < allAssets.Length; i++)
        {
            Sprite sprite = allAssets[i] as Sprite;

            if (sprite == null)
                continue;

            if (NormalizeKey(sprite.name) == normalizedTargetId)
                return sprite;
        }

        return null;
    }

    private static string NormalizeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        string trimmed = value.Trim().ToLowerInvariant();

        // 대소문자 무시
        // 띄어쓰기 무시
        // 언더바 무시
        // 하이픈 무시
        return Regex.Replace(trimmed, @"[\s_\-]+", "");
    }

    [ContextMenu("Debug Print Sprites In Icon Folder")]
    private void DebugPrintSpritesInIconFolder()
    {
        if (string.IsNullOrWhiteSpace(iconSearchFolderPath))
        {
            Debug.LogWarning("Icon search folder path is empty.", this);
            return;
        }

        if (!AssetDatabase.IsValidFolder(iconSearchFolderPath))
        {
            Debug.LogWarning($"Icon folder not found: {iconSearchFolderPath}", this);
            return;
        }

        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { iconSearchFolderPath });

        Debug.Log($"---- Sprite Debug Start / Folder: {iconSearchFolderPath} / Texture Count: {textureGuids.Length} ----", this);

        for (int i = 0; i < textureGuids.Length; i++)
        {
            string texturePath = AssetDatabase.GUIDToAssetPath(textureGuids[i]);

            Debug.Log($"Texture: {texturePath}", this);

            UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(texturePath);

            for (int j = 0; j < subAssets.Length; j++)
            {
                Sprite sprite = subAssets[j] as Sprite;

                if (sprite == null)
                    continue;

                Debug.Log($"Sub Sprite: {sprite.name} / Normalized: {NormalizeKey(sprite.name)}", sprite);
            }

            UnityEngine.Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(texturePath);

            for (int j = 0; j < allAssets.Length; j++)
            {
                Sprite sprite = allAssets[j] as Sprite;

                if (sprite == null)
                    continue;

                Debug.Log($"All Asset Sprite: {sprite.name} / Normalized: {NormalizeKey(sprite.name)}", sprite);
            }
        }

        Debug.Log($"---- Sprite Debug End / Target ID: {dataId} / Normalized Target: {NormalizeKey(dataId)} ----", this);
    }

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