using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Reusable data-to-UI binding. Does not create UI or perform gameplay actions.</summary>
public abstract class DataBindingUI : MonoBehaviour, ISceneLocalizedUI
{
    protected virtual BasicInformationModule BasicInformation => null;
    protected virtual ClassificationModule Classification => null;
    protected virtual SlotInformationModule SlotInformation => null;
    public Image icon { get => SlotInformation?.icon ?? BasicInformation?.icon ?? null; set { if (SlotInformation != null) SlotInformation.icon = value; else if (BasicInformation != null) BasicInformation.icon = value; } }
    public TextMeshProUGUI dataNameText { get => SlotInformation?.dataNameText ?? BasicInformation?.dataNameText ?? null; set { if (SlotInformation != null) SlotInformation.dataNameText = value; else if (BasicInformation != null) BasicInformation.dataNameText = value; } }
    public TextMeshProUGUI descriptionText { get => BasicInformation?.descriptionText ?? null; set { if (BasicInformation != null) BasicInformation.descriptionText = value; } }
    public TextMeshProUGUI detailDescriptionText { get => BasicInformation?.detailDescriptionText ?? null; set { if (BasicInformation != null) BasicInformation.detailDescriptionText = value; } }
    public bool hideEmptyDescription { get => BasicInformation?.hideEmptyDescription ?? false; set { if (BasicInformation != null) BasicInformation.hideEmptyDescription = value; } }
    public bool hideEmptyDetailDescription { get => BasicInformation?.hideEmptyDetailDescription ?? false; set { if (BasicInformation != null) BasicInformation.hideEmptyDetailDescription = value; } }
    public TextMeshProUGUI seriesText { get => Classification?.seriesText ?? null; set { if (Classification != null) Classification.seriesText = value; } }
    public TextMeshProUGUI gradeText { get => Classification?.gradeText ?? null; set { if (Classification != null) Classification.gradeText = value; } }
    public Image seriesIcon { get => Classification?.seriesIcon ?? null; set { if (Classification != null) Classification.seriesIcon = value; } }
    public Image gradeIcon { get => Classification?.gradeIcon ?? null; set { if (Classification != null) Classification.gradeIcon = value; } }
    public ItemSeries series { get => Classification?.series ?? ItemSeries.None; set { if (Classification != null) Classification.series = value; } }
    public Grade grade { get => Classification?.grade ?? Grade.None; set { if (Classification != null) Classification.grade = value; } }
    public virtual TextMeshProUGUI amountText { get => SlotInformation?.amountText; set { if (SlotInformation != null) SlotInformation.amountText = value; } }

    public DefaultData dataSource;

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
        if (!hasBinding && dataSource != null) SetData(dataSource);
        registeredScene = gameObject.scene;
        SceneLanguageMediator.Register(this);
    }

    protected virtual void OnDisable()
    {
        DefinedDataManager.DefinitionsChanged -= RefreshData;
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

    public virtual void Show(DefaultData data) { SetData(data); gameObject.SetActive(data != null); }
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
