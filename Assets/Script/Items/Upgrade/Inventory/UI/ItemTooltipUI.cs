using TMPro;
using Unity.VisualScripting;
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
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI gradeText;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI descriptionText;

    private BaseItemSlotUI currentSlot;
    private RectTransform currentSlotRect;

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

    public void Show(BaseItemSlotUI slot)
    {
        if (!useTooltip)
            return;

        if (slot == null)
            return;

        if (slot.currentItem == null || slot.currentItem.itemData == null)
            return;

        currentSlot = slot;
        currentSlotRect = slot.GetComponent<RectTransform>();

        ApplyItem(slot.currentItem);

        tooltipPanel.SetActive(true);

        UpdatePosition();
    }

    public void Hide()
    {
        currentSlot = null;
        currentSlotRect = null;

        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    public void Hide(BaseItemSlotUI slot)
    {
        if (currentSlot != slot)
            return;

        Hide();
    }

    private void ApplyItem(InventoryItem item)
    {
        ItemData itemData = item.itemData;

        if (icon != null)
        {
            icon.sprite = itemData.icon;
            icon.enabled = itemData.icon != null;
        }

        if (nameText != null)
            nameText.text = itemData.GetDataName();

        if (gradeText != null)
            gradeText.text = itemData.grade.ToString();

        if (amountText != null)
        {
            bool showAmount = item.amount > 1;
            amountText.gameObject.SetActive(showAmount);
            amountText.text = showAmount ? $"x{item.amount}" : "";
        }

        if (descriptionText != null)
            descriptionText.text = itemData.GetDescription();
    }

    private void UpdatePosition()
    {
        if (currentSlotRect == null || tooltipRect == null || parentRect == null)
            return;

        Vector3[] corners = new Vector3[4];
        currentSlotRect.GetWorldCorners(corners);

        // ½½·ÔÀÇ À§ÂÊ Áß¾Ó À§Ä¡
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