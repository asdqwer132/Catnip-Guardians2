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

    [Header("BGM")]
    public float bgmFadeDuration = 0.5f;

    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private readonly List<AudioSource> sfxSources = new List<AudioSource>();
    private readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();

    private Coroutine bgmFadeCoroutine;

    private const string MASTER_VOLUME_PARAM = "MasterVolume";
    private const string BGM_VOLUME_PARAM = "BgmVolume";
    private const string SFX_VOLUME_PARAM = "SfxVolume";

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
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
        }

        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;
        bgmSource.outputAudioMixerGroup = bgmMixerGroup;
    }

    private void BuildCache()
    {
        clipCache.Clear();

        if (audioLibrary == null)
        {
            Debug.LogWarning("AudioLibrary가 없습니다.");
            return;
        }

        for (int i = 0; i < audioLibrary.clips.Count; i++)
        {
            AudioEntry entry = audioLibrary.clips[i];

            if (entry == null || entry.clip == null)
                continue;

            string key = MakeKey(entry.category, entry.soundName);

            if (clipCache.ContainsKey(key))
            {
                Debug.LogWarning($"중복 오디오 키가 있습니다: {entry.category}/{entry.soundName}");
                continue;
            }

            clipCache.Add(key, entry.clip);
        }
    }

    private void CreateSfxPool()
    {
        sfxSources.Clear();

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

    public void PlaySfx(string category, string soundName)
    {
        PlaySfx(category, soundName, 1f);
    }

    public void PlaySfx(string category, string soundName, float volumeScale)
    {
        AudioClip clip = GetClip(category, soundName);

        if (clip == null)
            return;

        PlaySfx(clip, volumeScale);
    }

    public void PlaySfx(AudioClip clip)
    {
        PlaySfx(clip, 1f);
    }

    public void PlaySfx(AudioClip clip, float volumeScale)
    {
        if (clip == null)
            return;

        AudioSource source = GetAvailableSfxSource();

        if (source == null)
            return;

        source.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

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

    public void StopAllSfx()
    {
        for (int i = 0; i < sfxSources.Count; i++)
        {
            if (sfxSources[i] != null)
                sfxSources[i].Stop();
        }
    }

    public void StopAllAudio()
    {
        StopBgm();
        StopAllSfx();
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

    private AudioSource GetAvailableSfxSource()
    {
        for (int i = 0; i < sfxSources.Count; i++)
        {
            if (sfxSources[i] != null && !sfxSources[i].isPlaying)
                return sfxSources[i];
        }

        if (!expandSfxPool)
            return null;

        AudioSource source = CreateSfxSource();
        sfxSources.Add(source);

        return source;
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
}