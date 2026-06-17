using UnityEngine;
using UnityEngine.EventSystems;

public class UIDragInParentBounds : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Target")]
    public RectTransform target;

    [Header("Bounds")]
    public RectTransform bounds;

    [Header("Option")]
    public bool clampToParent = true;
    public bool useUnscaledTime = true;

    private Canvas canvas;
    private RectTransform rectTransform;
    private Vector2 pointerOffset;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (target == null)
            target = rectTransform;

        if (bounds == null && clampToParent)
            bounds = target.parent as RectTransform;

        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (target == null)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            target,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPosition
        );

        pointerOffset = localPointerPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (target == null || bounds == null)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            bounds,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPosition
        );

        Vector2 newPosition = localPointerPosition - pointerOffset;
        target.anchoredPosition = ClampToBounds(newPosition);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (target == null || bounds == null)
            return;

        target.anchoredPosition = ClampToBounds(target.anchoredPosition);
    }

    private Vector2 ClampToBounds(Vector2 position)
    {
        Rect boundsRect = bounds.rect;
        Rect targetRect = target.rect;

        float targetWidth = targetRect.width;
        float targetHeight = targetRect.height;

        Vector2 pivot = target.pivot;

        float minX = boundsRect.xMin + targetWidth * pivot.x;
        float maxX = boundsRect.xMax - targetWidth * (1f - pivot.x);

        float minY = boundsRect.yMin + targetHeight * pivot.y;
        float maxY = boundsRect.yMax - targetHeight * (1f - pivot.y);

        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);

        return position;
    }
}