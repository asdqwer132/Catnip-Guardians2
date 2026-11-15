using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public abstract class DataViewModule { }

[Serializable]
public sealed class BasicInformationModule : DataViewModule
{
    public Image icon;
    public TextMeshProUGUI dataNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI detailDescriptionText;
    public bool hideEmptyDescription;
    public bool hideEmptyDetailDescription = true;
}

[Serializable]
public sealed class ClassificationModule : DataViewModule
{
    public TextMeshProUGUI seriesText;
    public TextMeshProUGUI gradeText;
    public Image seriesIcon;
    public Image gradeIcon;
    public ItemSeries series = ItemSeries.None;
    public Grade grade = Grade.None;
}

[Serializable]
public sealed class ItemInformationModule : DataViewModule
{
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI categoryText;
    public TextMeshProUGUI weightText;
    public TextMeshProUGUI cooldownText;
    public TMP_Text effectDescriptions;
}

[Serializable]
public sealed class SkillMapInformationModule : DataViewModule
{
    public TextMeshProUGUI totalNodesText;
    public TextMeshProUGUI completedNodesText;
}

[Serializable]
public sealed class CodexInformationModule : DataViewModule
{
    [NonSerialized] public readonly System.Collections.Generic.List<CodexPortraitSlotUI> Slots = new System.Collections.Generic.List<CodexPortraitSlotUI>();
    public Image portraitImage;
    public Image previewImage;
    public TMP_Text portraitDescription;
    public TMP_Text portraitPage;
    public Button previousPortrait;
    public Button nextPortrait;
    public DataViewUI portraitInformation;
    public Transform portraitContent;
    public CodexPortraitSlotUI portraitSlotPrefab;
    public ToggleGroup portraitSelectionGroup;
    public CodexInventoryUI childInventory;
}

[System.Serializable]
public sealed class PanelVisibilityModule : DataViewModule
{
    public GameObject[] alternatePanels;
}
