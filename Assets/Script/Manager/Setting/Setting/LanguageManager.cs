using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum language { english, korean }

[DefaultExecutionOrder(-1000)]
public class LanguageManager : MonoBehaviour
{
    private static LanguageManager current;
    public static event Action InstanceChanged;
    public event Action<language> LanguageChanged;
    public static LanguageManager instance
    {
        get => current;
        set
        {
            if (ReferenceEquals(current, value)) return;
            current = value; InstanceChanged?.Invoke();
        }
    }
    public static language CurrentLanguage => instance != null ? instance.selectedLan : language.english;

    [SerializeField, FormerlySerializedAs("selectedLan")] private language selectedLanguage = language.english;
    public language selectedLan { get => selectedLanguage; set => SetLanguage(value); }
    private language lastPublished;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { current = null; InstanceChanged = null; }
    private void OnEnable()
    {
        if (instance != null && instance != this) { enabled = false; return; }
        lastPublished = selectedLanguage;
        instance = this;
    }
    private void OnDisable() { if (ReferenceEquals(instance, this)) instance = null; }
    private void OnDestroy() { if (ReferenceEquals(instance, this)) instance = null; }
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
