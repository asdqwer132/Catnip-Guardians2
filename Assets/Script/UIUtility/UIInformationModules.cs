using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public abstract class UIInformationModule { }

[Serializable]
public sealed class BasicInformationModule : UIInformationModule
{
    public Image icon;
    public TextMeshProUGUI dataNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI detailDescriptionText;
    public bool hideEmptyDescription;
    public bool hideEmptyDetailDescription = true;
}

[Serializable]
public sealed class ClassificationModule : UIInformationModule
{
    public TextMeshProUGUI seriesText;
    public TextMeshProUGUI gradeText;
    public Image seriesIcon;
    public Image gradeIcon;
    public ItemSeries series = ItemSeries.None;
    public Grade grade = Grade.None;
}

[Serializable]
public sealed class ItemInformationModule : UIInformationModule
{
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI categoryText;
    public TextMeshProUGUI weightText;
    public TextMeshProUGUI cooldownText;
    public TMP_Text effectDescriptions;
}

[Serializable]
public sealed class SkillMapInformationModule : UIInformationModule
{
    public TextMeshProUGUI totalNodesText;
    public TextMeshProUGUI completedNodesText;
}

[Serializable]
public sealed class CodexInformationModule : UIInformationModule
{
    [NonSerialized] public readonly System.Collections.Generic.List<CodexPortraitSlotUI> Slots = new System.Collections.Generic.List<CodexPortraitSlotUI>();
    public Image portraitImage;
    public Image previewImage;
    public TMP_Text portraitDescription;
    public TMP_Text portraitPage;
    public Button previousPortrait;
    public Button nextPortrait;
    public TooltipUI portraitInformation;
    public Transform portraitContent;
    public CodexPortraitSlotUI portraitSlotPrefab;
    public ToggleGroup portraitSelectionGroup;
    public CodexInventoryUI childInventory;
}

[System.Serializable]
public sealed class PanelVisibilityModule : UIInformationModule
{
    public GameObject[] alternatePanels;
}

// Direct slots carry only their icon, name and quantity, with no tooltip behaviour.
[Serializable]
public sealed class SlotInformationModule
{
    public Image icon;
    public TextMeshProUGUI dataNameText;
    public TextMeshProUGUI amountText;
}
