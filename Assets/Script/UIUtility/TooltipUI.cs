using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>Reusable data-to-UI binding. Does not create UI or perform gameplay actions.</summary>
[AddComponentMenu("UI/Tooltip UI")]
public partial class TooltipUI : MonoBehaviour, ISceneLocalizedUI
{
    [FormerlySerializedAs("boxIcon")] public Image icon;
    [FormerlySerializedAs("nameText"), FormerlySerializedAs("titleText"), FormerlySerializedAs("boxNameText")]
    public TextMeshProUGUI dataNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI detailDescriptionText;

    public DefaultData dataSource;
    public bool hideEmptyDescription;
    public bool hideEmptyDetailDescription = true;

    // All fields are optional: wire only the information this authored UI displays.
    public TextMeshProUGUI seriesText;
    [FormerlySerializedAs("subTitleText")] public TextMeshProUGUI gradeText;
    [FormerlySerializedAs("seriesImage")] public Image seriesIcon;
    [FormerlySerializedAs("gradeImage")] public Image gradeIcon;
    public ItemSeries series = ItemSeries.None;
    public Grade grade = Grade.None;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI categoryText;
    public TextMeshProUGUI weightText;
    public TextMeshProUGUI cooldownText;
    public TMP_Text effectDescriptions;
    public TextMeshProUGUI totalNodesText;
    public TextMeshProUGUI completedNodesText;
    private DefaultData effectSource;

    public void SetClassification(ItemSeries itemSeries, Grade itemGrade)
    {
        series = itemSeries;
        grade = itemGrade;
        RefreshData();
    }

    private void ApplyClassification(language selected)
    {
        var manager = DefinedDataManager.instance;
        ApplyDefinition(seriesText, seriesIcon, series != ItemSeries.None ? manager?.GetSeries(series) : null,
            selected, series != ItemSeries.None ? series.ToString() : "");
        ApplyDefinition(gradeText, gradeIcon, grade != Grade.None ? manager?.GetGrade(grade) : null,
            selected, grade != Grade.None ? grade.ToString() : "");
    }

    private void ApplyEffectDescriptions(language selected)
    {
        if (effectDescriptions == null) return;
        var text = new System.Text.StringBuilder();
        foreach (var effect in CodexEffectUtility.GetDescriptions(effectSource))
        {
            if (text.Length > 0) text.Append("\n\n");
            text.Append(DefinedDataUtility.GetName(effect, selected, effect.name));
            string description = DefinedDataUtility.GetDescription(effect, selected);
            string detail = DefinedDataUtility.GetDetailDescription(effect, selected);
            if (!string.IsNullOrEmpty(description)) text.Append('\n').Append(description);
            if (!string.IsNullOrEmpty(detail)) text.Append('\n').Append(detail);
        }
        effectDescriptions.text = text.ToString();
        effectDescriptions.gameObject.SetActive(text.Length > 0);
    }

    private DefinedData boundData;
    private string fallbackName;
    private bool hasBinding;
    private Scene registeredScene;
    protected language DisplayLanguage { get; private set; } = language.english;
    protected virtual bool UpdateIcon => true;
    protected virtual bool HideDescriptionWhenEmpty => hideEmptyDescription;

    protected virtual void OnEnable()
    {
        DefinedDataManager.DefinitionsChanged += RefreshData;
        BindPortraitButtons();
        if (!hasBinding && dataSource != null) SetData(dataSource);
        registeredScene = gameObject.scene;
        SceneLanguageMediator.Register(this);
    }

    protected virtual void OnDisable()
    {
        DefinedDataManager.DefinitionsChanged -= RefreshData;
        UnbindPortraitButtons();
        SceneLanguageMediator.Unregister(this, registeredScene);
    }

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

    protected virtual void OnDataLanguageChanged(language selected)
    {
        ApplyClassification(selected);
        ApplyEffectDescriptions(selected);
        RefreshCodexLanguage(selected);
    }

    protected virtual void OnDataChanged(DefinedData data)
    {
        var payload = data as TooltipData;
        var source = payload != null ? payload.sourceData : data as DefaultData;
        series = ItemSeries.None;
        grade = Grade.None;
        var item = data as ItemTooltipData;
        if (item != null) { series = item.series; grade = item.grade; }
        else if (source is ItemData itemData) { series = itemData.series; grade = itemData.grade; }
        else if (source is ItemBoxData box) { series = box.itemSeries; grade = box.grade; }

        SetOptionalText(amountText, payload?.amountText);
        if (item == null && source is ItemData sourceItem) item = ItemTooltipData.FromItem(sourceItem, 0);
        SetOptionalText(categoryText, item?.CategoryText);
        SetOptionalText(weightText, item?.WeightText);
        SetOptionalText(cooldownText, item?.CooldownText);
        var map = data as SkillMapTooltipData;
        SetOptionalText(totalNodesText, map?.totalNodesCount);
        SetOptionalText(completedNodesText, map?.completedNodesCount);
        effectSource = source;
        BindCodexData(source);
    }

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
        target.gameObject.SetActive(!hideWhenEmpty || !string.IsNullOrEmpty(value));
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
