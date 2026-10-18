using System;
using System.Reflection;
using UnityEngine;

public static class SceneAudioChecks
{
    private sealed class Channel : IDisposable
    {
        public readonly GameObject Owner;
        public readonly SceneAudioChannel Value = new SceneAudioChannel();
        public Channel(string scene)
        {
            Owner = new GameObject(scene); Owner.Add(Value); Value.enabled = true;
            Value.library = new AudioLibrary();
            Value.effectsSource = new AudioSource(); Value.musicSource = new AudioSource();
            Owner.Add(Value.effectsSource); Owner.Add(Value.musicSource);
            Call("OnEnable");
        }
        public void Call(string name) => typeof(SceneAudioChannel).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(Value, null);
        public void Dispose() { Value.enabled = false; Call("OnDisable"); }
    }
    private static void Expect(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    public static void Run()
    {
        AudioManager.instance = new AudioManager();
        using (var battle = new Channel("fight"))
        using (var item = new Channel("merge"))
        using (var navigation = new Channel("navigation"))
        {
            SceneAudioChannel.PlaySfx(battle.Owner,"hit");
            Expect(battle.Value.effectsSource.oneShots.Count == 1 && item.Value.effectsSource.oneShots.Count == 0, "Battle effects must stay in the battle scene source.");
            SceneAudioChannel.PlaySfx(item.Owner,"merge","success");
            Expect(item.Value.effectsSource.oneShots[0] == item.Value.library.clip, "Item effects must use their own library/source.");
            navigation.Value.muted = true; SceneAudioChannel.PlaySfx(navigation.Owner,"click");
            Expect(navigation.Value.effectsSource.mute && !battle.Value.effectsSource.mute, "Scene mute must not affect another scene.");
            Expect(AudioManager.instance.effects == 0, "Configured scenes must not fall through to the shared SFX source.");
            SceneAudioChannel.PlayBgm(battle.Owner,"song"); SceneAudioChannel.PlayBgm(item.Owner,"another song");
            SceneAudioChannel.PlayBgm(battle.Owner,"song");
            Expect(battle.Value.musicSource.starts == 1 && item.Value.musicSource.starts == 1, "Scene music must have independent sources and avoid duplicate restarts.");
            battle.Value.StopMusic(); Expect(item.Value.musicSource.isPlaying, "Stopping battle music must not stop item music.");
            SettingManager.instance = new SettingManager();
            SettingManager.instance.data.masterVolume=.5f; SettingManager.instance.data.sfxVolume=.4f;
            battle.Value.effectsVolume=.8f; battle.Call("LateUpdate");
            Expect(Math.Abs(battle.Value.effectsSource.volume-.16f)<.0001f, "Local and shared gains must compose once.");
            item.Value.followSharedVolume=false; item.Value.effectsVolume=.7f; item.Call("LateUpdate");
            Expect(Math.Abs(item.Value.effectsSource.volume-.7f)<.0001f, "Mixer-managed scene must be able to ignore shared gains.");
        }
        Expect(SceneAudioChannel.InScene(new UnityEngine.SceneManagement.Scene("fight")) == null, "Unloaded scene audio must unregister.");
        SceneAudioChannel.PlaySfx(new GameObject("legacy scene"),"hit");
        Expect(AudioManager.instance.effects == 1, "Unconfigured scenes must preserve legacy playback.");
        SettingManager.instance = null; AudioManager.instance = null;
        Console.WriteLine("PASS scene audio simulation: independent SFX/music, local mute/volume, shared-gain option, unregister/stop and legacy fallback.");
    }
}
