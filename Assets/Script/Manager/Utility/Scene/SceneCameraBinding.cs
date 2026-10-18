using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneCameraRole { Background, Content, Navigation }

// 씬 이름이나 로딩 순서 대신 각 카메라에 저장한 역할/설정을 사용합니다.
[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
[DefaultExecutionOrder(-9000)]
public sealed class SceneCameraBinding : MonoBehaviour
{
    public SceneCameraRole role = SceneCameraRole.Content;
    [Tooltip("Background끼리 높은 값 하나만 켭니다. 같은 씬의 Content 선택과 설정 대상 선택에도 사용합니다.")]
    public int priority;
    [Header("표시 레이어")]
    public LayerMask standaloneLayers = -1;
    public LayerMask composedLayers = -1;
    [Header("카메라 태그")]
    public string standaloneTag = "Untagged";
    public string composedTag = "Untagged";
    [Header("Background 역할 전용")]
    public Rect backgroundViewport = new Rect(0, 0, 1, 1);
    public float backgroundDepth = -100f;
    [Header("게임 표시 설정")]
    [Tooltip("피해 숫자/체력바 설정을 적용할 콘텐츠 카메라에만 켭니다.")]
    public bool receiveVisibilitySettings;

    private static readonly List<SceneCameraBinding> bindings = new List<SceneCameraBinding>();
    private static SceneCameraBinding settingsBinding;
    public static event Action SettingsCameraChanged;
    private Camera boundCamera;
    private bool composed;
    private Rect standaloneViewport;
    private bool standaloneEnabled;
    private int visibilityOverrides, visibleOverrides;
    public Camera Camera => boundCamera != null ? boundCamera : (boundCamera = GetComponent<Camera>());
    public bool IsComposed => composed;
    public static Camera SettingsCamera => settingsBinding != null ? settingsBinding.Camera : null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetBindings()
    {
        bindings.Clear(); settingsBinding = null; SettingsCameraChanged = null;
    }
    private void Awake()
    {
        if (role == SceneCameraRole.Background) Camera.enabled = false;
        ApplyConfiguration();
    }
    private void OnEnable()
    {
        if (!bindings.Contains(this)) bindings.Add(this);
        ApplyConfiguration(); Refresh();
    }
    private void OnDisable()
    {
        bindings.Remove(this);
        if (role == SceneCameraRole.Background && boundCamera != null) boundCamera.enabled = false;
        Refresh();
    }
    private void LateUpdate() { ApplyConfiguration(); Refresh(); }
    public void SetComposed(bool value)
    {
        if (value && !composed)
        { standaloneViewport = Camera.rect; standaloneEnabled = Camera.enabled; }
        if (!value && composed)
        { Camera.rect = standaloneViewport; Camera.enabled = standaloneEnabled; }
        composed = value; ApplyConfiguration(); Refresh();
    }
    private void ApplyConfiguration()
    {
        int layers = composed ? composedLayers.value : standaloneLayers.value;
        // 표시 설정은 카메라가 허용한 레이어 안에서만 숨기거나 복구합니다.
        Camera.cullingMask = receiveVisibilitySettings
            ? layers & (~visibilityOverrides | visibleOverrides) : layers;
        string cameraTag = composed ? composedTag : standaloneTag;
        if (!string.IsNullOrEmpty(cameraTag) && !Camera.CompareTag(cameraTag)) Camera.tag = cameraTag;
        if (role == SceneCameraRole.Background)
        { Camera.rect = backgroundViewport; Camera.depth = backgroundDepth; }
    }
    public void SetLayerVisible(int layer, bool visible)
    {
        if (!receiveVisibilitySettings || layer < 0 || layer > 31) return;
        int bit = 1 << layer;
        visibilityOverrides |= bit;
        if (visible) visibleOverrides |= bit; else visibleOverrides &= ~bit;
        ApplyConfiguration();
    }
    public static SceneCameraBinding ContentIn(Scene scene)
        => InScene(scene, SceneCameraRole.Content);
    public static SceneCameraBinding NavigationIn(Scene scene)
        => InScene(scene, SceneCameraRole.Navigation);
    private static SceneCameraBinding InScene(Scene scene, SceneCameraRole role)
    {
        SceneCameraBinding result = null;
        foreach (var value in bindings)
            if (value != null && value.isActiveAndEnabled && value.role == role && value.gameObject.scene == scene &&
                (result == null || value.priority > result.priority)) result = value;
        return result;
    }
    public static void Refresh()
    {
        bindings.RemoveAll(value => value == null);
        SceneCameraBinding background = null, settings = null;
        foreach (var value in bindings)
        {
            if (!value.isActiveAndEnabled) continue;
            if (value.role == SceneCameraRole.Background && (background == null || value.priority > background.priority)) background = value;
            if (value.role == SceneCameraRole.Content && value.receiveVisibilitySettings && value.Camera.isActiveAndEnabled &&
                (settings == null || value.priority > settings.priority)) settings = value;
        }
        // 이전 배경을 먼저 끄고 선택한 배경만 켭니다.
        foreach (var value in bindings)
            if (value.role == SceneCameraRole.Background && value != background) value.Camera.enabled = false;
        if (background != null) background.Camera.enabled = true;
        if (settingsBinding == settings) return;
        settingsBinding = settings; SettingsCameraChanged?.Invoke();
    }
}
