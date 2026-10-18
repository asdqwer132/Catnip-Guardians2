// Audio transport simulation only. Production APIs are separately compiled against Unity DLLs.
using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine
{
    public sealed class RangeAttribute : Attribute { public RangeAttribute(float min, float max) { } }
    public static class Mathf { public static float Clamp01(float value) => Math.Clamp(value, 0f, 1f); }
    public sealed class AudioClip { }
    public sealed class AudioSource : Behaviour
    {
        public float volume;
        public bool mute, loop, isPlaying;
        public AudioClip clip;
        public int starts, stops;
        public readonly List<AudioClip> oneShots = new List<AudioClip>();
        public void PlayOneShot(AudioClip value) { oneShots.Add(value); isPlaying = true; }
        public void Play() { starts++; isPlaying = true; }
        public void Stop() { stops++; isPlaying = false; }
    }
}
public sealed class AudioLibrary
{
    public AudioClip clip = new AudioClip();
    public AudioClip GetClip(string category, string name) => clip;
    public AudioClip GetRandomClipByCategory(string category) => clip;
}
public sealed class GameSettingData { public float masterVolume=1, sfxVolume=1, bgmVolume=1; }
public sealed class SettingManager
{
    public static SettingManager instance;
    public readonly GameSettingData data = new GameSettingData();
    public GameSettingData GetSetting() => data;
}
public sealed class AudioManager
{
    public static AudioManager instance;
    public int effects, music;
    public void PlaySfx(string category) { effects++; }
    public void PlaySfx(string category, string clip) { effects++; }
    public void PlayBgm(string clip) { music++; }
}
