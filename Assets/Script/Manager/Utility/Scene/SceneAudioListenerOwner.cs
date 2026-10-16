using System.Collections.Generic;
using UnityEngine;

// 씬에 저장된 Listener는 꺼 두고 이 컴포넌트만 켭니다. 코어는 화면이 없는 동안의 예비 수신기입니다.
[RequireComponent(typeof(AudioListener))]
[DisallowMultipleComponent]
[DefaultExecutionOrder(-9999)]
public sealed class SceneAudioListenerOwner : MonoBehaviour
{
    [Tooltip("값이 큰 씬을 우선 사용합니다. 코어 0, 아이템 50, 배틀/메인/스토리/업그레이드 100.")]
    public int priority = 100;
    [Tooltip("카메라가 꺼지면 다른 씬에 양도합니다. 코어의 예비 Listener는 끄세요.")]
    public bool requireEnabledCamera = true;
    private static readonly List<SceneAudioListenerOwner> owners = new List<SceneAudioListenerOwner>();
    private AudioListener listener;
    private Camera sceneCamera;
    private bool wasAvailable;
    private bool Available => isActiveAndEnabled && listener != null &&
        (!requireEnabledCamera || (sceneCamera != null && sceneCamera.isActiveAndEnabled));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOwners() => owners.Clear();
    private void Awake()
    {
        listener = GetComponent<AudioListener>();
        sceneCamera = GetComponent<Camera>();
        listener.enabled = false;
    }
    private void OnEnable()
    {
        if (!owners.Contains(this)) owners.Add(this);
        Refresh();
    }
    private void OnDisable()
    {
        owners.Remove(this);
        if (listener != null) listener.enabled = false;
        Refresh();
    }
    private void LateUpdate()
    {
        // General의 Battle Only / Item Only 전환에도 실제로 보이는 화면을 따라갑니다.
        if (wasAvailable != Available) Refresh();
    }
    public static void Refresh()
    {
        owners.RemoveAll(owner => owner == null);
        SceneAudioListenerOwner selected = null;
        foreach (var owner in owners)
        {
            owner.wasAvailable = owner.Available;
            if (owner.wasAvailable && (selected == null || owner.priority > selected.priority)) selected = owner;
        }
        foreach (var owner in owners)
            if (owner != selected && owner.listener != null) owner.listener.enabled = false;
        if (selected != null) selected.listener.enabled = true;
    }
}
