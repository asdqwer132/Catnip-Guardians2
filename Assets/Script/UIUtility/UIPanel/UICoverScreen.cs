using UnityEngine;

public enum UICoverAnchor
{
    Center,
    Bottom,
    Top,
    Left,
    Right
}

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class UICoverScreen : MonoBehaviour
{
    public RectTransform target;
    public Vector2 imageSize = new Vector2(1920f, 1080f);

    [Header("Extra Space")]
    public float extraLeft = 0f;
    public float extraRight = 0f;
    public float extraTop = 0f;
    public float extraBottom = 0f;

    [Header("Position")]
    public UICoverAnchor anchor = UICoverAnchor.Center;

    private RectTransform rect;
    private Vector2 lastTargetSize;
    private Vector2 lastImageSize;
    private Vector4 lastExtra;
    private UICoverAnchor lastAnchor;

    private void Awake()
    {
        Init();
    }

    private void OnEnable()
    {
        Init();
        Fit();
    }

    private void Update()
    {
        Fit();
    }

    private void OnValidate()
    {
        imageSize.x = Mathf.Max(1f, imageSize.x);
        imageSize.y = Mathf.Max(1f, imageSize.y);

        extraLeft = Mathf.Max(0f, extraLeft);
        extraRight = Mathf.Max(0f, extraRight);
        extraTop = Mathf.Max(0f, extraTop);
        extraBottom = Mathf.Max(0f, extraBottom);

        lastTargetSize = Vector2.zero;
        lastImageSize = Vector2.zero;
        lastExtra = Vector4.zero;
    }

    private void Init()
    {
        if (rect == null)
            rect = GetComponent<RectTransform>();

        if (target == null && transform.parent != null)
            target = transform.parent as RectTransform;
    }

    private void Fit()
    {
        Init();

        if (target == null)
            return;

        Vector2 targetSize = target.rect.size;

        if (targetSize.x <= 0f || targetSize.y <= 0f)
            return;

        Vector4 extra = new Vector4(extraLeft, extraRight, extraTop, extraBottom);

        if (
            targetSize == lastTargetSize &&
            imageSize == lastImageSize &&
            extra == lastExtra &&
            anchor == lastAnchor
        )
            return;

        float screenRatio = targetSize.x / targetSize.y;
        float imageRatio = imageSize.x / imageSize.y;

        Vector2 coverSize;

        if (screenRatio > imageRatio)
        {
            float height = targetSize.x / imageRatio;
            coverSize = new Vector2(targetSize.x, height);
        }
        else
        {
            float width = targetSize.y * imageRatio;
            coverSize = new Vector2(width, targetSize.y);
        }

        Vector2 finalSize = new Vector2(
            coverSize.x + extraLeft + extraRight,
            coverSize.y + extraTop + extraBottom
        );

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = finalSize;

        rect.anchoredPosition = GetPositionOffset(coverSize, finalSize);

        lastTargetSize = targetSize;
        lastImageSize = imageSize;
        lastExtra = extra;
        lastAnchor = anchor;
    }

    private Vector2 GetPositionOffset(Vector2 coverSize, Vector2 finalSize)
    {
        float x = (extraRight - extraLeft) * 0.5f;
        float y = (extraTop - extraBottom) * 0.5f;

        switch (anchor)
        {
            case UICoverAnchor.Bottom:
                y += (finalSize.y - coverSize.y) * 0.5f;
                break;

            case UICoverAnchor.Top:
                y -= (finalSize.y - coverSize.y) * 0.5f;
                break;

            case UICoverAnchor.Left:
                x += (finalSize.x - coverSize.x) * 0.5f;
                break;

            case UICoverAnchor.Right:
                x -= (finalSize.x - coverSize.x) * 0.5f;
                break;
        }

        return new Vector2(x, y);
    }
}