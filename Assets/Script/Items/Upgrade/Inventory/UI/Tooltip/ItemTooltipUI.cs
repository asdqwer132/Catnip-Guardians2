using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemTooltipUI : MonoBehaviour
{
    [Header("Tooltip Option")]
    public bool useTooltip = true;
    public Vector2 offset = new Vector2(0f, 20f);
    public bool clampToParent = true;

    [Header("Tooltip Objects")]
    public GameObject tooltipPanel;

    [Header("UI")]
    public Image icon;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subTitleText;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI descriptionText;

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

        UpdatePosition();
    }

    public void Hide()
    {
        currentOwner = null;
        currentAnchorRect = null;

        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
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

        if (titleText != null)
            titleText.text = data.title ?? "";

        if (subTitleText != null)
        {
            bool hasSubTitle = !string.IsNullOrEmpty(data.subTitle);
            subTitleText.gameObject.SetActive(hasSubTitle);
            subTitleText.text = hasSubTitle ? data.subTitle : "";
        }

        if (amountText != null)
        {
            bool hasAmount = !string.IsNullOrEmpty(data.amountText);
            amountText.gameObject.SetActive(hasAmount);
            amountText.text = hasAmount ? data.amountText : "";
        }

        if (descriptionText != null)
            descriptionText.text = data.description ?? "";
    }

    private void UpdatePosition()
    {
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