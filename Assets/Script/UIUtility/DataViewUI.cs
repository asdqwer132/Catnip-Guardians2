using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Reusable data-to-UI binding. Does not create UI or perform gameplay actions.</summary>
[AddComponentMenu("UI/Data View UI")]
public partial class DataViewUI : MonoBehaviour, ISceneLocalizedUI
{
    protected virtual BasicInformationModule BasicInformation => null;
    protected virtual ClassificationModule Classification => null;
    protected virtual ItemInformationModule ItemInformation => null;
    protected virtual SkillMapInformationModule SkillMapInformation => null;
    protected virtual CodexInformationModule CodexInformation => null;
    protected virtual PanelVisibilityModule PanelVisibility => null;
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
    public TextMeshProUGUI amountText { get => SlotInformation?.amountText ?? ItemInformation?.amountText ?? null; set { if (SlotInformation != null) SlotInformation.amountText = value; else if (ItemInformation != null) ItemInformation.amountText = value; } }
    public TextMeshProUGUI categoryText { get => ItemInformation?.categoryText ?? null; set { if (ItemInformation != null) ItemInformation.categoryText = value; } }
    public TextMeshProUGUI weightText { get => ItemInformation?.weightText ?? null; set { if (ItemInformation != null) ItemInformation.weightText = value; } }
    public TextMeshProUGUI cooldownText { get => ItemInformation?.cooldownText ?? null; set { if (ItemInformation != null) ItemInformation.cooldownText = value; } }
    public TMP_Text effectDescriptions { get => ItemInformation?.effectDescriptions ?? null; set { if (ItemInformation != null) ItemInformation.effectDescriptions = value; } }
    public TextMeshProUGUI totalNodesText { get => SkillMapInformation?.totalNodesText ?? null; set { if (SkillMapInformation != null) SkillMapInformation.totalNodesText = value; } }
    public TextMeshProUGUI completedNodesText { get => SkillMapInformation?.completedNodesText ?? null; set { if (SkillMapInformation != null) SkillMapInformation.completedNodesText = value; } }
    public Image portraitImage { get => CodexInformation?.portraitImage ?? null; set { if (CodexInformation != null) CodexInformation.portraitImage = value; } }
    public Image previewImage { get => CodexInformation?.previewImage ?? null; set { if (CodexInformation != null) CodexInformation.previewImage = value; } }
    public TMP_Text portraitDescription { get => CodexInformation?.portraitDescription ?? null; set { if (CodexInformation != null) CodexInformation.portraitDescription = value; } }
    public TMP_Text portraitPage { get => CodexInformation?.portraitPage ?? null; set { if (CodexInformation != null) CodexInformation.portraitPage = value; } }
    public Button previousPortrait { get => CodexInformation?.previousPortrait ?? null; set { if (CodexInformation != null) CodexInformation.previousPortrait = value; } }
    public Button nextPortrait { get => CodexInformation?.nextPortrait ?? null; set { if (CodexInformation != null) CodexInformation.nextPortrait = value; } }
    public DataViewUI portraitInformation { get => CodexInformation?.portraitInformation ?? null; set { if (CodexInformation != null) CodexInformation.portraitInformation = value; } }
    public Transform portraitContent { get => CodexInformation?.portraitContent ?? null; set { if (CodexInformation != null) CodexInformation.portraitContent = value; } }
    public CodexPortraitSlotUI portraitSlotPrefab { get => CodexInformation?.portraitSlotPrefab ?? null; set { if (CodexInformation != null) CodexInformation.portraitSlotPrefab = value; } }
    public ToggleGroup portraitSelectionGroup { get => CodexInformation?.portraitSelectionGroup ?? null; set { if (CodexInformation != null) CodexInformation.portraitSelectionGroup = value; } }
    public CodexInventoryUI childInventory { get => CodexInformation?.childInventory ?? null; set { if (CodexInformation != null) CodexInformation.childInventory = value; } }
    public GameObject[] alternatePanels { get => PanelVisibility?.alternatePanels ?? null; set { if (PanelVisibility != null) PanelVisibility.alternatePanels = value; } }

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
        if (ItemInformation != null && item == null && source is ItemData sourceItem) item = ItemTooltipData.FromItem(sourceItem, 0);
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
