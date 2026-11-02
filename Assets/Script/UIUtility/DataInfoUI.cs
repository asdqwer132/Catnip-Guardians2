using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>Reusable data-to-UI binding. Does not create UI or perform gameplay actions.</summary>
[AddComponentMenu("UI/Data Info UI")]
public class DataInfoUI : MonoBehaviour, ISceneLocalizedUI
{
    [Header("Data Information")]
    [FormerlySerializedAs("boxIcon")] public Image icon;
    [FormerlySerializedAs("nameText"), FormerlySerializedAs("titleText"), FormerlySerializedAs("boxNameText")]
    public TextMeshProUGUI dataNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI detailDescriptionText;

    [Header("Optional Initial Data")]
    public DefaultData dataSource;
    public bool hideEmptyDescription;
    public bool hideEmptyDetailDescription = true;

    private DefinedData boundData;
    private string fallbackName;
    private bool hasBinding;
    private Scene registeredScene;
    protected language DisplayLanguage { get; private set; } = language.english;
    protected virtual bool UpdateIcon => true;
    protected virtual bool HideDescriptionWhenEmpty => hideEmptyDescription;

    protected virtual void OnEnable()
    {
        if (!hasBinding && dataSource != null) SetData(dataSource);
        registeredScene = gameObject.scene;
        SceneLanguageMediator.Register(this);
    }

    protected virtual void OnDisable() => SceneLanguageMediator.Unregister(this, registeredScene);

    // DefaultData overload can be connected directly to a UnityEvent in the Inspector.
    public void SetData(DefaultData data) => SetDefinedData(data, data != null ? data.name : "");

    // Also accepts entries from series, grade and currency collections.
    public void SetDefinedData(DefinedData data, string nameFallback = "")
    {
        dataSource = data as DefaultData;
        boundData = data;
        fallbackName = nameFallback;
        hasBinding = true;
        OnDataChanged(data);
        ApplyLanguage(SceneLanguageMediator.GetLanguage(gameObject.scene));
    }

    public void ClearData() => SetData(null);

    public void RefreshData()
    {
        if (isActiveAndEnabled && registeredScene != gameObject.scene)
        {
            SceneLanguageMediator.Unregister(this, registeredScene);
            registeredScene = gameObject.scene;
            SceneLanguageMediator.Register(this);
        }
        else ApplyLanguage(SceneLanguageMediator.GetLanguage(gameObject.scene));
    }

    void ISceneLocalizedUI.RefreshLanguage() => RefreshData();
    void ISceneLocalizedUI.ApplyLanguage(language selected, LocalizedTextTable table) => ApplyLanguage(selected);

    private void ApplyLanguage(language selected)
    {
        DisplayLanguage = selected;
        if (hasBinding)
        {
            if (UpdateIcon && icon != null)
            {
                icon.sprite = boundData?.Icon;
                icon.enabled = icon.sprite != null;
            }
            BindText(dataNameText, DefinedDataUtility.GetName(boundData, selected, fallbackName), LocalizedTextSource.DataName, false);
            BindText(descriptionText, DefinedDataUtility.GetDescription(boundData, selected), LocalizedTextSource.Description, HideDescriptionWhenEmpty);
            BindText(detailDescriptionText, DefinedDataUtility.GetDetailDescription(boundData, selected), LocalizedTextSource.DetailDescription, hideEmptyDetailDescription);
        }
        OnDataLanguageChanged(selected);
    }

    protected virtual void OnDataLanguageChanged(language selected) { }
    protected virtual void OnDataChanged(DefinedData data) { }

    public static void ApplyDefinition(TextMeshProUGUI text, Image image, DefinedData data,
        language selected, string fallback = "", bool hideEmptyText = true)
    {
        if (text != null)
        {
            string value = DefinedDataUtility.GetName(data, selected, fallback);
            text.text = value ?? "";
            if (hideEmptyText) text.gameObject.SetActive(!string.IsNullOrEmpty(value));
            LocalizedText.BindDefinedData(text, data, fallback);
        }
        if (image != null)
        {
            image.sprite = data?.Icon;
            image.enabled = image.sprite != null;
        }
    }

    private void BindText(TextMeshProUGUI target, string value, LocalizedTextSource field, bool hideWhenEmpty)
    {
        if (target == null) return;
        target.text = value ?? "";
        if (hideWhenEmpty) target.gameObject.SetActive(!string.IsNullOrEmpty(value));
        LocalizedText.BindDefinedData(target, boundData, field, fallbackName);
    }

    protected static void SetOptionalText(TextMeshProUGUI target, string value)
    {
        if (target == null) return;
        target.text = value ?? "";
        target.gameObject.SetActive(!string.IsNullOrEmpty(value));
        LocalizedText.RefreshTranslation(target);
    }
}
