using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// 각 씬은 독립 실행용 EventSystem을 보유하고, 함께 열었을 때만 코어 우선으로 하나를 사용합니다.
[RequireComponent(typeof(EventSystem))]
[DisallowMultipleComponent]
[DefaultExecutionOrder(-10000)]
public sealed class SceneEventSystemOwner : MonoBehaviour
{
    [Tooltip("이 씬이 UI 입력을 맡을 때만 켤 입력 모듈입니다. EventSystem과 모듈은 씬에서 꺼 두세요.")]
    [SerializeField] private BaseInputModule[] inputModules;

    private static readonly List<SceneEventSystemOwner> owners = new List<SceneEventSystemOwner>();
    private EventSystem events;

    private void Awake()
    {
        events = GetComponent<EventSystem>();
        if (inputModules == null || inputModules.Length == 0)
            inputModules = GetComponents<BaseInputModule>();

        // enabled는 사용 가능 여부가 아니라 소유권 결과입니다. 직렬화된 초기값은 false입니다.
        SetInputEnabled(false);
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
        if (events == null) return;
        events.enabled = value;
        for (int i = 0; i < inputModules.Length; i++)
            if (inputModules[i] != null) inputModules[i].enabled = value;
    }
    private static void Refresh()
    {
        owners.RemoveAll(owner => owner == null);
        SceneEventSystemOwner selected = null;
        foreach (var owner in owners)
        {
            if (!owner.isActiveAndEnabled || owner.events == null) continue;
            if (selected == null || (selected.gameObject.scene.name != GameSceneRoutes.Core &&
                                    owner.gameObject.scene.name == GameSceneRoutes.Core)) selected = owner;
        }
        foreach (var owner in owners)
        {
            // 새 EventSystem.OnEnable보다 기존 EventSystem.OnDisable이 반드시 먼저 끝나야 합니다.
            if (owner != selected) owner.SetInputEnabled(false);
        }
        if (selected != null) selected.SetInputEnabled(true);
        // current는 EventSystem.OnEnable/OnDisable이 관리합니다.
        // 여기서 강제로 지정하면 Unity 등록 전 호출되어 unknown EventSystem 오류가 납니다.
    }
}
