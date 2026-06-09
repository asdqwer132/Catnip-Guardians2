using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "AudioLibrary", menuName = "Game/Audio Library")]
public class AudioLibrary : ScriptableObject
{
    [Header("Scan Root")]
    public string rootAssetFolder = "Assets/Audio";

    [Header("Clips")]
    public List<AudioEntry> clips = new List<AudioEntry>();

    public AudioClip GetClip(string category, string soundName)
    {
        string keyCategory = NormalizeKey(category);
        string keyName = NormalizeKey(soundName);

        for (int i = 0; i < clips.Count; i++)
        {
            AudioEntry entry = clips[i];

            if (entry == null)
                continue;

            if (NormalizeKey(entry.category) == keyCategory &&
                NormalizeKey(entry.soundName) == keyName)
            {
                return entry.clip;
            }
        }

        return null;
    }

    public static string NormalizeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim().ToLowerInvariant();
    }

#if UNITY_EDITOR
    [ContextMenu("Rebuild Audio Library From Root Folder")]
    public void Rebuild()
    {
        clips.Clear();

        if (!AssetDatabase.IsValidFolder(rootAssetFolder))
        {
            Debug.LogWarning($"오디오 루트 폴더가 없습니다: {rootAssetFolder}");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { rootAssetFolder });

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);

            if (clip == null)
                continue;

            string category = GetCategoryFromPath(path);
            string soundName = clip.name;

            clips.Add(new AudioEntry
            {
                category = category,
                soundName = soundName,
                clip = clip
            });
        }

        clips.Sort((a, b) =>
        {
            int categoryCompare = string.Compare(a.category, b.category, StringComparison.OrdinalIgnoreCase);

            if (categoryCompare != 0)
                return categoryCompare;

            return string.Compare(a.soundName, b.soundName, StringComparison.OrdinalIgnoreCase);
        });

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();

        Debug.Log($"AudioLibrary 재빌드 완료. 등록된 클립 수: {clips.Count}");
    }

    private string GetCategoryFromPath(string path)
    {
        string fixedRoot = rootAssetFolder.Replace("\\", "/").TrimEnd('/');
        string fixedPath = path.Replace("\\", "/");

        if (!fixedPath.StartsWith(fixedRoot))
            return "Default";

        string relativePath = fixedPath.Substring(fixedRoot.Length).TrimStart('/');
        string[] parts = relativePath.Split('/');

        if (parts.Length <= 1)
            return "Default";

        return parts[0];
    }
#endif
}

[Serializable]
public class AudioEntry
{
    public string category;
    public string soundName;
    public AudioClip clip;
}