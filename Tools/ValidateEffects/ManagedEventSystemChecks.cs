// Runs the actual SceneEventSystemOwner callbacks with a synchronous registration model.
// This catches overlapping enable calls; it does not execute Unity scene loading or uGUI.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

public static class EventSystemChecks
{
    private sealed class SceneInput : IDisposable
    {
        public readonly EventSystem Events = new EventSystem();
        public readonly BaseInputModule Module = new BaseInputModule();
        public readonly SceneEventSystemOwner Owner = new SceneEventSystemOwner();
        public SceneInput(string sceneName)
        {
            var host = new GameObject(sceneName);
            host.Add(Events); host.Add(Module); host.Add(Owner);
            Owner.enabled = true;
            Call("Awake"); Call("OnEnable");
        }
        public void Disable() { Owner.enabled = false; Call("OnDisable"); }
        public void Enable() { Owner.enabled = true; Call("OnEnable"); }
        public void Dispose() => Disable();
        private void Call(string method) => typeof(SceneEventSystemOwner)
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Owner, null);
    }

    private static void ExpectOwner(SceneInput selected, params SceneInput[] others)
    {
        if (EventSystem.Active.Count != 1 || !selected.Events.enabled || !selected.Module.enabled ||
            others.Any(other => other.Events.enabled || other.Module.enabled))
            throw new InvalidOperationException("Exactly one matching EventSystem and module must own input.");
    }

    public static void Main()
    {
        // Core boot: child scenes must not register a second EventSystem even briefly.
        using (var core = new SceneInput(GameSceneRoutes.Core))
        using (var general = new SceneInput(GameSceneRoutes.General))
        using (var battle = new SceneInput(GameSceneRoutes.Battle))
        using (var item = new SceneInput(GameSceneRoutes.Item))
        {
            ExpectOwner(core, general, battle, item);
            battle.Disable(); ExpectOwner(core, general, battle, item);
            battle.Enable(); ExpectOwner(core, general, battle, item);
        }
        Console.WriteLine("PASS input lifecycle simulation: Core -> General + Battle + Item, child disable/re-enable.");

        // Reverse loading order: independent battle -> Core added -> Core removed.
        using (var battle = new SceneInput(GameSceneRoutes.Battle))
        {
            ExpectOwner(battle);
            using (var core = new SceneInput(GameSceneRoutes.Core))
                ExpectOwner(core, battle);
            ExpectOwner(battle);
            battle.Disable();
            if (EventSystem.Active.Count != 0) throw new InvalidOperationException("Disabled owners retained input.");
            battle.Enable(); ExpectOwner(battle);
        }
        Console.WriteLine("PASS input lifecycle simulation: standalone Battle, late Core takeover, fallback, re-enable.");

        // Every other scene remains independently usable with initially disabled components.
        foreach (string scene in new[] { GameSceneRoutes.General, GameSceneRoutes.Item,
                                        GameSceneRoutes.Main, GameSceneRoutes.Story, GameSceneRoutes.Upgrade })
            using (var input = new SceneInput(scene)) ExpectOwner(input);
        if (EventSystem.Active.Count != 0) throw new InvalidOperationException("Input registration leaked between runs.");
        Console.WriteLine("PASS input lifecycle simulation: all standalone scenes and complete input cleanup.");
    }
}

// Deliberately isolated from the Unity reference DLLs used by the API compile.
namespace UnityEngine
{
    public sealed class RequireComponent : Attribute { public RequireComponent(Type type) { } }
    public sealed class DisallowMultipleComponent : Attribute { }
    public sealed class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int order) { } }
    public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string text) { } }
    public sealed class SerializeField : Attribute { }
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { } }
    public sealed class GameObject
    {
        private readonly List<Behaviour> components = new List<Behaviour>();
        public Scene scene;
        public GameObject(string sceneName) { scene.name = sceneName; }
        public void Add(Behaviour component) { component.gameObject = this; components.Add(component); }
        public T[] GetComponents<T>() => components.OfType<T>().ToArray();
    }
    public struct Scene { public string name; }
    public class Behaviour
    {
        private bool active;
        public GameObject gameObject;
        public bool isActiveAndEnabled => active;
        public bool enabled
        {
            get => active;
            set { if (active == value) return; active = value; OnEnabledChanged(); }
        }
        protected virtual void OnEnabledChanged() { }
        public T GetComponent<T>() => gameObject.GetComponents<T>().SingleOrDefault();
        public T[] GetComponents<T>() => gameObject.GetComponents<T>();
    }
    public class MonoBehaviour : Behaviour { }
}
namespace UnityEngine.EventSystems
{
    public sealed class EventSystem : Behaviour
    {
        public static readonly HashSet<EventSystem> Active = new HashSet<EventSystem>();
        protected override void OnEnabledChanged()
        {
            if (enabled) Active.Add(this); else Active.Remove(this);
            if (Active.Count > 1) throw new InvalidOperationException("Overlapping EventSystem registration.");
        }
    }
    public sealed class BaseInputModule : Behaviour
    {
        protected override void OnEnabledChanged()
        {
            if (enabled && (!GetComponent<EventSystem>().enabled || EventSystem.Active.Count != 1))
                throw new InvalidOperationException("Module enabled before its exclusive EventSystem.");
        }
    }
}
