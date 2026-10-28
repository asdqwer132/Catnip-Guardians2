using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum LocalizedTextSource { TranslationKey, DataName, Description, DetailDescription }

[DisallowMultipleComponent]
[AddComponentMenu("UI/Localization/Localized Text")]
public sealed class LocalizedText : MonoBehaviour
{
    [Tooltip("비우면 처음 작성한 영어 문구를 키로 사용합니다. Hierarchy 이름은 바꾸지 않습니다.")]
    public string key;
    public TMP_Text tmpText;
    public Text legacyText;
    public LocalizedTextSource source;
    public DefaultData dataSource;
    private string authoredEnglish;
    private bool captured;
    private Scene registeredScene;
    private string lastAppliedText;
    private bool applied;
    public string AuthoredEnglish { get { Cache(); return authoredEnglish; } }

    private void Reset() { tmpText = GetComponent<TMP_Text>(); if (tmpText == null) legacyText = GetComponent<Text>(); }
    private void Awake() => Cache();
    private void OnEnable() { Cache(); registeredScene = gameObject.scene; SceneLanguageMediator.Register(this); }
    private void OnDisable() => SceneLanguageMediator.Unregister(this, registeredScene);
    private void Cache()
    {
        if (tmpText == null && legacyText == null)
        { tmpText = GetComponent<TMP_Text>(); if (tmpText == null) legacyText = GetComponent<Text>(); }
        if (captured) return;
        authoredEnglish = tmpText != null ? tmpText.text : legacyText != null ? legacyText.text : "";
        captured = true;
    }
    public static void BindData(TMP_Text target, DefaultData data, LocalizedTextSource field)
    {
        if (target != null && target.TryGetComponent<LocalizedText>(out var marker)) marker.BindData(data, field);
    }
    public static void RefreshTranslation(TMP_Text target)
    {
        if (target != null && target.TryGetComponent<LocalizedText>(out var marker) && marker.source == LocalizedTextSource.TranslationKey)
            marker.Refresh();
    }
    public void BindData(DefaultData data, LocalizedTextSource field)
    {
        dataSource = data; source = field;
        Refresh();
    }
    public void Refresh()
    {
        if (isActiveAndEnabled && registeredScene != gameObject.scene)
        {
            SceneLanguageMediator.Unregister(this, registeredScene);
            registeredScene = gameObject.scene;
            SceneLanguageMediator.Register(this);
        }
        else SceneLanguageMediator.RefreshText(this);
    }
    internal void Apply(language selected, LocalizedTextTable table)
    {
        Cache();
        string currentText = tmpText != null ? tmpText.text : legacyText != null ? legacyText.text : "";
        // 슬롯 재사용 등으로 외부 코드가 새 영어 문구를 넣었으면 새 원문을 기억합니다.
        if (source == LocalizedTextSource.TranslationKey && applied && currentText != lastAppliedText) authoredEnglish = currentText;
        string text = authoredEnglish;
        if (source != LocalizedTextSource.TranslationKey)
        {
            text = ReadData(selected);
            if (string.IsNullOrEmpty(text)) text = ReadData(language.english);
            if (string.IsNullOrEmpty(text)) text = source == LocalizedTextSource.DataName && dataSource != null ? dataSource.name : "";
        }
        else if (table != null)
        {
            string lookupKey = string.IsNullOrEmpty(key) ? authoredEnglish : key;
            if (!table.TryGetText(lookupKey, selected, out text) && !table.TryGetText(lookupKey, language.english, out text))
                text = authoredEnglish;
        }
        if (tmpText != null) tmpText.text = text ?? "";
        else if (legacyText != null) legacyText.text = text ?? "";
        lastAppliedText = text ?? ""; applied = true;
    }
    private string ReadData(language selected)
    {
        if (dataSource == null) return null;
        switch (source)
        {
            case LocalizedTextSource.DataName: return dataSource.GetDataName(selected);
            case LocalizedTextSource.Description: return dataSource.GetDescription(selected);
            case LocalizedTextSource.DetailDescription: return dataSource.GetDetailDescription(selected);
            default: return null;
        }
    }
}
