using UnityEngine;
using UnityEngine.EventSystems;

// 각 씬은 독립 실행용 EventSystem을 보유하고, 함께 열었을 때만 코어 우선으로 하나를 사용합니다.
[RequireComponent(typeof(EventSystem))]
[DefaultExecutionOrder(-10000)]
public sealed class SceneEventSystemOwner : MonoBehaviour
{
    private EventSystem events;
    private BaseInputModule[] modules;
    private bool[] enabledModules;
    private bool available;
    private void Awake()
    {
        events = GetComponent<EventSystem>(); available = events.enabled;
        modules = GetComponents<BaseInputModule>(); enabledModules = new bool[modules.Length];
        for (int i = 0; i < modules.Length; i++) enabledModules[i] = modules[i].enabled;
    }
    private void OnEnable() => Refresh();
    private void OnDisable() => Refresh();
    private static void Refresh()
    {
        var owners = FindObjectsByType<SceneEventSystemOwner>(FindObjectsSortMode.InstanceID);
        SceneEventSystemOwner selected = null;
        foreach (var owner in owners)
        {
            if (!owner.isActiveAndEnabled || !owner.available || owner.events == null) continue;
            if (selected == null || owner.gameObject.scene.name == GameSceneRoutes.Core) selected = owner;
        }
        foreach (var owner in owners)
        {
            if (owner.events == null || !owner.available) continue;
            bool use = owner == selected;
            owner.events.enabled = use;
            for (int i = 0; i < owner.modules.Length; i++)
                if (owner.modules[i] != null) owner.modules[i].enabled = use && owner.enabledModules[i];
        }
        if (selected != null) EventSystem.current = selected.events;
    }
}
