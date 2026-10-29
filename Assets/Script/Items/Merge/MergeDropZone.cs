using UnityEngine;

// 이동 영역은 상자 로컬 좌표, 제한선은 연결된 LineRenderer의 로컬 좌표입니다.
[DisallowMultipleComponent]
public sealed class MergeDropZone : MonoBehaviour
{
    [Header("투입구 이동 영역 · 상자 로컬 좌표")]
    public Rect movementArea = new Rect(-1.8f, 1.9f, 3.6f, .6f);

    [Header("아이템 투입 제한선 · LineRenderer 로컬 좌표, 왼쪽에서 오른쪽")]
    public bool blockAboveLine = true;
    public Vector2 lineStart = new Vector2(-2f, 1.5f);
    public Vector2 lineEnd = new Vector2(2f, 1.5f);
    public LineRenderer lineRenderer;
    [Tooltip("월드 제한선 표시 여부입니다. 끄더라도 투입 제한 판정은 유지합니다.")]
    public bool showWorldLine = true;

    [Header("투입구 표시")]
    public SpriteRenderer spawnMarker;
    [Min(.01f)] public float markerSize = .35f;

    private void LateUpdate() => RefreshLine();
    private Transform LineTransform => lineRenderer != null ? lineRenderer.transform : transform;

    public bool IsBlocked(MergegameManager board)
    {
        if (!blockAboveLine || board == null) return false;
        Vector2 start = LineTransform.TransformPoint(lineStart);
        Vector2 end = LineTransform.TransformPoint(lineEnd);
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
        lineRenderer.enabled = blockAboveLine && showWorldLine;
        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, lineStart);
        lineRenderer.SetPosition(1, lineEnd);
    }

    private void OnDrawGizmosSelected()
    {
        var previous = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(movementArea.center, new Vector3(movementArea.width, movementArea.height, 0f));
        Gizmos.matrix = LineTransform.localToWorldMatrix;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(lineStart, lineEnd);
        Gizmos.matrix = previous;
    }
}
