using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Library")]
    public AudioLibrary audioLibrary;

    [Header("Mixer")]
    public AudioMixer audioMixer;
    public AudioMixerGroup bgmMixerGroup;
    public AudioMixerGroup sfxMixerGroup;

    [Header("Sources")]
    public AudioSource bgmSource;
    public Transform sfxSourceParent;

    [Header("SFX Pool")]
    public int initialSfxSourceCount = 10;
    public bool expandSfxPool = true;

    [Header("SFX Category Limit")]
    public bool useSfxCategoryLimit = true;
    [Min(1)] public int defaultCategoryMaxSimultaneous = 10;
    public List<SfxCategoryLimit> sfxCategoryLimits = new List<SfxCategoryLimit>();

    [Header("SFX Same Clip Limit")]
    public bool preventSameSfxOverlap = false;
    [Min(0f)] public float sameSfxMinInterval = 0.05f;

    [Header("BGM")]
    public float bgmFadeDuration = 0.5f;

    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private readonly List<AudioSource> sfxSources = new List<AudioSource>();

    private readonly Dictionary<string, AudioClip> clipCache =
        new Dictionary<string, AudioClip>();

    private readonly Dictionary<string, List<AudioClip>> categoryClipCache =
        new Dictionary<string, List<AudioClip>>();

    private readonly Dictionary<AudioSource, string> sfxSourceCategories =
        new Dictionary<AudioSource, string>();

    private readonly Dictionary<AudioClip, float> lastSfxPlayTimes =
        new Dictionary<AudioClip, float>();

    private Coroutine bgmFadeCoroutine;

    private const string MASTER_VOLUME_PARAM = "MasterVolume";
    private const string BGM_VOLUME_PARAM = "BgmVolume";
    private const string SFX_VOLUME_PARAM = "SfxVolume";

    private const string DEFAULT_CATEGORY = "Default";

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        SetupBgmSource();
        BuildCache();
        CreateSfxPool();
        ApplyMixerVolume();
    }

    private void SetupBgmSource()
    {
        if (bgmSource == null)
            bgmSource = gameObject.AddComponent<AudioSource>();

        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;
        bgmSource.outputAudioMixerGroup = bgmMixerGroup;
    }

    private void BuildCache()
    {
        clipCache.Clear();
        categoryClipCache.Clear();

        if (audioLibrary == null)
        {
            Debug.LogWarning("AudioLibrary가 없습니다.");
            return;
        }

        if (audioLibrary.categories == null)
            return;

        for (int i = 0; i < audioLibrary.categories.Count; i++)
        {
            AudioCategoryGroup group = audioLibrary.categories[i];

            if (group == null)
                continue;

            string normalizedCategory = AudioLibrary.NormalizeKey(group.categoryName);

            if (!categoryClipCache.ContainsKey(normalizedCategory))
                categoryClipCache[normalizedCategory] = new List<AudioClip>();

            if (group.clips == null)
                continue;

            for (int j = 0; j < group.clips.Count; j++)
            {
                AudioClipEntry entry = group.clips[j];

                if (entry == null || entry.clip == null)
                    continue;

                string key = MakeKey(group.categoryName, entry.soundName);

                if (clipCache.ContainsKey(key))
                {
                    Debug.LogWarning($"중복 오디오 키가 있습니다: {group.categoryName}/{entry.soundName}");
                    continue;
                }

                clipCache.Add(key, entry.clip);
                categoryClipCache[normalizedCategory].Add(entry.clip);
            }
        }
    }
    private void CreateSfxPool()
    {
        sfxSources.Clear();
        sfxSourceCategories.Clear();

        for (int i = 0; i < initialSfxSourceCount; i++)
        {
            AudioSource source = CreateSfxSource();
            sfxSources.Add(source);
        }
    }

    private AudioSource CreateSfxSource()
    {
        GameObject obj = new GameObject("SfxSource");
        obj.transform.SetParent(sfxSourceParent != null ? sfxSourceParent : transform);

        AudioSource source = obj.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = sfxMixerGroup;

        return source;
    }

    #region Volume

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        ApplyMixerVolume();
    }

    public void SetBgmVolume(float value)
    {
        bgmVolume = Mathf.Clamp01(value);
        ApplyMixerVolume();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        ApplyMixerVolume();
    }

    private void ApplyMixerVolume()
    {
        if (audioMixer == null)
            return;

        audioMixer.SetFloat(MASTER_VOLUME_PARAM, ConvertVolumeToDb(masterVolume));
        audioMixer.SetFloat(BGM_VOLUME_PARAM, ConvertVolumeToDb(bgmVolume));
        audioMixer.SetFloat(SFX_VOLUME_PARAM, ConvertVolumeToDb(sfxVolume));
    }

    private float ConvertVolumeToDb(float volume)
    {
        if (volume <= 0.0001f)
            return -80f;

        return Mathf.Log10(volume) * 20f;
    }

    #endregion

    #region SFX

    public void PlaySfx(string category, string soundName)
    {
        PlaySfx(category, soundName, 1f);
    }

    public void PlaySfx(string category)
    {
        AudioClip clip = GetRandomClipByCategory(category);

        if (clip == null)
            return;

        PlaySfx(category, clip, 1f);
    }

    public void PlaySfx(string category, string soundName, float volumeScale)
    {
        AudioClip clip = GetClip(category, soundName);

        if (clip == null)
            return;

        PlaySfx(category, clip, volumeScale);
    }

    public void PlaySfx(AudioClip clip)
    {
        PlaySfx(DEFAULT_CATEGORY, clip, 1f);
    }

    public void PlaySfx(AudioClip clip, float volumeScale)
    {
        PlaySfx(DEFAULT_CATEGORY, clip, volumeScale);
    }

    public void PlaySfx(string category, AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null)
            return;

        string normalizedCategory = AudioLibrary.NormalizeKey(category);

        if (string.IsNullOrEmpty(normalizedCategory))
            normalizedCategory = AudioLibrary.NormalizeKey(DEFAULT_CATEGORY);

        if (!CanPlaySfxCategory(normalizedCategory))
            return;

        if (!CanPlaySameClip(clip))
            return;

        AudioSource source = GetAvailableSfxSource();

        if (source == null)
            return;

        sfxSourceCategories[source] = normalizedCategory;

        source.PlayOneShot(clip, Mathf.Clamp01(volumeScale));

        if (preventSameSfxOverlap)
            lastSfxPlayTimes[clip] = Time.unscaledTime;
    }

    private bool CanPlaySfxCategory(string normalizedCategory)
    {
        if (!useSfxCategoryLimit)
            return true;

        int currentCount = GetPlayingSfxCountByCategory(normalizedCategory);
        int maxCount = GetMaxSfxCountByCategory(normalizedCategory);

        return currentCount < maxCount;
    }

    private int GetPlayingSfxCountByCategory(string normalizedCategory)
    {
        int count = 0;

        for (int i = 0; i < sfxSources.Count; i++)
        {
            AudioSource source = sfxSources[i];

            if (source == null)
                continue;

            if (!source.isPlaying)
                continue;

            if (!sfxSourceCategories.TryGetValue(source, out string sourceCategory))
                continue;

            if (sourceCategory == normalizedCategory)
                count++;
        }

        return count;
    }

    private int GetMaxSfxCountByCategory(string normalizedCategory)
    {
        for (int i = 0; i < sfxCategoryLimits.Count; i++)
        {
            SfxCategoryLimit limit = sfxCategoryLimits[i];

            if (limit == null)
                continue;

            string limitCategory = AudioLibrary.NormalizeKey(limit.category);

            if (limitCategory == normalizedCategory)
                return Mathf.Max(1, limit.maxSimultaneous);
        }

        return Mathf.Max(1, defaultCategoryMaxSimultaneous);
    }

    private bool CanPlaySameClip(AudioClip clip)
    {
        if (!preventSameSfxOverlap)
            return true;

        if (clip == null)
            return false;

        if (!lastSfxPlayTimes.TryGetValue(clip, out float lastPlayTime))
            return true;

        return Time.unscaledTime - lastPlayTime >= sameSfxMinInterval;
    }

    private AudioSource GetAvailableSfxSource()
    {
        for (int i = 0; i < sfxSources.Count; i++)
        {
            AudioSource source = sfxSources[i];

            if (source != null && !source.isPlaying)
                return source;
        }

        if (!expandSfxPool)
            return null;

        AudioSource newSource = CreateSfxSource();
        sfxSources.Add(newSource);

        return newSource;
    }

    private AudioClip GetRandomClipByCategory(string category)
    {
        string normalizedCategory = AudioLibrary.NormalizeKey(category);

        if (!categoryClipCache.TryGetValue(normalizedCategory, out List<AudioClip> matchedClips))
        {
            Debug.LogWarning($"해당 카테고리의 오디오 클립이 없습니다: {category}");
            return null;
        }

        if (matchedClips == null || matchedClips.Count == 0)
        {
            Debug.LogWarning($"해당 카테고리의 오디오 클립이 없습니다: {category}");
            return null;
        }

        int randomIndex = UnityEngine.Random.Range(0, matchedClips.Count);
        return matchedClips[randomIndex];
    }

    public void StopAllSfx()
    {
        for (int i = 0; i < sfxSources.Count; i++)
        {
            AudioSource source = sfxSources[i];

            if (source == null)
                continue;

            source.Stop();

            if (sfxSourceCategories.ContainsKey(source))
                sfxSourceCategories[source] = string.Empty;
        }
    }

    #endregion

    #region BGM

    public void PlayBgm(string soundName)
    {
        string category = "Bgm";
        AudioClip clip = GetClip(category, soundName);

        if (clip == null)
            return;

        PlayBgm(clip);
    }

    public void PlayBgm(AudioClip clip)
    {
        if (clip == null || bgmSource == null)
            return;

        if (bgmSource.clip == clip && bgmSource.isPlaying)
            return;

        if (bgmFadeCoroutine != null)
            StopCoroutine(bgmFadeCoroutine);

        bgmFadeCoroutine = StartCoroutine(ChangeBgmRoutine(clip));
    }

    public void StopBgm()
    {
        if (bgmSource == null)
            return;

        if (bgmFadeCoroutine != null)
            StopCoroutine(bgmFadeCoroutine);

        bgmFadeCoroutine = StartCoroutine(StopBgmRoutine());
    }

    public void PauseBgm()
    {
        if (bgmSource == null)
            return;

        bgmSource.Pause();
    }

    public void ResumeBgm()
    {
        if (bgmSource == null)
            return;

        bgmSource.UnPause();
    }

    private IEnumerator ChangeBgmRoutine(AudioClip nextClip)
    {
        float targetVolume = 1f;

        if (bgmSource.isPlaying)
        {
            float startVolume = bgmSource.volume;
            float timer = 0f;

            while (timer < bgmFadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, timer / bgmFadeDuration);
                yield return null;
            }
        }

        bgmSource.Stop();
        bgmSource.clip = nextClip;
        bgmSource.loop = true;
        bgmSource.volume = 0f;
        bgmSource.Play();

        float fadeInTimer = 0f;

        while (fadeInTimer < bgmFadeDuration)
        {
            fadeInTimer += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(0f, targetVolume, fadeInTimer / bgmFadeDuration);
            yield return null;
        }

        bgmSource.volume = targetVolume;
        bgmFadeCoroutine = null;
    }

    private IEnumerator StopBgmRoutine()
    {
        float startVolume = bgmSource.volume;
        float timer = 0f;

        while (timer < bgmFadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, timer / bgmFadeDuration);
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.clip = null;
        bgmSource.volume = 1f;
        bgmFadeCoroutine = null;
    }

    #endregion

    #region Common

    public void StopAllAudio()
    {
        StopBgm();
        StopAllSfx();
    }

    private AudioClip GetClip(string category, string soundName)
    {
        string key = MakeKey(category, soundName);

        if (clipCache.TryGetValue(key, out AudioClip clip))
            return clip;

        Debug.LogWarning($"오디오 클립을 찾을 수 없습니다: {category}/{soundName}");
        return null;
    }

    private string MakeKey(string category, string soundName)
    {
        return $"{AudioLibrary.NormalizeKey(category)}/{AudioLibrary.NormalizeKey(soundName)}";
    }

    #endregion
}

[Serializable]
public class SfxCategoryLimit
{
    public string category;
    [Min(1)] public int maxSimultaneous = 5;
}