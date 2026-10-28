using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class TooltipData
{
    public DefaultData sourceData;
    public Sprite icon;
    [FormerlySerializedAs("title")] public string dataName;
    public string amountText;
    [TextArea] public string description;
    [TextArea] public string detailDescription;
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
        string dataName = item.GetDataName(selected);
        string description = item.GetDescription(selected);
        string detail = item.GetDetailDescription(selected);
        if (string.IsNullOrEmpty(dataName)) dataName = item.GetDataName(language.english);
        if (string.IsNullOrEmpty(description)) description = item.GetDescription(language.english);
        if (string.IsNullOrEmpty(detail)) detail = item.GetDetailDescription(language.english);
        return new ItemTooltipData
        {
            sourceData = item,
            icon = item.icon,
            dataName = string.IsNullOrEmpty(dataName) ? item.name : dataName,
            description = description ?? "",
            detailDescription = detail ?? "",
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
}
