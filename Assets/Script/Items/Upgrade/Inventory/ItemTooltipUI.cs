using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ItemTooltipUI : MonoBehaviour
{
    public static ItemTooltipUI instance;

    [Header("Tooltip Option")]
    public bool useTooltip = true;

    [Header("Tooltip Objects")]
    public GameObject tooltipPanel;

    [Header("UI")]
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI gradeText;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI descriptionText;

    [Header("Follow Mouse")]
    public Vector2 offset = new Vector2(20f, -20f);

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform tooltipRect;
    private RectTransform parentRect;

    private bool isShowing = false;

    private void Awake()
    {
        instance = this;

        canvas = GetComponentInParent<Canvas>();

        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        if (tooltipPanel != null)
        {
            tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            parentRect = tooltipRect.parent as RectTransform;
        }

        DisableChildRaycasts();

        Hide();
    }

    private void LateUpdate()
    {
        if (!isShowing)
            return;

        UpdateTooltipPosition();
    }

    public void ToggleTooltip()
    {
        useTooltip = !useTooltip;

        if (!useTooltip)
            Hide();

        Debug.Log("ÅøÆÁ »óÅÂ: " + (useTooltip ? "ÄÑÁü" : "²¨Áü"));
    }

    public void Show(InventoryItem item)
    {
        if (!useTooltip)
        {
            Hide();
            return;
        }

        if (item == null || item.itemData == null)
        {
            Hide();
            return;
        }

        if (tooltipPanel != null)
            tooltipPanel.SetActive(true);

        SetData(item);

        isShowing = true;

        if (tooltipRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);

        UpdateTooltipPosition();
    }

    private void SetData(InventoryItem item)
    {
        ItemData itemData = item.itemData;

        if (icon != null)
        {
            icon.sprite = itemData.icon;
            icon.enabled = itemData.icon != null;
        }

        if (nameText != null)
            nameText.text = itemData.dataName;

        if (gradeText != null)
            gradeText.text = itemData.grade.ToString();

        if (amountText != null)
            amountText.text = "x " + item.amount;

        if (descriptionText != null)
            descriptionText.text = itemData.description;
    }

    public void Hide()
    {
        isShowing = false;

        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    private void UpdateTooltipPosition()
    {
        if (tooltipRect == null || parentRect == null)
            return;

        if (Mouse.current == null)
            return;

        Camera uiCamera = null;

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        Vector2 finalScreenPos = mousePos + offset;

        tooltipRect.pivot = new Vector2(0.5f, 0.5f);

        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);

        Vector2 tooltipSize = tooltipRect.rect.size;

        if (canvas != null)
            tooltipSize *= canvas.scaleFactor;

        float halfWidth = tooltipSize.x * 0.5f;
        float halfHeight = tooltipSize.y * 0.5f;

        if (finalScreenPos.x + halfWidth > Screen.width)
            finalScreenPos.x = mousePos.x - offset.x;

        if (finalScreenPos.x - halfWidth < 0f)
            finalScreenPos.x = mousePos.x - offset.x;

        if (finalScreenPos.y + halfHeight > Screen.height)
            finalScreenPos.y = mousePos.y - offset.y;

        if (finalScreenPos.y - halfHeight < 0f)
            finalScreenPos.y = mousePos.y - offset.y;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            finalScreenPos,
            uiCamera,
            out Vector2 localPoint
        );

        tooltipRect.anchoredPosition = localPoint;
    }

    private void DisableChildRaycasts()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in graphics)
        {
            graphic.raycastTarget = false;
        }
    }
}