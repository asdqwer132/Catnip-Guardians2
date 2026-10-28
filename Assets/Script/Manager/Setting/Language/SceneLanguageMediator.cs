using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// LanguageManager는 전역 선택만 소유하고, 각 씬의 중개기가 그 씬의 표시 대상을 관리합니다.
[DisallowMultipleComponent]
[DefaultExecutionOrder(-500)]
public sealed class SceneLanguageMediator : MonoBehaviour
{
    public LocalizedTextTable translations;
    [Tooltip("코어 없이 이 씬만 실행할 때 사용할 언어입니다.")]
    public language standaloneLanguage = language.english;
    private LanguageManager boundManager;
    private Scene ownedScene;
    private static readonly Dictionary<Scene, SceneLanguageMediator> owners = new Dictionary<Scene, SceneLanguageMediator>();
    private static readonly Dictionary<Scene, HashSet<LocalizedText>> texts = new Dictionary<Scene, HashSet<LocalizedText>>();
    public language CurrentLanguage => boundManager != null ? boundManager.selectedLan : standaloneLanguage;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { owners.Clear(); texts.Clear(); }
    private void OnEnable()
    {
        ownedScene = gameObject.scene;
        if (owners.TryGetValue(ownedScene, out var other) && other != null && other != this)
        {
            Debug.LogError("SceneLanguageMediator는 씬마다 하나만 연결하세요.", this); enabled = false; return;
        }
        owners[ownedScene] = this;
        LanguageManager.InstanceChanged += BindManager;
        BindManager();
    }
    private void OnDisable()
    {
        LanguageManager.InstanceChanged -= BindManager;
        if (boundManager != null) boundManager.LanguageChanged -= OnLanguageChanged;
        boundManager = null;
        if (owners.TryGetValue(ownedScene, out var owner) && owner == this) owners.Remove(ownedScene);
    }
    private void BindManager()
    {
        if (boundManager != null) boundManager.LanguageChanged -= OnLanguageChanged;
        boundManager = LanguageManager.instance;
        if (boundManager != null) boundManager.LanguageChanged += OnLanguageChanged;
        RefreshAll();
    }
    private void OnLanguageChanged(language selected) => RefreshAll();
    public void RefreshAll()
    {
        if (!texts.TryGetValue(ownedScene, out var registered)) return;
        foreach (var text in new List<LocalizedText>(registered))
            if (text != null && text.isActiveAndEnabled) text.Refresh();
    }
    internal static void Register(LocalizedText text)
    {
        Scene scene = text.gameObject.scene;
        if (!texts.TryGetValue(scene, out var registered)) texts.Add(scene, registered = new HashSet<LocalizedText>());
        registered.Add(text); RefreshText(text);
    }
    internal static void Unregister(LocalizedText text, Scene scene)
    {
        if (!texts.TryGetValue(scene, out var registered)) return;
        registered.Remove(text); if (registered.Count == 0) texts.Remove(scene);
    }
    internal static void RefreshText(LocalizedText text)
    {
        if (owners.TryGetValue(text.gameObject.scene, out var owner) && owner != null)
            text.Apply(owner.CurrentLanguage, owner.translations);
        else text.Apply(language.english, null);
    }
}
