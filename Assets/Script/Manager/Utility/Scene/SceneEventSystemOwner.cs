using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// 각 씬은 독립 실행용 EventSystem을 보유하고, 함께 열었을 때만 코어 우선으로 하나를 사용합니다.
[RequireComponent(typeof(EventSystem))]
[DefaultExecutionOrder(-10000)]
public sealed class SceneEventSystemOwner : MonoBehaviour
{
    private static readonly List<SceneEventSystemOwner> owners = new List<SceneEventSystemOwner>();
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
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOwners() => owners.Clear();
    private void OnEnable()
    {
        if (!owners.Contains(this)) owners.Add(this);
        Refresh();
    }
    private void OnDisable()
    {
        owners.Remove(this); SetInputEnabled(false); Refresh();
    }
    private void SetInputEnabled(bool value)
    {
        if (events == null || !available) return;
        events.enabled = value;
        for (int i = 0; i < modules.Length; i++)
            if (modules[i] != null) modules[i].enabled = value && enabledModules[i];
    }
    private static void Refresh()
    {
        owners.RemoveAll(owner => owner == null);
        SceneEventSystemOwner selected = null;
        foreach (var owner in owners)
        {
            if (!owner.isActiveAndEnabled || !owner.available || owner.events == null) continue;
            if (selected == null || owner.gameObject.scene.name == GameSceneRoutes.Core) selected = owner;
        }
        foreach (var owner in owners)
        {
            owner.SetInputEnabled(owner == selected);
        }
        // current는 EventSystem.OnEnable/OnDisable이 관리합니다.
        // 여기서 강제로 지정하면 Unity 등록 전 호출되어 unknown EventSystem 오류가 납니다.
    }
}
