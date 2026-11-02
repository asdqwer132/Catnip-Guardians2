using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class ItemTooltipUI : DataInfoUI
{
    [Header("Tooltip Option")]
    public bool useTooltip = true;
    public Vector2 offset = new Vector2(0f, 20f);
    public bool clampToParent = true;
    [Tooltip("Disable for an information panel already positioned by the scene layout.")]
    public bool positionAtAnchor = true;
    public bool removePannel = true;
    [Tooltip("Existing panels to hide while this information panel is visible.")]
    public GameObject[] alternatePanels;

    [Header("Tooltip Objects")]
    public GameObject tooltipPanel;

    [Header("UI")]
    public TextMeshProUGUI amountText;
    [FormerlySerializedAs("subTitleText")] public TextMeshProUGUI gradeText;
    public TextMeshProUGUI categoryText;
    public TextMeshProUGUI seriesText;
    public TextMeshProUGUI weightText;
    public TextMeshProUGUI cooldownText;

    [Header("Defined Item Information")]
    public ItemSeriesData seriesData;
    public ItemGradeData gradeData;
    public Image seriesIcon;
    public Image gradeIcon;

    private TooltipData displayedData;
    protected override void OnDataLanguageChanged(language selected) => RefreshDefinedInformation();

    private object currentOwner;
    private object pinnedOwner;
    private RectTransform currentAnchorRect;

    private RectTransform tooltipRect;
    private RectTransform parentRect;
    private Canvas rootCanvas;
    private Camera uiCamera;

    private void Awake()
    {
        if (tooltipPanel == null)
            tooltipPanel = gameObject;

        tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        parentRect = tooltipRect.parent as RectTransform;

        rootCanvas = tooltipPanel.GetComponentInParent<Canvas>();

        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = rootCanvas.worldCamera;

        Hide();
    }

    public void Show(ITooltipContentProvider provider)
    {
        if (!useTooltip)
            return;

        if (provider == null)
            return;

        if (!provider.TryGetTooltipData(out TooltipData data))
            return;

        RectTransform anchor = provider.GetTooltipAnchor();

        if (anchor == null)
            return;

        Show(data, anchor, provider);
    }

    public void Show(TooltipData data, RectTransform anchor, object owner)
    {
        if (!useTooltip)
            return;

        if (data == null || anchor == null)
            return;
        if (pinnedOwner != null && pinnedOwner != owner)
            return;

        currentOwner = owner;
        currentAnchorRect = anchor;

        ApplyData(data);

        if (tooltipPanel != null)
            tooltipPanel.SetActive(true);

        SetAlternatePanelsActive(false);

        UpdatePosition();
    }

    public void Hide()
    {
        pinnedOwner = null;
        if (!removePannel) return;

        currentOwner = null;
        currentAnchorRect = null;

        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);

        SetAlternatePanelsActive(true);
    }

    private void SetAlternatePanelsActive(bool active)
    {
        if (alternatePanels == null)
            return;
        foreach (GameObject panel in alternatePanels)
            if (panel != null && panel != tooltipPanel)
                panel.SetActive(active);
    }

    public void Hide(object owner)
    {
        if (currentOwner != owner || pinnedOwner != null)
            return;

        Hide();
    }

    public void Pin(ITooltipContentProvider provider)
    {
        if (!useTooltip || provider == null || !provider.TryGetTooltipData(out var data)) return;
        var anchor = provider.GetTooltipAnchor();
        if (data == null || anchor == null) return;
        pinnedOwner = provider;
        Show(data, anchor, provider);
    }

    public void ReleasePin(object owner, bool hide = false)
    {
        if (pinnedOwner != owner) return;
        pinnedOwner = null;
        if (hide) Hide(owner);
    }

    protected virtual void ApplyData(TooltipData data)
    {
        displayedData = data;
        SetDefinedData(data, data.dataName);
        SetOptionalText(amountText, data.amountText);

        var item = data as ItemTooltipData;
        SetOptionalText(categoryText, item?.CategoryText);
        SetOptionalText(weightText, item?.WeightText);
        SetOptionalText(cooldownText, item?.CooldownText);
    }

    private void RefreshDefinedInformation()
    {
        var item = displayedData as ItemTooltipData;
        DefinedData series = item != null && item.series != ItemSeries.None ? seriesData?.GetData(item.series) : null;
        DefinedData grade = item != null && item.grade != ItemGrade.None ? gradeData?.GetData(item.grade) : null;
        string gradeFallback = item?.GradeText;
        if (displayedData is BagTooltipData bag) gradeFallback = bag.bagType;
        else if (displayedData is SkillMapTooltipData map) gradeFallback = map.totalNodesCount;
        SetDefinedText(seriesText, series, item?.SeriesText);
        SetDefinedText(gradeText, grade, gradeFallback);
        SetDefinedIcon(seriesIcon, series?.Icon);
        SetDefinedIcon(gradeIcon, grade?.Icon);
    }
    private void SetDefinedText(TextMeshProUGUI target, DefinedData data, string fallback)
    {
        SetOptionalText(target, DefinedDataUtility.GetName(data, DisplayLanguage, fallback));
        LocalizedText.BindDefinedData(target, data, fallback);
    }
    private static void SetDefinedIcon(Image target, Sprite sprite)
    {
        if (target == null) return;
        target.sprite = sprite; target.enabled = sprite != null;
    }

    private void UpdatePosition()
    {
        if (!positionAtAnchor)
            return;

        if (currentAnchorRect == null || tooltipRect == null || parentRect == null)
            return;

        Vector3[] corners = new Vector3[4];
        currentAnchorRect.GetWorldCorners(corners);

        Vector3 topCenterWorld = (corners[1] + corners[2]) * 0.5f;

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, topCenterWorld);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            screenPos,
            uiCamera,
            out Vector2 localPos
        );

        Vector2 targetPos = localPos + offset;

        if (clampToParent)
            targetPos = ClampToParent(targetPos);

        tooltipRect.anchoredPosition = targetPos;
    }

    private Vector2 ClampToParent(Vector2 targetPos)
    {
        if (parentRect == null || tooltipRect == null)
            return targetPos;

        Rect parent = parentRect.rect;
        Vector2 tooltipSize = tooltipRect.rect.size;
        Vector2 pivot = tooltipRect.pivot;

        float minX = parent.xMin + tooltipSize.x * pivot.x;
        float maxX = parent.xMax - tooltipSize.x * (1f - pivot.x);

        float minY = parent.yMin + tooltipSize.y * pivot.y;
        float maxY = parent.yMax - tooltipSize.y * (1f - pivot.y);

        targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

        return targetPos;
    }
}
