using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class TooltipUI
{
    protected virtual ItemInformationModule ItemInformation => null;
    protected virtual SkillMapInformationModule SkillMapInformation => null;
    protected virtual CodexInformationModule CodexInformation => null;
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
    public TooltipUI portraitInformation { get => CodexInformation?.portraitInformation ?? null; set { if (CodexInformation != null) CodexInformation.portraitInformation = value; } }
    public Transform portraitContent { get => CodexInformation?.portraitContent ?? null; set { if (CodexInformation != null) CodexInformation.portraitContent = value; } }
    public CodexPortraitSlotUI portraitSlotPrefab { get => CodexInformation?.portraitSlotPrefab ?? null; set { if (CodexInformation != null) CodexInformation.portraitSlotPrefab = value; } }
    public ToggleGroup portraitSelectionGroup { get => CodexInformation?.portraitSelectionGroup ?? null; set { if (CodexInformation != null) CodexInformation.portraitSelectionGroup = value; } }
    public CodexInventoryUI childInventory { get => CodexInformation?.childInventory ?? null; set { if (CodexInformation != null) CodexInformation.childInventory = value; } }
    public GameObject[] alternatePanels { get => PanelVisibility?.alternatePanels ?? null; set { if (PanelVisibility != null) PanelVisibility.alternatePanels = value; } }

    public override TextMeshProUGUI amountText
    {
        get => ItemInformation?.amountText;
        set { if (ItemInformation != null) ItemInformation.amountText = value; }
    }
    private DefaultData effectSource;
    public virtual GameObject[] AlternatePanels => alternatePanels;
    protected override void OnEnable() { BindPortraitButtons(); base.OnEnable(); }
    protected override void OnDisable() { UnbindPortraitButtons(); base.OnDisable(); }
    protected override void OnDataChanged(DefinedData data)
    {
        base.OnDataChanged(data);
        var payload = data as TooltipData;
        var source = payload != null ? payload.sourceData : data as DefaultData;
        var item = data as ItemTooltipData;
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
    protected override void OnDataLanguageChanged(language selected)
    {
        base.OnDataLanguageChanged(selected);
        ApplyEffectDescriptions(selected);
        RefreshCodexLanguage(selected);
    }
    private void ApplyEffectDescriptions(language selected)
    {
        if (effectDescriptions == null) return;
        var text = new System.Text.StringBuilder();
        bool sharedBody = effectDescriptions == descriptionText;
        if (sharedBody && effectSource != null)
            text.Append(DefinedDataUtility.GetDescription(effectSource, selected));
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
        effectDescriptions.gameObject.SetActive(text.Length > 0 || (sharedBody && !HideDescriptionWhenEmpty));
    }

}
