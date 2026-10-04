using UnityEngine;

// 기존 DamageArea의 피해, 버프, 타격 모드, 수명, 전체 정리를 그대로 사용한다.
// 공격의 로컬 +Y가 전방이며, 루트 위치는 직사각형의 시작점이다.
[RequireComponent(typeof(BoxCollider2D))]
public class DirectionalRectangleDamageArea : DamageArea
{
    [Header("Rectangle Collider")]
    public BoxCollider2D boxCollider;

    [Header("Runtime Rectangle Size")]
    [Min(0.01f)]
    [SerializeField] private float rectangleWidth = 1f;

    [Min(0.01f)]
    [SerializeField] private float rectangleHeight = 4f;

    public void SetRectangleSize(float width, float height)
    {
        rectangleWidth = Mathf.Max(0.01f, width);
        rectangleHeight = Mathf.Max(0.01f, height);

        ApplyRadius();
    }

    // 상위 클래스가 초기화되거나 버프로 스탯이 바뀔 때도 네모 범위를 유지한다.
    protected override void ApplyRadius()
    {
        rectangleWidth = Mathf.Max(0.01f, rectangleWidth);
        rectangleHeight = Mathf.Max(0.01f, rectangleHeight);

        transform.localScale = Vector3.one;

        if (circleCollider == null)
            circleCollider = GetComponent<CircleCollider2D>();

        if (circleCollider != null)
            circleCollider.enabled = false;

        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider2D>();

        if (boxCollider != null)
        {
            boxCollider.autoTiling = false;
            boxCollider.edgeRadius = 0f;
            boxCollider.size = new Vector2(rectangleWidth, rectangleHeight);
            boxCollider.offset = new Vector2(0f, rectangleHeight * 0.5f);
            boxCollider.isTrigger = true;
        }

        // Range Visual에는 중심 피벗의 1 x 1 스프라이트를 가진 직계 자식을 연결한다.
        // 루트 자체를 늘리면 콜라이더도 함께 늘어나므로 루트는 스케일링하지 않는다.
        if (rangeVisual != null && rangeVisual != transform &&
            rangeVisual.parent == transform)
        {
            rangeVisual.localPosition = new Vector3(0f, rectangleHeight * 0.5f, 0f);
            rangeVisual.localRotation = Quaternion.identity;
            rangeVisual.localScale = new Vector3(rectangleWidth, rectangleHeight, 1f);
        }
    }
}
