using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("Layout/Polygon Layout Group")]
public class PolygonLayoutGroup : LayoutGroup
{
    [Header("Polygon")]
    [Min(3)]
    public int polygonVertexCount = 6;

    [Header("Radius")]
    public float radius = 200f;

    [Header("Rotation")]
    public float startAngle = 90f;
    public bool clockwise = true;

    [Header("Center Offset")]
    public Vector2 centerOffset = Vector2.zero;

    [Header("Child")]
    public bool rotateChildToCenter = false;
    public bool ignoreInactiveChildren = true;

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        ArrangeChildren();
    }

    public override void CalculateLayoutInputVertical()
    {
        ArrangeChildren();
    }

    public override void SetLayoutHorizontal()
    {
        ArrangeChildren();
    }

    public override void SetLayoutVertical()
    {
        ArrangeChildren();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        polygonVertexCount = Mathf.Max(3, polygonVertexCount);
        radius = Mathf.Max(0f, radius);

        if (isActiveAndEnabled)
            SettDirty();
    }
#endif

    protected override void OnEnable()
    {
        base.OnEnable();
        SettDirty();
    }

    protected override void OnTransformChildrenChanged()
    {
        base.OnTransformChildrenChanged();
        SettDirty();
    }

    public void ArrangeChildren()
    {
        if (rectTransform == null)
            return;

        int activeChildCount = GetActiveChildCount();

        if (activeChildCount <= 0)
            return;

        int vertexCount = Mathf.Max(3, polygonVertexCount);

        float direction = clockwise ? -1f : 1f;
        float angleStep = 360f / vertexCount;

        int index = 0;

        for (int i = 0; i < rectTransform.childCount; i++)
        {
            RectTransform child = rectTransform.GetChild(i) as RectTransform;

            if (child == null)
                continue;

            if (ignoreInactiveChildren && !child.gameObject.activeSelf)
                continue;

            float angle = startAngle + angleStep * index * direction;
            float radian = angle * Mathf.Deg2Rad;

            Vector2 position = new Vector2(
                Mathf.Cos(radian),
                Mathf.Sin(radian)
            ) * radius;

            position += centerOffset;

            SetChildAlongAxis(child, 0, position.x + rectTransform.rect.width * 0.5f - child.rect.width * 0.5f);
            SetChildAlongAxis(child, 1, position.y + rectTransform.rect.height * 0.5f - child.rect.height * 0.5f);

            if (rotateChildToCenter)
            {
                Vector2 toCenter = -position;
                float childAngle = Mathf.Atan2(toCenter.y, toCenter.x) * Mathf.Rad2Deg;
                child.localRotation = Quaternion.Euler(0f, 0f, childAngle - 90f);
            }
            else
            {
                child.localRotation = Quaternion.identity;
            }

            index++;

            if (index >= vertexCount)
                break;
        }
    }

    private int GetActiveChildCount()
    {
        int count = 0;

        for (int i = 0; i < rectTransform.childCount; i++)
        {
            Transform child = rectTransform.GetChild(i);

            if (ignoreInactiveChildren && !child.gameObject.activeSelf)
                continue;

            count++;
        }

        return count;
    }

    protected  void SettDirty()
    {
        if (!IsActive())
            return;

        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }
}