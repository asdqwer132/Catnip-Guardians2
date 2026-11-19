using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

public enum language { english, korean }

[DefaultExecutionOrder(-1000)]
public class LanguageManager : MonoBehaviour
{
    private static readonly List<LanguageManager> candidates = new List<LanguageManager>();
    private static LanguageManager current;
    public static event Action InstanceChanged;
    public event Action<language> LanguageChanged;
    public static LanguageManager instance => current;
    public static language CurrentLanguage => instance != null ? instance.selectedLan : language.english;

    [SerializeField, FormerlySerializedAs("selectedLan")] private language selectedLanguage = language.english;
    public language selectedLan { get => selectedLanguage; set => SetLanguage(value); }
    public bool IsCoreAuthority => gameObject.scene.name == GameSceneRoutes.Core;
    private language lastPublished;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        candidates.Clear();
        current = null;
        InstanceChanged = null;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnEnable()
    {
        lastPublished = selectedLanguage;
        candidates.Add(this);
        SelectAuthority();
    }

    private void OnDisable()
    {
        candidates.Remove(this);
        SelectAuthority();
    }

    private static void OnActiveSceneChanged(Scene previous, Scene next) => SelectAuthority();

    private static void SelectAuthority()
    {
        var activeScene = SceneManager.GetActiveScene();
        LanguageManager next = null;
        foreach (var candidate in candidates)
        {
            if (candidate == null) continue;
            if (candidate.IsCoreAuthority)
            {
                next = candidate;
                break;
            }
            if (candidate.gameObject.scene == activeScene) next = candidate;
        }

        if (ReferenceEquals(current, next)) return;
        current = next;
        if (current != null) current.lastPublished = current.selectedLanguage;
        InstanceChanged?.Invoke();
    }

    // Inspector에서 Play 중 값을 바꾼 경우도 같은 변경 알림으로 처리합니다.
    private void Update() => PublishIfChanged();
    public void SetLanguage(int value)
    {
        if (Enum.IsDefined(typeof(language), value)) SetLanguage((language)value);
    }
    public void SetLanguage(language value)
    {
        if (!Enum.IsDefined(typeof(language), value)) return;
        selectedLanguage = value; PublishIfChanged();
    }
    private void PublishIfChanged()
    {
        if (instance != this || selectedLanguage == lastPublished) return;
        lastPublished = selectedLanguage;
        LanguageChanged?.Invoke(selectedLanguage);
    }
}
