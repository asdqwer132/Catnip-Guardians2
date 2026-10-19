// Executes the production Context and button relay against an isolated lifecycle model.
// Unity result-panel behavior is covered by the separately compiled SceneFlowTests.
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BattleSceneContextChecks
{
    private static void Expect(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
    private static void Set(object target, string field, object value)
        => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static void Lifecycle(BattleSceneContext context, string name)
        => typeof(BattleSceneContext).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(context, null);
    private static void Reset()
        => typeof(BattleSceneContext).GetMethod("ResetContexts", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);

    private sealed class Battle : IDisposable
    {
        public readonly GameManager Manager = new GameManager();
        public readonly BattleSceneContext Context = new BattleSceneContext();
        public Scene Scene => Context.gameObject.scene;
        public Battle(string name)
        {
            var host = new GameObject(name); host.Add(Manager); host.Add(Context);
            SceneManager.SetLoaded(host.scene, true);
            Manager.enabled = true; Context.enabled = true; Set(Context, "gameManager", Manager);
            Lifecycle(Context, "OnEnable");
        }
        public void Disable() { Context.enabled = false; Lifecycle(Context, "OnDisable"); }
        public void Enable() { Context.enabled = true; Lifecycle(Context, "OnEnable"); }
        public void Dispose() => Disable();
    }

    public static void Run()
    {
        Reset(); SceneManager.Clear(); Debug.Warnings.Clear();
        var host = new GameObject("authored-shell");
        var controller = new GameGeneralSceneController { battleScene = "Assets/Scenes/custom-fight.unity" };
        var relay = new BattleSceneButtonRelay(); host.Add(controller); host.Add(relay);
        controller.enabled = relay.enabled = true; Set(relay, "sceneController", controller);
        relay.RequestDefeat();
        Expect(Debug.Warnings.Count == 1 && !relay.TryGetBattle(out _), "General must safely wait for an unloaded Battle.");
        SceneManager.SetLoaded(new Scene("custom-fight"), true);
        Expect(!relay.TryGetBattle(out _), "A loaded scene without a registered Context is not a callable target.");
        using (var other = new Battle("other-fight"))
        {
            relay.RequestDefeat();
            Expect(other.Manager.Defeats == 0, "A missing configured target must never fall back to another Battle.");
            using (var battle = new Battle("custom-fight"))
            {
                Expect(relay.TryGetBattle(out var resolved) && resolved == battle.Context, "Late Context registration must be visible without rebuilding button listeners.");
                relay.RequestDefeat();
                Expect(battle.Manager.Defeats == 1 && other.Manager.Defeats == 0, "Relay must call only the configured Battle's public action.");

                battle.Disable(); relay.RequestDefeat();
                Expect(battle.Manager.Defeats == 1 && !relay.TryGetBattle(out _), "Disabled Contexts must release their target.");
                battle.Enable(); battle.Enable(); relay.RequestDefeat();
                Expect(battle.Manager.Defeats == 2, "Re-enabling must register once and preserve one invocation per click.");

                using (var duplicate = new Battle("custom-fight"))
                {
                    relay.RequestDefeat();
                    Expect(!relay.TryGetBattle(out _) && battle.Manager.Defeats == 2 && duplicate.Manager.Defeats == 0,
                        "Ambiguous Contexts must not select an arbitrary receiver.");
                }
                relay.RequestDefeat(); Expect(battle.Manager.Defeats == 3, "Removing a duplicate must restore the remaining Context.");

                battle.Manager.enabled = false; relay.RequestDefeat();
                Expect(battle.Manager.Defeats == 3, "A disabled local manager must not receive requests.");
                battle.Manager.enabled = true;
                Set(battle.Context, "gameManager", other.Manager); relay.RequestDefeat();
                Expect(other.Manager.Defeats == 0, "Context must reject a manager belonging to another scene.");
                Set(battle.Context, "gameManager", battle.Manager);

                SceneManager.SetLoaded(battle.Scene, false); relay.RequestDefeat();
                Expect(!relay.TryGetBattle(out _) && battle.Manager.Defeats == 3, "Unloaded scenes must not be called even before callback cleanup.");
                SceneManager.SetLoaded(battle.Scene, true);
                controller.battleScene = "other-fight"; relay.RequestDefeat();
                Expect(other.Manager.Defeats == 1 && battle.Manager.Defeats == 3, "Changing General's scene setting must change the target without hardcoded scene names.");
                controller.battleScene = "custom-fight";
            }

            using (var replacement = new Battle("custom-fight"))
            {
                relay.RequestDefeat();
                Expect(replacement.Manager.Defeats == 1, "Reloaded Battle must receive requests through its new Context.");
                relay.enabled = false; relay.RequestDefeat();
                Expect(replacement.Manager.Defeats == 1, "Disabled relays must not forward button requests.");
                relay.enabled = true;
                Set(relay, "sceneController", null); relay.RequestDefeat();
                Expect(replacement.Manager.Defeats == 1, "Missing local controller must safely stop forwarding.");
                var foreignHost = new GameObject("foreign-shell");
                var foreignController = new GameGeneralSceneController { battleScene = "custom-fight" };
                foreignHost.Add(foreignController); Set(relay, "sceneController", foreignController); relay.RequestDefeat();
                Expect(replacement.Manager.Defeats == 1, "Relay must reject a controller belonging to another scene.");
                Set(relay, "sceneController", controller);

                Reset(); Expect(!relay.TryGetBattle(out _), "A new Play must clear static registrations even with domain reload disabled.");
                replacement.Enable(); relay.RequestDefeat();
                Expect(replacement.Manager.Defeats == 2, "Context must register normally after a Play reset.");
            }
        }
        Expect(!relay.TryGetBattle(out _), "Scene teardown must leave no stale Context registration.");
        Reset(); SceneManager.Clear(); Debug.Warnings.Clear();
        Console.WriteLine("PASS Battle Context/relay lifecycle simulation: late load, configured scene selection, disable/re-enable, ambiguity, unload/reload, local references and Play reset.");
    }
}

public sealed class GameManager : MonoBehaviour
{
    public int Defeats;
    public void GameOver() => Defeats++;
}
public sealed class GameGeneralSceneController : MonoBehaviour { public string battleScene; }

namespace UnityEngine
{
    public static class Debug
    {
        public static readonly List<string> Warnings = new List<string>();
        public static void LogWarning(object message, object context) => Warnings.Add(message.ToString());
    }
}
namespace UnityEngine.SceneManagement
{
    public static class SceneManager
    {
        private static readonly Dictionary<string, Scene> loaded = new Dictionary<string, Scene>();
        public static Scene GetSceneByName(string name) => loaded.TryGetValue(name, out var scene) ? scene : default;
        public static bool IsLoaded(Scene scene) => loaded.ContainsKey(scene.name);
        public static void SetLoaded(Scene scene, bool value)
        { if (value) loaded[scene.name] = scene; else loaded.Remove(scene.name); }
        public static void Clear() => loaded.Clear();
    }
}
