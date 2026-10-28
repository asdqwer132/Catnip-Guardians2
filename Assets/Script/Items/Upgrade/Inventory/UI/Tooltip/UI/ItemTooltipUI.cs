using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class ItemTooltipUI : MonoBehaviour
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
    public Image icon;
    [FormerlySerializedAs("titleText")] public TextMeshProUGUI dataNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI detailDescriptionText;
    public TextMeshProUGUI amountText;
    [FormerlySerializedAs("subTitleText")] public TextMeshProUGUI gradeText;
    public TextMeshProUGUI categoryText;
    public TextMeshProUGUI seriesText;
    public TextMeshProUGUI weightText;
    public TextMeshProUGUI cooldownText;

    private object currentOwner;
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
        if (currentOwner != owner)
            return;

        Hide();
    }

    protected virtual void ApplyData(TooltipData data)
    {
        if (icon != null)
        {
            icon.sprite = data.icon;
            icon.enabled = data.icon != null;
        }

        if (dataNameText != null) dataNameText.text = data.dataName ?? "";
        if (descriptionText != null) descriptionText.text = data.description ?? "";
        SetOptionalText(detailDescriptionText, data.detailDescription);
        SetOptionalText(amountText, data.amountText);

        var item = data as ItemTooltipData;
        string grade = item?.GradeText;
        // 기존 가방/스킬 맵 화면의 보조 텍스트 연결도 유지합니다.
        if (data is BagTooltipData bag) grade = bag.bagType;
        else if (data is SkillMapTooltipData map) grade = map.totalNodesCount;
        SetOptionalText(gradeText, grade);
        SetOptionalText(categoryText, item?.CategoryText);
        SetOptionalText(seriesText, item?.SeriesText);
        SetOptionalText(weightText, item?.WeightText);
        SetOptionalText(cooldownText, item?.CooldownText);
    }

    private static void SetOptionalText(TextMeshProUGUI target, string value)
    {
        if (target == null) return;
        target.text = value ?? "";
        target.gameObject.SetActive(!string.IsNullOrEmpty(value));
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
