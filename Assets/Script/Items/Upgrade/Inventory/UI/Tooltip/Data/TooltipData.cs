using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class TooltipData
{
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
        return new ItemTooltipData
        {
            icon = item.icon,
            dataName = string.IsNullOrEmpty(dataName) ? item.name : dataName,
            description = item.GetDescription(selected) ?? "",
            detailDescription = item.GetDetailDescription(selected) ?? "",
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
