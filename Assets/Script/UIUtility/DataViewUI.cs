using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>Reusable data-to-UI binding. Does not create UI or perform gameplay actions.</summary>
[AddComponentMenu("UI/Data View UI")]
public partial class DataViewUI : MonoBehaviour, ISceneLocalizedUI
{
    [FormerlySerializedAs("nameText"), FormerlySerializedAs("titleText"), FormerlySerializedAs("boxNameText")]

    [SerializeReference] public System.Collections.Generic.List<DataViewModule> modules = new System.Collections.Generic.List<DataViewModule>();
    public T Module<T>() where T : DataViewModule => modules.Find(module => module is T) as T;
    private T EnsureModule<T>() where T : DataViewModule, new()
    {
        var module = Module<T>();
        if (module == null) { module = new T(); modules.Add(module); }
        return module;
    }
    public Image icon { get => Module<BasicInformationModule>()?.icon ?? null; set => EnsureModule<BasicInformationModule>().icon = value; }
    public TextMeshProUGUI dataNameText { get => Module<BasicInformationModule>()?.dataNameText ?? null; set => EnsureModule<BasicInformationModule>().dataNameText = value; }
    public TextMeshProUGUI descriptionText { get => Module<BasicInformationModule>()?.descriptionText ?? null; set => EnsureModule<BasicInformationModule>().descriptionText = value; }
    public TextMeshProUGUI detailDescriptionText { get => Module<BasicInformationModule>()?.detailDescriptionText ?? null; set => EnsureModule<BasicInformationModule>().detailDescriptionText = value; }
    public bool hideEmptyDescription { get => Module<BasicInformationModule>()?.hideEmptyDescription ?? false; set => EnsureModule<BasicInformationModule>().hideEmptyDescription = value; }
    public bool hideEmptyDetailDescription { get => Module<BasicInformationModule>()?.hideEmptyDetailDescription ?? false; set => EnsureModule<BasicInformationModule>().hideEmptyDetailDescription = value; }
    public TextMeshProUGUI seriesText { get => Module<ClassificationModule>()?.seriesText ?? null; set => EnsureModule<ClassificationModule>().seriesText = value; }
    public TextMeshProUGUI gradeText { get => Module<ClassificationModule>()?.gradeText ?? null; set => EnsureModule<ClassificationModule>().gradeText = value; }
    public Image seriesIcon { get => Module<ClassificationModule>()?.seriesIcon ?? null; set => EnsureModule<ClassificationModule>().seriesIcon = value; }
    public Image gradeIcon { get => Module<ClassificationModule>()?.gradeIcon ?? null; set => EnsureModule<ClassificationModule>().gradeIcon = value; }
    public ItemSeries series { get => Module<ClassificationModule>()?.series ?? ItemSeries.None; set { var module = Module<ClassificationModule>(); if (module != null) module.series = value; } }
    public Grade grade { get => Module<ClassificationModule>()?.grade ?? Grade.None; set { var module = Module<ClassificationModule>(); if (module != null) module.grade = value; } }
    public TextMeshProUGUI amountText { get => Module<ItemInformationModule>()?.amountText ?? null; set => EnsureModule<ItemInformationModule>().amountText = value; }
    public TextMeshProUGUI categoryText { get => Module<ItemInformationModule>()?.categoryText ?? null; set => EnsureModule<ItemInformationModule>().categoryText = value; }
    public TextMeshProUGUI weightText { get => Module<ItemInformationModule>()?.weightText ?? null; set => EnsureModule<ItemInformationModule>().weightText = value; }
    public TextMeshProUGUI cooldownText { get => Module<ItemInformationModule>()?.cooldownText ?? null; set => EnsureModule<ItemInformationModule>().cooldownText = value; }
    public TMP_Text effectDescriptions { get => Module<ItemInformationModule>()?.effectDescriptions ?? null; set => EnsureModule<ItemInformationModule>().effectDescriptions = value; }
    public TextMeshProUGUI totalNodesText { get => Module<SkillMapInformationModule>()?.totalNodesText ?? null; set => EnsureModule<SkillMapInformationModule>().totalNodesText = value; }
    public TextMeshProUGUI completedNodesText { get => Module<SkillMapInformationModule>()?.completedNodesText ?? null; set => EnsureModule<SkillMapInformationModule>().completedNodesText = value; }
    public Image portraitImage { get => Module<CodexInformationModule>()?.portraitImage ?? null; set => EnsureModule<CodexInformationModule>().portraitImage = value; }
    public Image previewImage { get => Module<CodexInformationModule>()?.previewImage ?? null; set => EnsureModule<CodexInformationModule>().previewImage = value; }
    public TMP_Text portraitDescription { get => Module<CodexInformationModule>()?.portraitDescription ?? null; set => EnsureModule<CodexInformationModule>().portraitDescription = value; }
    public TMP_Text portraitPage { get => Module<CodexInformationModule>()?.portraitPage ?? null; set => EnsureModule<CodexInformationModule>().portraitPage = value; }
    public Button previousPortrait { get => Module<CodexInformationModule>()?.previousPortrait ?? null; set => EnsureModule<CodexInformationModule>().previousPortrait = value; }
    public Button nextPortrait { get => Module<CodexInformationModule>()?.nextPortrait ?? null; set => EnsureModule<CodexInformationModule>().nextPortrait = value; }
    public DataViewUI portraitInformation { get => Module<CodexInformationModule>()?.portraitInformation ?? null; set => EnsureModule<CodexInformationModule>().portraitInformation = value; }
    public Transform portraitContent { get => Module<CodexInformationModule>()?.portraitContent ?? null; set => EnsureModule<CodexInformationModule>().portraitContent = value; }
    public CodexPortraitSlotUI portraitSlotPrefab { get => Module<CodexInformationModule>()?.portraitSlotPrefab ?? null; set => EnsureModule<CodexInformationModule>().portraitSlotPrefab = value; }
    public ToggleGroup portraitSelectionGroup { get => Module<CodexInformationModule>()?.portraitSelectionGroup ?? null; set => EnsureModule<CodexInformationModule>().portraitSelectionGroup = value; }
    public CodexInventoryUI childInventory { get => Module<CodexInformationModule>()?.childInventory ?? null; set => EnsureModule<CodexInformationModule>().childInventory = value; }

    public DefaultData dataSource;

    // All fields are optional: wire only the information this authored UI displays.
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

    public GameObject[] alternatePanels { get => Module<PanelVisibilityModule>()?.alternatePanels; set => EnsureModule<PanelVisibilityModule>().alternatePanels = value; }
    public virtual GameObject[] AlternatePanels => alternatePanels;
    public void Show(DefaultData data) { if (bindingCodexData) return; SetData(data); gameObject.SetActive(data != null); if (data != null && AlternatePanels != null) foreach (var panel in AlternatePanels) if (panel != null && panel != gameObject) panel.SetActive(false); }
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
