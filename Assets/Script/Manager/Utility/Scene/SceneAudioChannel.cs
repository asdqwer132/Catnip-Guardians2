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
    private static readonly List<SceneAudioChannel> channels = new List<SceneAudioChannel>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetChannels() => channels.Clear();
    private void OnEnable() { if (!channels.Contains(this)) channels.Add(this); ApplyVolume(); }
    private void OnDisable()
    {
        channels.Remove(this);
        if (effectsSource != null) effectsSource.Stop();
        if (musicSource != null) musicSource.Stop();
    }
    private void LateUpdate() => ApplyVolume();
    private void ApplyVolume()
    {
        var setting = followSharedVolume && SettingManager.instance != null ? SettingManager.instance.GetSetting() : null;
        if (effectsSource != null)
        {
            effectsSource.volume = Mathf.Clamp01(effectsVolume) * (setting != null ? setting.masterVolume * setting.sfxVolume : 1f);
            effectsSource.mute = muted;
        }
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
    public void PlayClip(AudioClip clip)
    {
        if (clip == null || effectsSource == null || !isActiveAndEnabled) return;
        ApplyVolume(); effectsSource.PlayOneShot(clip);
    }
    public void PlayCategory(string category) => PlayClip(library != null ? library.GetRandomClipByCategory(category) : null);
    public void PlayNamed(string category, string clipName) => PlayClip(library != null ? library.GetClip(category, clipName) : null);
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
