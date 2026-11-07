using System.Text;
using TMPro;
using UnityEngine;

/// <summary>Shared detail fields; opening, closing and navigation belong to each panel.</summary>
public class ItemDetailsUI : ItemDefinitionUI
{
    [Header("Optional Item Details")]
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI categoryText;
    public TextMeshProUGUI weightText;
    public TextMeshProUGUI cooldownText;
    public TMP_Text effectDescriptions;

    private DefaultData effectSource;

    protected override void OnDataChanged(DefinedData data)
    {
        base.OnDataChanged(data);
        var tooltip = data as TooltipData;
        var item = data as ItemTooltipData;
        if (data is ItemData source) item = ItemTooltipData.FromItem(source, 0);

        SetOptionalText(amountText, tooltip?.amountText);
        SetOptionalText(categoryText, item?.CategoryText);
        SetOptionalText(weightText, item?.WeightText);
        SetOptionalText(cooldownText, item?.CooldownText);
        effectSource = tooltip != null ? tooltip.sourceData : data as DefaultData;
    }

    protected override void OnDataLanguageChanged(language selected)
    {
        base.OnDataLanguageChanged(selected);
        if (effectDescriptions == null) return;

        var text = new StringBuilder();
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
    }
}
