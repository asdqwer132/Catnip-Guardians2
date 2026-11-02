using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>Common series/class and grade frames, icons and localized labels.</summary>
[AddComponentMenu("UI/Item Definition UI")]
public class ItemDefinitionUI : DataInfoUI
{
    [Header("Series And Grade UI")]
    public TextMeshProUGUI seriesText;
    [FormerlySerializedAs("subTitleText")] public TextMeshProUGUI gradeText;
    [FormerlySerializedAs("seriesImage")] public Image seriesIcon;
    [FormerlySerializedAs("gradeImage")] public Image gradeIcon;
    public ItemSeries series = ItemSeries.None;
    public ItemGrade grade = ItemGrade.None;

    private string alternateGradeLabel;

    protected override void OnEnable()
    {
        DefinedDataManager.DefinitionsChanged += RefreshData;
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        DefinedDataManager.DefinitionsChanged -= RefreshData;
        base.OnDisable();
    }

    protected override void OnDataChanged(DefinedData data)
    {
        base.OnDataChanged(data);
        series = ItemSeries.None;
        grade = ItemGrade.None;
        alternateGradeLabel = null;
        switch (data)
        {
            case ItemData item: series = item.series; grade = item.grade; break;
            case ItemBoxData box: series = box.itemSeries; grade = box.grade; break;
            case ItemTooltipData payload: series = payload.series; grade = payload.grade; break;
            case BagTooltipData bag: alternateGradeLabel = bag.bagType; break;
            case SkillMapTooltipData map: alternateGradeLabel = map.totalNodesCount; break;
        }
    }

    public void SetClassification(ItemSeries itemSeries, ItemGrade itemGrade)
    {
        series = itemSeries;
        grade = itemGrade;
        alternateGradeLabel = null;
        RefreshData();
    }

    protected override void OnDataLanguageChanged(language selected)
    {
        base.OnDataLanguageChanged(selected);
        var manager = DefinedDataManager.instance;
        ApplyDefinition(seriesText, seriesIcon, series != ItemSeries.None ? manager?.GetSeries(series) : null,
            selected, series != ItemSeries.None ? series.ToString() : "");
        ApplyDefinition(gradeText, gradeIcon, grade != ItemGrade.None ? manager?.GetGrade(grade) : null,
            selected, alternateGradeLabel ?? (grade != ItemGrade.None ? grade.ToString() : ""));
    }
}
