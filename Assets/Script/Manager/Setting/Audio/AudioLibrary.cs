using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "AudioLibrary", menuName = "GameData/Audio Library")]
public class AudioLibrary : ScriptableObject
{
#if UNITY_EDITOR
    [Header("Source Folder")]
    [Tooltip("여기에 Assets 안의 오디오 루트 폴더를 드래그하세요.")]
    public DefaultAsset sourceFolder;
#endif

    [Header("Scan Root Path")]
    [SerializeField] private string rootAssetFolder = "Assets/Audio";

    [Header("Categories")]
    public List<AudioCategoryGroup> categories = new List<AudioCategoryGroup>();

    public string RootAssetFolder => rootAssetFolder;

    public AudioClip GetClip(string category, string soundName)
    {
        string keyCategory = NormalizeKey(category);
        string keyName = NormalizeKey(soundName);

        for (int i = 0; i < categories.Count; i++)
        {
            AudioCategoryGroup group = categories[i];

            if (group == null)
                continue;

            if (NormalizeKey(group.categoryName) != keyCategory)
                continue;

            if (group.clips == null)
                continue;

            for (int j = 0; j < group.clips.Count; j++)
            {
                AudioClipEntry entry = group.clips[j];

                if (entry == null)
                    continue;

                if (NormalizeKey(entry.soundName) == keyName)
                    return entry.clip;
            }
        }

        return null;
    }

    public AudioClip GetRandomClipByCategory(string category)
    {
        AudioCategoryGroup group = GetCategoryGroup(category);

        if (group == null || group.clips == null || group.clips.Count == 0)
            return null;

        List<AudioClip> validClips = new List<AudioClip>();

        for (int i = 0; i < group.clips.Count; i++)
        {
            AudioClipEntry entry = group.clips[i];

            if (entry == null || entry.clip == null)
                continue;

            validClips.Add(entry.clip);
        }

        if (validClips.Count == 0)
            return null;

        int randomIndex = UnityEngine.Random.Range(0, validClips.Count);
        return validClips[randomIndex];
    }

    public AudioCategoryGroup GetCategoryGroup(string category)
    {
        string keyCategory = NormalizeKey(category);

        for (int i = 0; i < categories.Count; i++)
        {
            AudioCategoryGroup group = categories[i];

            if (group == null)
                continue;

            if (NormalizeKey(group.categoryName) == keyCategory)
                return group;
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

    private void OnValidate()
    {
        UpdateRootPathFromSourceFolder();
    }

    private void UpdateRootPathFromSourceFolder()
    {
        if (sourceFolder == null)
            return;

        string path = AssetDatabase.GetAssetPath(sourceFolder);

        if (string.IsNullOrEmpty(path))
            return;

        if (!AssetDatabase.IsValidFolder(path))
        {
            Debug.LogWarning($"선택한 에셋은 폴더가 아닙니다: {path}", this);
            return;
        }

        rootAssetFolder = path;
    }

    [ContextMenu("Rebuild Audio Library From Source Folder")]
    public void Rebuild()
    {
        UpdateRootPathFromSourceFolder();

        categories.Clear();

        if (string.IsNullOrEmpty(rootAssetFolder))
        {
            Debug.LogWarning("오디오 루트 폴더가 비어있습니다.", this);
            return;
        }

        if (!AssetDatabase.IsValidFolder(rootAssetFolder))
        {
            Debug.LogWarning($"오디오 루트 폴더가 없습니다: {rootAssetFolder}", this);
            return;
        }

        string[] subFolders = AssetDatabase.GetSubFolders(rootAssetFolder);

        for (int i = 0; i < subFolders.Length; i++)
        {
            string categoryFolderPath = subFolders[i];
            string categoryName = GetFolderName(categoryFolderPath);

            AudioCategoryGroup group = new AudioCategoryGroup
            {
                categoryName = categoryName,
                clips = new List<AudioClipEntry>()
            };

            AddClipsFromFolder(group, categoryFolderPath);

            if (group.clips.Count > 0)
                categories.Add(group);
        }

        SortCategoriesAndClips();

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();

        Debug.Log($"AudioLibrary 재빌드 완료. Root: {rootAssetFolder}, 카테고리 수: {categories.Count}", this);
    }

    private void AddClipsFromFolder(AudioCategoryGroup group, string folderPath)
    {
        if (group == null)
            return;

        if (string.IsNullOrEmpty(folderPath))
            return;

        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folderPath });

        for (int i = 0; i < guids.Length; i++)
        {
            string clipPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);

            if (clip == null)
                continue;

            if (ContainsClip(group, clip))
                continue;

            group.clips.Add(new AudioClipEntry
            {
                soundName = clip.name,
                clip = clip
            });
        }
    }

    private bool ContainsClip(AudioCategoryGroup group, AudioClip clip)
    {
        if (group == null || group.clips == null || clip == null)
            return false;

        for (int i = 0; i < group.clips.Count; i++)
        {
            AudioClipEntry entry = group.clips[i];

            if (entry == null)
                continue;

            if (entry.clip == clip)
                return true;
        }

        return false;
    }

    private void SortCategoriesAndClips()
    {
        categories.Sort((a, b) =>
        {
            string aName = a != null ? a.categoryName : string.Empty;
            string bName = b != null ? b.categoryName : string.Empty;

            return string.Compare(aName, bName, StringComparison.OrdinalIgnoreCase);
        });

        for (int i = 0; i < categories.Count; i++)
        {
            AudioCategoryGroup group = categories[i];

            if (group == null || group.clips == null)
                continue;

            group.clips.Sort((a, b) =>
            {
                string aName = a != null ? a.soundName : string.Empty;
                string bName = b != null ? b.soundName : string.Empty;

                return string.Compare(aName, bName, StringComparison.OrdinalIgnoreCase);
            });
        }
    }

    private string GetFolderName(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "Default";

        string fixedPath = path.Replace("\\", "/").TrimEnd('/');
        int lastSlashIndex = fixedPath.LastIndexOf('/');

        if (lastSlashIndex < 0)
            return fixedPath;

        return fixedPath.Substring(lastSlashIndex + 1);
    }

#endif
}

[Serializable]
public class AudioClipNameWithCategory
{
    public string categoryName;
    public string clipName;
}

[Serializable]
public class AudioCategoryGroup
{
    public string categoryName;
    public List<AudioClipEntry> clips = new List<AudioClipEntry>();
}

[Serializable]
public class AudioClipEntry
{
    public string soundName;
    public AudioClip clip;
}