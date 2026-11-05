using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class TooltipData : DefinedData
{
    public DefaultData sourceData;
    public Sprite icon;
    [FormerlySerializedAs("title")] public string dataName;
    public string amountText;
    [TextArea] public string description;
    [TextArea] public string detailDescription;

    public Sprite Icon => icon;
    public string GetDataName(language selected) => sourceData != null ? sourceData.GetDataName(selected) : dataName;
    public string GetDescription(language selected) => sourceData != null ? sourceData.GetDescription(selected) : description;
    public string GetDetailDescription(language selected) => sourceData != null ? sourceData.GetDetailDescription(selected) : detailDescription;
}

// 슬롯과 툴팁이 같은 아이템 정보와 언어 선택을 사용합니다.
[Serializable]
public class ItemTooltipData : TooltipData
{
    public int amount;
    public ItemGrade grade;
    public ItemCategory category;
    public ItemSeries series;
    public float weight;
    public float cooldown;

    public string GradeText => grade == ItemGrade.None ? "" : grade.ToString();
    public string CategoryText => category == ItemCategory.None ? "" : category.ToString();
    public string SeriesText => series == ItemSeries.None ? "" : series.ToString();
    public string WeightText => weight.ToString("0.##", CultureInfo.InvariantCulture);
    public string CooldownText => cooldown.ToString("0.##", CultureInfo.InvariantCulture);

    public static ItemTooltipData FromItem(ItemData item, int amount)
    {
        if (item == null) return null;
        language selected = LanguageManager.instance != null ? LanguageManager.instance.selectedLan : language.english;
        return new ItemTooltipData
        {
            sourceData = item,
            icon = item.icon,
            dataName = DefinedDataUtility.GetName(item, selected, item.name),
            description = DefinedDataUtility.GetDescription(item, selected),
            detailDescription = DefinedDataUtility.GetDetailDescription(item, selected),
            amount = amount,
            amountText = $"x{amount}",
            grade = item.grade,
            category = item.category,
            series = item.series,
            weight = item.weight,
            cooldown = item.Cooldown
        };
    }
}

[Serializable]
public class SkillMapTooltipData : TooltipData
{
    public string totalNodesCount;
    public string completedNodesCount;
}
[Serializable]
public class BagTooltipData : TooltipData
{
    public string bagType;
    public string weight;
    public string slots;
}
[Serializable]
public class CostTooltipData : TooltipData
{
    public Cost[] costs;
    public bool isUnlocked;
}
