using System;
using UnityEngine;
using UnityEngine.Video;

public enum TutorialMediaType { Sprite, Video }

[Serializable]
public sealed class TutorialPageData : DefinedData
{
    public Description[] data = Array.Empty<Description>();
    public TutorialMediaType mediaType;
    public Sprite sprite;
    public VideoClip video;
    public bool loopVideo = true;
    public Sprite Icon => sprite;
    private Description Read(language selected)
    {
        if (data != null) foreach (var entry in data)
            if (entry != null && entry.language == selected) return entry;
        return null;
    }
    public string GetDataName(language selected) => Read(selected)?.dataName;
    public string GetDescription(language selected) => Read(selected)?.description;
    public string GetDetailDescription(language selected) => Read(selected)?.detailDescription;
}

[CreateAssetMenu(fileName = "TutorialData", menuName = "Settings/Tutorial Data")]
public sealed class TutorialData : DefaultData
{
    public TutorialPageData[] pages = Array.Empty<TutorialPageData>();
    public bool HasPages
    {
        get
        {
            if (pages == null || pages.Length == 0) return false;
            foreach (var page in pages) if (page == null) return false;
            return true;
        }
    }
}
