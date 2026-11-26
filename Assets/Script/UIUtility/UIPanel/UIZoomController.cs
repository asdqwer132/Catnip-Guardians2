using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIZoomController : MonoBehaviour, IScrollHandler
{
    [Header("Target")]
    public RectTransform zoomTarget;

    [Header("Zoom")]
    public float zoomSpeed = 0.1f;
    public float minZoom = 0.5f;
    public float maxZoom = 2.5f;
    public float defaultZoom = 1f;

    [Header("ScrollRect")]
    public ScrollRect scrollRect;
    public bool disableScrollRectWheelMove = true;

    [Header("Shared Slider")]
    public Slider zoomSlider;
    [SerializeField] private Button zoomInButton;
    [SerializeField] private Button zoomOutButton;
    [SerializeField] private TMPro.TMP_Text zoomLabel;

    [Header("Debug")]
    public bool debugLog = false;

    private float currentZoom;
    private bool isInitialized;

    private void Awake()
    {
        if (zoomTarget == null)
            zoomTarget = GetComponent<RectTransform>();

        currentZoom = Mathf.Clamp(defaultZoom, minZoom, maxZoom);

        if (disableScrollRectWheelMove && scrollRect != null)
            scrollRect.scrollSensitivity = 0f;
    }

    private void OnEnable()
    {
        InitSlider();
        if (zoomInButton != null) zoomInButton.onClick.AddListener(ZoomIn);
        if (zoomOutButton != null) zoomOutButton.onClick.AddListener(ZoomOut);
        if (zoomSlider == null && zoomLabel != null) ApplyZoom();
    }

    private void OnDisable()
    {
        if (zoomInButton != null) zoomInButton.onClick.RemoveListener(ZoomIn);
        if (zoomOutButton != null) zoomOutButton.onClick.RemoveListener(ZoomOut);
        if (zoomSlider != null)
            zoomSlider.onValueChanged.RemoveListener(SetZoomFromSlider);
    }

    private void InitSlider()
    {
        if (zoomSlider == null)
            return;

        zoomSlider.minValue = minZoom;
        zoomSlider.maxValue = maxZoom;
        zoomSlider.wholeNumbers = false;

        zoomSlider.onValueChanged.RemoveListener(SetZoomFromSlider);
        zoomSlider.onValueChanged.AddListener(SetZoomFromSlider);

        if (!isInitialized)
        {
            currentZoom = Mathf.Clamp(zoomSlider.value, minZoom, maxZoom);

            // 슬라이더 값이 이상하게 0으로 되어있을 때만 기본값으로 보정
            if (currentZoom < minZoom || currentZoom > maxZoom)
                currentZoom = Mathf.Clamp(defaultZoom, minZoom, maxZoom);

            ApplyZoom();
            isInitialized = true;
        }
        else
        {
            SetZoomFromSlider(zoomSlider.value);
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (zoomSlider != null || zoomInButton != null || zoomOutButton != null)
            ChangeZoom(eventData.scrollDelta.y);
    }

    private void ZoomIn() => ChangeZoom(1f);
    private void ZoomOut() => ChangeZoom(-1f);

    private void ChangeZoom(float steps)
    {
        if (Mathf.Approximately(steps, 0f)) return;
        float next = Mathf.Clamp(currentZoom + steps * zoomSpeed, minZoom, maxZoom);
        if (zoomSlider != null) zoomSlider.value = next;
        else SetZoomFromSlider(next);
    }

    private void SetZoomFromSlider(float value)
    {
        currentZoom = Mathf.Clamp(value, minZoom, maxZoom);
        ApplyZoom();

        if (debugLog)
            Debug.Log($"[UIZoomController] Apply Zoom: {currentZoom} / Target: {zoomTarget.name}");
    }

    private void ApplyZoom()
    {
        if (zoomTarget == null)
            return;

        zoomTarget.localScale = Vector3.one * currentZoom;
        if (zoomLabel != null) zoomLabel.text = $"{Mathf.RoundToInt(currentZoom * 100f)}%";
        if (zoomInButton != null) zoomInButton.interactable = currentZoom < maxZoom;
        if (zoomOutButton != null) zoomOutButton.interactable = currentZoom > minZoom;
    }

    public void ResetZoom()
    {
        if (zoomSlider != null)
            zoomSlider.value = Mathf.Clamp(defaultZoom, minZoom, maxZoom);
        else
            SetZoomFromSlider(defaultZoom);
    }
}