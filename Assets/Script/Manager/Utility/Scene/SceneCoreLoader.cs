using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Attach to a standalone entry scene to reuse Core's settings and common services.
public sealed class SceneCoreLoader : MonoBehaviour
{
    public string coreScene = GameSceneRoutes.Core;
    public bool loadOnStart = true;
    private bool openingSettings;
    private static readonly Dictionary<string, AsyncOperation> loads = new Dictionary<string, AsyncOperation>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetLoads() => loads.Clear();

    private IEnumerator Start()
    {
        if (loadOnStart) yield return EnsureCore();
    }

    public IEnumerator EnsureCore()
    {
        string target = GameSceneRoutes.Resolve(coreScene);
        if (string.IsNullOrEmpty(target) || SceneManager.GetSceneByName(target).isLoaded) yield break;
        if (!loads.TryGetValue(target, out var operation) || operation == null || operation.isDone)
        {
            if (!Application.CanStreamedLevelBeLoaded(target))
            {
                Debug.LogError("Core scene is not in the build scene list: " + target, this);
                yield break;
            }
            operation = SceneManager.LoadSceneAsync(target, LoadSceneMode.Additive);
            loads[target] = operation;
        }
        while (!operation.isDone) yield return null;
        loads.Remove(target);
        // Let Core managers finish Start before handling an early button click.
        yield return null;
    }

    public void OpenSettings()
    {
        if (isActiveAndEnabled && !openingSettings) StartCoroutine(OpenSettingsWhenReady());
    }

    private IEnumerator OpenSettingsWhenReady()
    {
        openingSettings = true;
        yield return EnsureCore();
        if (!isActiveAndEnabled) { openingSettings = false; yield break; }
        if (SettingWindowController.instance != null) SettingWindowController.instance.OpenSetting(false);
        else Debug.LogWarning("Core settings controller is not available.", this);
        openingSettings = false;
    }
    private void OnDisable() => openingSettings = false;
}
