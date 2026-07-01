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
    }

    private void OnDisable()
    {
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
        if (zoomSlider == null)
            return;

        float scroll = eventData.scrollDelta.y;

        if (Mathf.Approximately(scroll, 0f))
            return;

        float nextZoom = zoomSlider.value + scroll * zoomSpeed;
        nextZoom = Mathf.Clamp(nextZoom, minZoom, maxZoom);

        // 중요:
        // SetValueWithoutNotify 쓰면 안 됨.
        // value를 직접 바꿔야 모든 UIZoomController가 onValueChanged를 받음.
        zoomSlider.value = nextZoom;

        if (debugLog)
            Debug.Log($"[UIZoomController] Wheel / Slider Zoom: {zoomSlider.value}");
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
    }

    public void ResetZoom()
    {
        if (zoomSlider != null)
            zoomSlider.value = Mathf.Clamp(defaultZoom, minZoom, maxZoom);
        else
            SetZoomFromSlider(defaultZoom);
    }
}