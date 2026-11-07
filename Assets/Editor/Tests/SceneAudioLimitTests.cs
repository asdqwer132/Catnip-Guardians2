using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class SceneAudioLimitTests
{
    private GameObject host;
    private SceneAudioChannel channel;
    private readonly List<AudioClip> clips = new List<AudioClip>();
    private AudioClip Clip()
    {
        var clip = AudioClip.Create("Limit test", 44100, 1, 44100, false);
        clips.Add(clip); return clip;
    }
    [UnitySetUp] public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        host = new GameObject("Scene audio");
        channel = host.AddComponent<SceneAudioChannel>();
        channel.effectsSource = host.AddComponent<AudioSource>();
        channel.effectsSource.playOnAwake = false;
        channel.musicSource = host.AddComponent<AudioSource>();
        channel.musicSource.playOnAwake = false;
        channel.maxSimultaneousPerClip = channel.defaultCategoryMaxSimultaneous = 16;
    }
    [UnityTearDown] public IEnumerator TearDown()
    {
        if (host != null) Object.DestroyImmediate(host);
        foreach (var clip in clips) Object.DestroyImmediate(clip);
        clips.Clear(); yield return new ExitPlayMode();
    }
    [Test] public void BurstRequestsRespectTotalLimitAndReuseSourcesAfterStop()
    {
        channel.maxSimultaneousEffects = 3;
        var clip = Clip();
        for (int i = 0; i < 30; i++) Assert.That(channel.TryPlayClip(clip), Is.EqualTo(i < 3));
        Assert.That(channel.ActiveEffectCount, Is.EqualTo(3));
        int sources = host.GetComponentsInChildren<AudioSource>().Length;
        channel.StopAllEffects(); Assert.That(channel.ActiveEffectCount, Is.Zero);
        for (int i = 0; i < 3; i++) Assert.That(channel.TryPlayClip(clip), Is.True);
        Assert.That(host.GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(sources));
    }
    [Test] public void SameClipAndNormalizedCategoryHaveIndependentLimits()
    {
        channel.maxSimultaneousPerClip = 1;
        channel.categoryLimits.Add(new SfxCategoryLimit { category = "Attack", maxSimultaneous = 2 });
        var attack = Clip();
        Assert.That(channel.TryPlayClip(attack, "Attack"), Is.True);
        Assert.That(channel.TryPlayClip(attack, "Hit"), Is.False);
        Assert.That(channel.TryPlayClip(Clip(), "attack"), Is.True);
        Assert.That(channel.TryPlayClip(Clip(), "Attack"), Is.False);
        Assert.That(channel.TryPlayClip(Clip(), "Hit"), Is.True);
    }
    [Test] public void LoweredLimitKeepsMusicSeparateAndDisableClearsEffects()
    {
        var clip = Clip();
        for (int i = 0; i < 4; i++) Assert.That(channel.TryPlayClip(clip), Is.True);
        channel.musicSource.clip = clip; channel.musicSource.loop = true;
        channel.maxSimultaneousEffects = 1;
        Assert.That(channel.TryPlayClip(clip), Is.False);
        Assert.That(channel.ActiveEffectCount, Is.EqualTo(1));
        Assert.That(channel.musicSource.clip, Is.SameAs(clip));
        channel.enabled = false;
        Assert.That(channel.ActiveEffectCount, Is.Zero);
        Assert.That(channel.TryPlayClip(clip), Is.False);
    }
}
