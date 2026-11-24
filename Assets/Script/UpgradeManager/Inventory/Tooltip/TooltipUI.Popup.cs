using UnityEngine;

public partial class TooltipUI
{
    public bool useTooltip = true;
    [Tooltip("Hide on startup for a hover tooltip; leave off for fixed information panels and inventory slots.")]
    public bool startHidden;
    protected virtual bool HideOnAwake => startHidden;
    public Vector2 offset = new Vector2(0f, 20f);
    public bool clampToParent = true;
    [Tooltip("Disable for an information panel already positioned by the scene layout.")]
    public bool positionAtAnchor = true;
    public bool removePannel = true;
    [Tooltip("Existing panels to hide while this information panel is visible.")]


    public GameObject tooltipPanel;

    private object currentOwner;
    private object pinnedOwner;
    private RectTransform currentAnchorRect;

    private RectTransform tooltipRect;
    private RectTransform parentRect;
    private Canvas rootCanvas;
    private Camera uiCamera;

    public bool IsVisible => currentOwner != null && tooltipPanel != null && tooltipPanel.activeSelf;
    public event System.Action<bool> VisibilityChanged;

    private void Awake()
    {
        if (HideOnAwake && currentOwner == null) Hide();
    }

    private void PrepareTooltipPanel()
    {
        if (tooltipPanel == null) tooltipPanel = gameObject;
        tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        parentRect = tooltipRect != null ? tooltipRect.parent as RectTransform : null;
        rootCanvas = tooltipPanel.GetComponentInParent<Canvas>();
        uiCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? rootCanvas.worldCamera : null;
    }

    // A fixed panel uses the same renderer without an anchor or item-specific payload.
    public override void Show(DefaultData data)
    {

        if (bindingCodexData) return;
        PrepareTooltipPanel();
        currentOwner = data;
        pinnedOwner = null;
        currentAnchorRect = null;
        SetData(data);
        tooltipPanel.SetActive(data != null);
        if (data != null) SetAlternatePanelsActive(false);
        VisibilityChanged?.Invoke(data != null);
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

        PrepareTooltipPanel();
        currentOwner = owner;
        currentAnchorRect = anchor;

        ApplyData(data);

        if (tooltipPanel != null)
            tooltipPanel.SetActive(true);

        SetAlternatePanelsActive(false);

        UpdatePosition();
        VisibilityChanged?.Invoke(true);
    }

    public void Hide()
    {
        pinnedOwner = null;
        if (!removePannel) return;
        if (tooltipPanel == null) tooltipPanel = gameObject;

        currentOwner = null;
        currentAnchorRect = null;

        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);

        SetAlternatePanelsActive(true);
        VisibilityChanged?.Invoke(false);
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

    public void TogglePin(ITooltipContentProvider provider)
    {
        if (!useTooltip || provider == null) return;
        if (pinnedOwner == provider)
        {
            ReleasePin(provider, true);
            return;
        }
        Pin(provider);
    }

    public bool IsPinned => pinnedOwner != null;

    public void Pin(ITooltipContentProvider provider)
    {
        if (!useTooltip || provider == null) return;
        if (!provider.TryGetTooltipData(out var data)) return;
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
        SetDefinedData(data, data.dataName);
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
