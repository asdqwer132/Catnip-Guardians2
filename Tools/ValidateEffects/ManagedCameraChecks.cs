// Executes production camera-binding lifecycle code with the same isolated API
// model as input/audio ownership checks. Does not execute the Unity renderer.
using System;
using System.Reflection;
using UnityEngine;

public static class SceneCameraChecks
{
    private sealed class View : IDisposable
    {
        public readonly Camera Camera = new Camera();
        public readonly SceneCameraBinding Binding = new SceneCameraBinding();
        public View(string scene, SceneCameraRole role, int priority, int layers, int composedLayers, bool settings = false)
        {
            var host = new GameObject(scene); host.Add(Camera); host.Add(Binding);
            Binding.role = role; Binding.priority = priority;
            Binding.standaloneLayers = layers; Binding.composedLayers = composedLayers;
            Binding.receiveVisibilitySettings = settings;
            Binding.standaloneTag = settings ? "MainCamera" : "Untagged";
            Binding.composedTag = Binding.standaloneTag;
            Binding.enabled = true; Camera.enabled = role == SceneCameraRole.Content;
            Camera.cullingMask = -1; // Inspector에 남은 이전 Everything 값도 프로필이 교정해야 합니다.
            Camera.rect = new Rect(0, 0, 1, 1);
            Call("Awake"); Call("OnEnable");
        }
        public void Call(string method) => typeof(SceneCameraBinding)
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Binding, null);
        public void Dispose() { Binding.enabled = false; Call("OnDisable"); }
    }
    private static void Expect(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }

    private static string EntryOrder(bool coreFirst)
    {
        // Arbitrary scene names and layer index demonstrate that runtime policy
        // does not depend on the game's scene names or on item layer 31.
        const int itemLayer = 1 << 25;
        View core = null, general = null;
        try
        {
            if (coreFirst) core = new View("shared", SceneCameraRole.Background, 0, 0, 0);
            general = new View("shell", SceneCameraRole.Background, 40, 0, 0);
            if (!coreFirst) core = new View("shared", SceneCameraRole.Background, 0, 0, 0);
            using (var battle = new View("fight", SceneCameraRole.Content, 90, ~itemLayer, ~itemLayer, true))
            using (var item = new View("merge", SceneCameraRole.Content, 50, -1, itemLayer))
            {
                battle.Binding.SetComposed(true); item.Binding.SetComposed(true);
                Expect(!core.Camera.enabled && general.Camera.enabled, "General must own the background regardless of arrival order.");
                Expect(core.Camera.cullingMask == 0 && general.Camera.cullingMask == 0, "Background must never draw enemies.");
                core.Camera.cullingMask = general.Camera.cullingMask = -1;
                core.Call("LateUpdate"); general.Call("LateUpdate");
                Expect(core.Camera.cullingMask == 0 && general.Camera.cullingMask == 0, "Configured background profiles must correct stale native camera masks.");
                core.Binding.priority = 80; SceneCameraBinding.Refresh();
                Expect(core.Camera.enabled && !general.Camera.enabled, "Edited priority must control ownership without scene-name special cases.");
                core.Binding.priority = 0; SceneCameraBinding.Refresh();
                Expect(!core.Camera.enabled && general.Camera.enabled, "Restoring priority must return ownership to the configured winner.");
                Expect(SceneCameraBinding.SettingsCamera == battle.Camera, "UI background must not take gameplay visibility settings.");
                Expect(SceneCameraBinding.ContentIn(item.Camera.gameObject.scene) == item.Binding, "Content lookup must use the configured scene binding.");
                battle.Camera.rect = new Rect(0, 0, .6f, .75f); battle.Call("LateUpdate");
                Expect(battle.Camera.rect.height == .75f, "Content binding must preserve General's navigation viewport.");
                Expect(item.Camera.cullingMask == itemLayer && (battle.Camera.cullingMask & itemLayer) == 0, "Composed camera masks must use configured profiles.");
                return general.Camera.enabled + ":" + general.Camera.cullingMask + ":" + battle.Camera.cullingMask + ":" + item.Camera.cullingMask;
            }
        }
        finally
        {
            general?.Dispose();
            if (core != null) { Expect(core.Camera.enabled, "Core background must resume after General unloads."); core.Dispose(); }
        }
    }
    public static void Run()
    {
        Expect(EntryOrder(true) == EntryOrder(false), "Core and General entry must produce identical camera states.");
        Console.WriteLine("PASS camera lifecycle simulation: Core-first and General-first give identical backgrounds, viewports, masks and settings target; Core fallback resumes.");
        using (var battle = new View("custom battle", SceneCameraRole.Content, 10, 255, 15, true))
        {
            battle.Binding.SetLayerVisible(2, false); Expect(battle.Camera.cullingMask == 251, "Settings must hide only their configured layer.");
            battle.Binding.SetComposed(true); Expect(battle.Camera.cullingMask == 11, "Settings overrides must survive composition.");
            battle.Binding.SetLayerVisible(7, true); Expect(battle.Camera.cullingMask == 11, "Settings must not add a layer excluded by the composed profile.");
            battle.Binding.SetComposed(false); Expect(battle.Camera.cullingMask == 251, "Standalone must restore its configured mask and visibility settings.");
            battle.Binding.SetLayerVisible(2, true); Expect(battle.Camera.cullingMask == 255, "Hidden allowed layer must be restorable.");
            battle.Camera.enabled = false; SceneCameraBinding.Refresh(); Expect(SceneCameraBinding.SettingsCamera == null, "Hidden content must release settings ownership.");
            battle.Camera.enabled = true; SceneCameraBinding.Refresh(); Expect(SceneCameraBinding.SettingsCamera == battle.Camera, "Re-enabled content must reclaim settings ownership.");
        }
        using (var item = new View("custom item", SceneCameraRole.Content, 10, -1, 1 << 20))
        {
            item.Binding.standaloneTag = "MainCamera"; item.Binding.composedTag = "Untagged";
            item.Binding.SetComposed(true); Expect(item.Camera.tag == "Untagged", "Composed item must not steal the main-camera tag.");
            item.Camera.rect = new Rect(.6f, 0, .4f, .75f); item.Camera.enabled = false;
            item.Binding.SetComposed(false); Expect(item.Camera.tag == "MainCamera" && item.Camera.cullingMask == -1, "Standalone item must recover its own tag and layers.");
            Expect(item.Camera.enabled && item.Camera.rect.width == 1 && item.Camera.rect.height == 1, "Standalone item must recover its original viewport and enable state.");
        }
        Console.WriteLine("PASS camera lifecycle simulation: editable layers/tags, standalone restoration, settings bounds, hidden-view fallback and no leaked registrations.");
        Expect(SceneCameraBinding.SettingsCamera == null, "Camera registrations leaked after scene unload.");
    }
}
