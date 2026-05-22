using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIZoomPan : MonoBehaviour, IDragHandler, IScrollHandler
{
    [Header("Zoom")]
    public float zoomSpeed = 0.1f;
    public float minZoom = 0.5f;
    public float maxZoom = 2.5f;

    [Header("UI")]
    public Slider zoomSlider;

    private RectTransform rectTransform;
    private float currentZoom = 1f;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        if (zoomSlider != null)
        {
            zoomSlider.minValue = minZoom;
            zoomSlider.maxValue = maxZoom;
            zoomSlider.value = currentZoom;

            zoomSlider.onValueChanged.AddListener(SetZoomFromSlider);
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        float scroll = eventData.scrollDelta.y;

        currentZoom += scroll * zoomSpeed;
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

        ApplyZoom();
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta;
    }

    void SetZoomFromSlider(float value)
    {
        currentZoom = value;
        ApplyZoom();
    }

    void ApplyZoom()
    {
        rectTransform.localScale = Vector3.one * currentZoom;

        if (zoomSlider != null && zoomSlider.value != currentZoom)
        {
            zoomSlider.value = currentZoom;
        }
    }
}