using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class RecipeTreeLineUI : MonoBehaviour
{
    public RectTransform from;
    public RectTransform to;
    public Image lineImage;
    [Min(1)] public int requiredAmount = 1;

    [Header("Style")]
    public float normalWidth = 3f;
    public float highlightedWidth = 6f;
    public Color normalColor = new Color(1f, 1f, 1f, 0.35f);
    public Color highlightedColor = new Color(1f, 0.82f, 0.2f, 1f);

    private bool highlighted;

    private void Reset()
    {
        lineImage = GetComponent<Image>();
        if (lineImage != null)
            lineImage.raycastTarget = false;
    }

    private void LateUpdate()
    {
        RefreshPosition();
    }

    public void Setup(RectTransform start, RectTransform end, int amount)
    {
        from = start;
        to = end;
        requiredAmount = Mathf.Max(1, amount);
        SetHighlighted(false);
        RefreshPosition();
    }

    public void SetHighlighted(bool value)
    {
        highlighted = value;
        if (lineImage == null)
            lineImage = GetComponent<Image>();

        if (lineImage != null)
            lineImage.color = highlighted ? highlightedColor : normalColor;

        RefreshPosition();
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void RefreshPosition()
    {
        if (from == null || to == null)
            return;

        RectTransform rect = transform as RectTransform;
        RectTransform parent = rect.parent as RectTransform;
        if (rect == null || parent == null)
            return;

        Vector2 start = parent.InverseTransformPoint(from.TransformPoint(from.rect.center));
        Vector2 end = parent.InverseTransformPoint(to.TransformPoint(to.rect.center));
        Vector2 delta = end - start;

        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = start;
        rect.sizeDelta = new Vector2(delta.magnitude, highlighted ? highlightedWidth : normalWidth);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

}
