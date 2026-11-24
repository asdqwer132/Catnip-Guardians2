using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// 이 게임은 명암 조명을 사용하지 않습니다. 기존 Lit 머티리얼도 모든 화면에서 원색으로 표시합니다.
[DefaultExecutionOrder(-20000), DisallowMultipleComponent]
public sealed class SceneFlatLighting : MonoBehaviour
{
    private static SceneFlatLighting instance;
    private Light2D fill;
    private readonly Dictionary<Light2D,bool> sceneLights = new Dictionary<Light2D,bool>();
    public Light2D FillLight => fill;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var root = new GameObject("Shared Full Bright Lighting");
        instance = root.AddComponent<SceneFlatLighting>();
        DontDestroyOnLoad(root);
    }
    private void OnEnable()
    {
        ConfigureFill();
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        RenderPipelineManager.beginCameraRendering += BeforeCameraRendering;
        for (int i=0;i<SceneManager.sceneCount;i++) RegisterScene(SceneManager.GetSceneAt(i));
    }
    private void ConfigureFill()
    {
        var layers = SortingLayer.layers;
        var ids = new int[layers.Length];
        for (int i=0;i<ids.Length;i++) ids[i]=layers[i].id;
        if (fill == null)
        {
            var root = new GameObject("White Global 2D Light");
            root.SetActive(false); root.transform.SetParent(transform,false);
            fill = root.AddComponent<Light2D>();
            // Global 전환 시 URP가 정렬 레이어를 즉시 검사하므로 활성화보다 먼저 지정합니다.
            fill.targetSortingLayers = ids;
            fill.lightType = Light2D.LightType.Global;
            fill.blendStyleIndex = 0; fill.color = Color.white; fill.intensity = 1f;
            fill.shadowsEnabled = false;
            root.SetActive(true);
        }
        fill.targetSortingLayers = ids;
        fill.enabled = true;
    }
    public void RegisterScene(Scene scene)
    {
        if (!scene.isLoaded) return;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var light in root.GetComponentsInChildren<Light2D>(true))
            {
                if (light == fill || sceneLights.ContainsKey(light)) continue;
                sceneLights.Add(light,light.enabled); light.enabled = false;
            }
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { RegisterScene(scene); ConfigureFill(); }
    private void OnSceneUnloaded(Scene scene)
    {
        var removed = new List<Light2D>();
        foreach (var pair in sceneLights) if (pair.Key == null) removed.Add(pair.Key);
        foreach (var light in removed) sceneLights.Remove(light);
    }
    private void BeforeCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (fill == null || camera == null) return;
        // Light2D도 카메라 Culling Mask에 걸립니다. 각 카메라가 이미 허용한 레이어를 사용합니다.
        // 카메라 마스크나 게임 오브젝트 레이어를 변경하지 않습니다.
        uint mask = unchecked((uint)camera.cullingMask);
        for (int layer=0;layer<32;layer++)
            if ((mask & (1u << layer)) != 0) { fill.gameObject.layer=layer; break; }
        foreach (var pair in sceneLights) if (pair.Key != null && pair.Key.enabled) pair.Key.enabled=false;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        RenderPipelineManager.beginCameraRendering -= BeforeCameraRendering;
        if (fill != null) fill.enabled=false;
        foreach (var pair in sceneLights) if (pair.Key != null) pair.Key.enabled=pair.Value;
        sceneLights.Clear();
    }
}
