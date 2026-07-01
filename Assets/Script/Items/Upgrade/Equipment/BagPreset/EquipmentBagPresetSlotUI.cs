using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentBagPresetSlotUI : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerClickHandler
{
    [Header("Text")]
    public TextMeshProUGUI nameText;

    [Header("Icon")]
    public Image bagIcon;
    public Image[] itemIcons;

    [Header("Button")]
    public Button applyButton;
    public Button deleteButton;

    [Header("Drag")]
    public bool hideOriginalWhileDragging = true;
    [Range(0f, 1f)] public float ghostAlpha = 0.95f;
    public Vector2 ghostOffset = Vector2.zero;

    private EquipmentBagPresetManager manager;
    private EquipmentBagPresetPanelUI panelUI;
    private EquipmentBagPreset preset;
    private int presetIndex;

    private CanvasGroup canvasGroup;
    private Canvas rootCanvas;
    private RectTransform dragGhostRect;
    private GameObject dragGhostObject;
    private bool isDragging;
    private bool ignoreClick;

    public int PresetIndex => presetIndex;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (applyButton != null)
        {
            applyButton.onClick.RemoveListener(ApplyPreset);
            applyButton.onClick.AddListener(ApplyPreset);
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveListener(DeletePreset);
            deleteButton.onClick.AddListener(DeletePreset);
        }
    }

    private void OnDestroy()
    {
        if (applyButton != null)
            applyButton.onClick.RemoveListener(ApplyPreset);

        if (deleteButton != null)
            deleteButton.onClick.RemoveListener(DeletePreset);
    }

    public void SetSlot(EquipmentBagPresetManager manager, int presetIndex, EquipmentBagPreset preset)
    {
        this.manager = manager;
        this.presetIndex = presetIndex;
        this.preset = preset;
        panelUI = manager != null ? manager.presetPanelUI : GetComponentInParent<EquipmentBagPresetPanelUI>();

        RefreshUI();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (applyButton != null)
            return;

        if (ignoreClick)
            return;

        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        ApplyPreset();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (manager == null)
            return;

        isDragging = true;
        ignoreClick = true;

        CreateDragGhost(eventData);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = hideOriginalWhileDragging ? 0f : 0.25f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        MoveDragGhost(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        isDragging = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        DestroyDragGhost();

        if (panelUI == null && manager != null)
            panelUI = manager.presetPanelUI;

        if (panelUI != null && manager != null)
        {
            int insertIndex = panelUI.GetInsertIndex(eventData, this);
            manager.MovePresetToInsertIndex(presetIndex, insertIndex);
        }

        Invoke(nameof(ResetIgnoreClick), 0.05f);
    }

    private void CreateDragGhost(PointerEventData eventData)
    {
        DestroyDragGhost();

        rootCanvas = GetComponentInParent<Canvas>();

        Transform ghostParent = null;

        if (panelUI == null && manager != null)
            panelUI = manager.presetPanelUI;

        if (panelUI != null && panelUI.DragGhostParent != null)
            ghostParent = panelUI.DragGhostParent;

        if (ghostParent == null && rootCanvas != null)
            ghostParent = rootCanvas.transform;

        if (ghostParent == null)
            ghostParent = transform.root;

        dragGhostObject = Instantiate(gameObject, ghostParent, false);
        dragGhostObject.name = gameObject.name + "_DragGhost";
        dragGhostObject.transform.SetAsLastSibling();

        EquipmentBagPresetSlotUI ghostSlot = dragGhostObject.GetComponent<EquipmentBagPresetSlotUI>();
        if (ghostSlot != null)
            ghostSlot.enabled = false;

        Button[] buttons = dragGhostObject.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
                buttons[i].interactable = false;
        }

        CanvasGroup ghostCanvasGroup = dragGhostObject.GetComponent<CanvasGroup>();
        if (ghostCanvasGroup == null)
            ghostCanvasGroup = dragGhostObject.AddComponent<CanvasGroup>();

        ghostCanvasGroup.alpha = ghostAlpha;
        ghostCanvasGroup.blocksRaycasts = false;
        ghostCanvasGroup.interactable = false;

        dragGhostRect = dragGhostObject.GetComponent<RectTransform>();
        RectTransform originalRect = transform as RectTransform;

        if (dragGhostRect != null && originalRect != null)
        {
            dragGhostRect.anchorMin = new Vector2(0.5f, 0.5f);
            dragGhostRect.anchorMax = new Vector2(0.5f, 0.5f);
            dragGhostRect.pivot = new Vector2(0.5f, 0.5f);
            dragGhostRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalRect.rect.width);
            dragGhostRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, originalRect.rect.height);
        }

        MoveDragGhost(eventData);
    }

    private void MoveDragGhost(PointerEventData eventData)
    {
        if (dragGhostRect == null)
            return;

        RectTransform parentRect = dragGhostRect.parent as RectTransform;

        if (parentRect == null)
            return;

        Camera eventCamera = GetEventCamera(eventData);
        Vector2 localPoint;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventCamera, out localPoint))
            dragGhostRect.anchoredPosition = localPoint + ghostOffset;
    }

    private Camera GetEventCamera(PointerEventData eventData)
    {
        Canvas canvas = rootCanvas;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (eventData != null && eventData.pressEventCamera != null)
            return eventData.pressEventCamera;

        if (eventData != null)
            return eventData.enterEventCamera;

        return null;
    }

    private void DestroyDragGhost()
    {
        if (dragGhostObject != null)
            Destroy(dragGhostObject);

        dragGhostObject = null;
        dragGhostRect = null;
    }

    private void RefreshUI()
    {
        if (preset == null)
            return;

        if (nameText != null)
            nameText.text = preset.presetName;

        if (bagIcon != null)
        {
            Sprite icon = preset.sourceBagData != null ? preset.sourceBagData.icon : null;
            bagIcon.sprite = icon;
            bagIcon.enabled = icon != null;
        }

        RefreshItemIcons();
    }

    private void RefreshItemIcons()
    {
        if (itemIcons == null)
            return;

        for (int i = 0; i < itemIcons.Length; i++)
        {
            Image iconImage = itemIcons[i];

            if (iconImage == null)
                continue;

            ItemData itemData = null;

            if (preset != null && preset.slotItems != null && i < preset.slotItems.Count)
                itemData = preset.slotItems[i];

            if (itemData != null && itemData.icon != null)
            {
                iconImage.sprite = itemData.icon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
        }
    }

    private void ApplyPreset()
    {
        if (manager == null)
            return;

        manager.ApplyPreset(presetIndex);
    }

    private void DeletePreset()
    {
        if (manager == null)
            return;

        manager.DeletePreset(presetIndex);
    }

    private void ResetIgnoreClick()
    {
        ignoreClick = false;
    }
}
