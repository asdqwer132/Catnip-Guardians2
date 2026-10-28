using UnityEngine;

// 모두 상자 로컬 좌표입니다. 상자/카메라 레이아웃이 바뀌어도 같은 영역을 유지합니다.
[DisallowMultipleComponent]
public sealed class MergeDropZone : MonoBehaviour
{
    [Header("투입구 이동 영역 · 상자 로컬 좌표")]
    public Rect movementArea = new Rect(-1.8f, 1.9f, 3.6f, .6f);

    [Header("아이템 투입 제한선 · 왼쪽에서 오른쪽")]
    public bool blockAboveLine = true;
    public Vector2 lineStart = new Vector2(-2f, 1.5f);
    public Vector2 lineEnd = new Vector2(2f, 1.5f);
    public LineRenderer lineRenderer;

    [Header("투입구 표시")]
    public SpriteRenderer spawnMarker;
    [Min(.01f)] public float markerSize = .35f;

    private void LateUpdate() => RefreshLine();

    public bool IsBlocked(MergegameManager board)
    {
        if (!blockAboveLine || board == null) return false;
        Vector2 start = transform.TransformPoint(lineStart);
        Vector2 end = transform.TransformPoint(lineEnd);
        Vector2 direction = end - start;
        float length = direction.magnitude;
        if (length < .0001f) return false;
        Vector2 tangent = direction / length;
        Vector2 normal = new Vector2(-tangent.y, tangent.x);
        foreach (var piece in board.ActiveItems)
        {
            if (piece == null || !piece.IsLive || piece.Owner != board || piece.circleCollider == null || !piece.circleCollider.enabled) continue;
            var bounds = piece.circleCollider.bounds;
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.y);
            Vector2 offset = (Vector2)bounds.center - start;
            float along = Vector2.Dot(offset, tangent);
            // 원의 가장자리가 선에 닿거나 넘어가면 차단합니다. 다른 상자의 아이템/벽은 검사하지 않습니다.
            if (along + radius >= 0f && along - radius <= length && Vector2.Dot(offset, normal) + radius >= 0f) return true;
        }
        return false;
    }

    public Vector3 ConstrainPosition(Vector3 worldPosition, Vector3 lockedLocalPosition, bool moveX, bool moveY)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        local.x = moveX ? Mathf.Clamp(local.x, movementArea.xMin, movementArea.xMax) : lockedLocalPosition.x;
        local.y = moveY ? Mathf.Clamp(local.y, movementArea.yMin, movementArea.yMax) : lockedLocalPosition.y;
        local.z = lockedLocalPosition.z;
        return transform.TransformPoint(local);
    }

    public void RefreshMarker(ItemData nextItem)
    {
        if (spawnMarker == null) return;
        var sprite = nextItem != null ? nextItem.icon : null;
        if (spawnMarker.sprite != sprite) spawnMarker.sprite = sprite;
        spawnMarker.enabled = sprite != null;
        if (sprite == null) return;
        float scale = Mathf.Max(.01f, markerSize) / Mathf.Max(.0001f, sprite.bounds.size.x, sprite.bounds.size.y);
        spawnMarker.transform.localScale = new Vector3(scale, scale, 1f);
    }

    public void RefreshLine()
    {
        if (lineRenderer == null) return;
        lineRenderer.enabled = blockAboveLine;
        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = 2;
        // LineRenderer를 다른 자식에 연결해도 상자 좌표와 동일한 위치에 표시합니다.
        lineRenderer.SetPosition(0, lineRenderer.transform.InverseTransformPoint(transform.TransformPoint(lineStart)));
        lineRenderer.SetPosition(1, lineRenderer.transform.InverseTransformPoint(transform.TransformPoint(lineEnd)));
    }

    private void OnDrawGizmosSelected()
    {
        var previous = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(movementArea.center, new Vector3(movementArea.width, movementArea.height, 0f));
        Gizmos.color = Color.red;
        Gizmos.DrawLine(lineStart, lineEnd);
        Gizmos.matrix = previous;
    }
}
