using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 재생 소스/볼륨/믹서는 씬 소유이며, 클립 라이브러리만 공유할 수 있습니다.
[DisallowMultipleComponent]
[DefaultExecutionOrder(-8000)]
public sealed class SceneAudioChannel : MonoBehaviour
{
    public AudioLibrary library;
    public AudioSource effectsSource;
    public AudioSource musicSource;
    public string musicCategory = "Bgm";
    [Range(0f, 1f)] public float effectsVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;
    public bool muted;
    [Tooltip("코어 설정의 Master/SFX/BGM 볼륨을 곱합니다. Mixer에서 이미 적용하면 끄세요.")]
    public bool followSharedVolume = true;
    [Header("Effect Concurrency Limits")]
    [Tooltip("Maximum simultaneous effects in this scene. Extra requests are skipped; music is separate.")]
    [Min(1)] public int maxSimultaneousEffects = 16;
    [Min(1)] public int maxSimultaneousPerClip = 4;
    [Min(1)] public int defaultCategoryMaxSimultaneous = 8;
    public List<SfxCategoryLimit> categoryLimits = new List<SfxCategoryLimit>();

    private sealed class EffectVoice
    {
        public AudioSource source;
        public string category;
        public int startedFrame = -1;
        // Reserve immediately, including before the audio backend begins playback.
        public bool Active => source != null && (source.isPlaying || startedFrame == Time.frameCount);
    }
    private readonly List<EffectVoice> voices = new List<EffectVoice>();
    public int ActiveEffectCount
    {
        get { int count = 0; foreach (var voice in voices) if (voice.Active) count++; return count; }
    }
    private static readonly List<SceneAudioChannel> channels = new List<SceneAudioChannel>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetChannels() => channels.Clear();
    private void OnEnable() { if (!channels.Contains(this)) channels.Add(this); ApplyVolume(); }
    private void OnDisable()
    {
        channels.Remove(this);
        StopAllEffects();
        if (musicSource != null) musicSource.Stop();
    }
    private void LateUpdate()
    {
        EnforceLimits();
        ApplyVolume();
    }
    private void ApplyVolume()
    {
        var setting = followSharedVolume && SettingManager.instance != null ? SettingManager.instance.GetSetting() : null;
        if (effectsSource != null)
        {
            effectsSource.volume = Mathf.Clamp01(effectsVolume) * (setting != null ? setting.masterVolume * setting.sfxVolume : 1f);
            effectsSource.mute = muted;
        }
        foreach (var voice in voices)
            if (voice.source != null && voice.source != effectsSource && effectsSource != null)
            { voice.source.volume = effectsSource.volume; voice.source.mute = muted; }
        if (musicSource != null)
        {
            musicSource.volume = Mathf.Clamp01(musicVolume) * (setting != null ? setting.masterVolume * setting.bgmVolume : 1f);
            musicSource.mute = muted;
        }
    }
    public static SceneAudioChannel InScene(Scene scene)
    {
        foreach (var channel in channels)
            if (channel != null && channel.isActiveAndEnabled && channel.gameObject.scene == scene) return channel;
        return null;
    }
    public void PlayClip(AudioClip clip) => TryPlayClip(clip);
    public bool TryPlayClip(AudioClip clip, string category = "Default")
    {
        if (clip == null || effectsSource == null || !isActiveAndEnabled) return false;
        category = AudioLibrary.NormalizeKey(category ?? "Default");
        EnforceLimits();
        int total = 0, sameClip = 0, sameCategory = 0;
        foreach (var voice in voices)
        {
            if (!voice.Active) continue;
            total++;
            if (voice.source.clip == clip) sameClip++;
            if (voice.category == category) sameCategory++;
        }
        if (total >= Mathf.Max(1, maxSimultaneousEffects) ||
            sameClip >= Mathf.Max(1, maxSimultaneousPerClip) || sameCategory >= CategoryLimit(category)) return false;
        EffectVoice available = null;
        foreach (var voice in voices)
            if (voice.source != null && !voice.Active) { available = voice; break; }
        if (available == null)
        {
            var source = voices.Count == 0 ? effectsSource : CreateEffectSource();
            available = new EffectVoice { source = source }; voices.Add(available);
        }
        ApplyVolume();
        available.source.playOnAwake = false;
        available.source.loop = false;
        available.source.clip = clip;
        available.category = category;
        available.startedFrame = Time.frameCount;
        available.source.Play();
        return true;
    }
    private int CategoryLimit(string category)
    {
        if (categoryLimits != null)
            foreach (var limit in categoryLimits)
                if (limit != null && AudioLibrary.NormalizeKey(limit.category) == category)
                    return Mathf.Max(1, limit.maxSimultaneous);
        return Mathf.Max(1, defaultCategoryMaxSimultaneous);
    }
    private void EnforceLimits()
    {
        // Lowering limits in Play Mode also stops excess voices in pool order.
        int total = 0;
        for (int i = 0; i < voices.Count; i++)
        {
            var voice = voices[i]; if (!voice.Active) continue;
            int sameClip = 0, sameCategory = 0;
            for (int j = 0; j < i; j++)
            {
                var other = voices[j]; if (!other.Active) continue;
                if (other.source.clip == voice.source.clip) sameClip++;
                if (other.category == voice.category) sameCategory++;
            }
            if (total >= Mathf.Max(1, maxSimultaneousEffects) || sameClip >= Mathf.Max(1, maxSimultaneousPerClip) ||
                sameCategory >= CategoryLimit(voice.category)) StopVoice(voice);
            else total++;
        }
    }
    private AudioSource CreateEffectSource()
    {
        var host = new GameObject("Scene Effect Voice");
        host.transform.SetParent(effectsSource.transform, false);
        var source = host.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.outputAudioMixerGroup = effectsSource.outputAudioMixerGroup;
        source.pitch = effectsSource.pitch; source.priority = effectsSource.priority;
        source.spatialBlend = effectsSource.spatialBlend; source.panStereo = effectsSource.panStereo;
        source.rolloffMode = effectsSource.rolloffMode;
        source.minDistance = effectsSource.minDistance; source.maxDistance = effectsSource.maxDistance;
        source.dopplerLevel = effectsSource.dopplerLevel; source.spread = effectsSource.spread;
        source.reverbZoneMix = effectsSource.reverbZoneMix;
        source.bypassEffects = effectsSource.bypassEffects; source.bypassListenerEffects = effectsSource.bypassListenerEffects;
        source.bypassReverbZones = effectsSource.bypassReverbZones;
        source.ignoreListenerPause = effectsSource.ignoreListenerPause;
        source.ignoreListenerVolume = effectsSource.ignoreListenerVolume;
        foreach (var curve in new[] { AudioSourceCurveType.CustomRolloff, AudioSourceCurveType.SpatialBlend,
            AudioSourceCurveType.ReverbZoneMix, AudioSourceCurveType.Spread })
            source.SetCustomCurve(curve, effectsSource.GetCustomCurve(curve));
        return source;
    }
    private static void StopVoice(EffectVoice voice)
    {
        if (voice.source != null) { voice.source.Stop(); voice.source.clip = null; }
        voice.startedFrame = -1;
    }
    public void StopAllEffects()
    {
        foreach (var voice in voices) StopVoice(voice);
        if (effectsSource != null) effectsSource.Stop();
    }
    public void PlayCategory(string category) => TryPlayClip(library != null ? library.GetRandomClipByCategory(category) : null, category);
    public void PlayNamed(string category, string clipName) => TryPlayClip(library != null ? library.GetClip(category, clipName) : null, category);
    public void PlayMusic(string clipName)
    {
        if (library == null || musicSource == null || !isActiveAndEnabled) return;
        AudioClip clip = library.GetClip(musicCategory, clipName);
        if (clip == null || (musicSource.clip == clip && musicSource.isPlaying)) return;
        ApplyVolume(); musicSource.clip = clip; musicSource.loop = true; musicSource.Play();
    }
    public void StopMusic() { if (musicSource != null) musicSource.Stop(); }
    public static void PlaySfx(GameObject owner, string category, string clipName = null)
    {
        var channel = owner != null ? InScene(owner.scene) : null;
        if (channel != null)
        {
            if (string.IsNullOrEmpty(clipName)) channel.PlayCategory(category); else channel.PlayNamed(category, clipName);
            return;
        }
        // 아직 채널을 연결하지 않은 독립/구형 씬의 기존 호출은 유지합니다.
        if (AudioManager.instance == null) return;
        if (string.IsNullOrEmpty(clipName)) AudioManager.instance.PlaySfx(category);
        else AudioManager.instance.PlaySfx(category, clipName);
    }
    public static void PlayBgm(GameObject owner, string clipName)
    {
        var channel = owner != null ? InScene(owner.scene) : null;
        if (channel != null) channel.PlayMusic(clipName);
        else if (AudioManager.instance != null) AudioManager.instance.PlayBgm(clipName);
    }
}
