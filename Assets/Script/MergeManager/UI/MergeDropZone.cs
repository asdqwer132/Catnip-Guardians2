using UnityEngine;

// 이동 영역은 표시 오브젝트의 기준 위치에 대한 오프셋입니다. 드래그 중 기준은 움직이지 않습니다.
[DisallowMultipleComponent]
public sealed class MergeDropZone : MonoBehaviour
{
    [Header("실제 스폰 위치 · 이 오브젝트의 현재 좌표 사용")]
    public Transform spawnPoint;
    [Header("투입구 이동 영역 · 표시 기준 왼쪽 아래 오프셋 X/Y, 폭/높이")]
    public Transform movementReference;
    public Rect movementArea = new Rect(-1.8f, -.3f, 3.6f, .6f);

    [Header("아이템 투입 제한선 · SpriteRenderer의 위치와 가로 크기")]
    public bool blockAboveLine = true;
    public SpriteRenderer lineSprite;
    [Tooltip("월드 제한선 표시 여부입니다. 끄더라도 투입 제한 판정은 유지합니다.")]
    public bool showWorldLine = true;

    [Header("투입구 표시")]
    public SpriteRenderer spawnMarker;
    public SpriteRenderer ItemMarker;
    [Min(.01f)] public float markerSize = .35f;

    private Transform capturedMarker;
    private Vector3 markerOrigin;
    private void OnEnable() => CaptureMovementOrigin();
    private void Start() => CaptureMovementOrigin();
    private void LateUpdate() => RefreshLine();

    public void CaptureMovementOrigin()
    {
        capturedMarker = spawnMarker != null ? spawnMarker.transform : null;
        markerOrigin = capturedMarker != null ? transform.InverseTransformPoint(capturedMarker.position) : Vector3.zero;
    }
    public Rect MovementBounds
    {
        get
        {
            if (capturedMarker != (spawnMarker != null ? spawnMarker.transform : null)) CaptureMovementOrigin();
            Vector2 origin = movementReference != null ? (Vector2)transform.InverseTransformPoint(movementReference.position) : (Vector2)markerOrigin;
            return new Rect(origin + movementArea.position, new Vector2(Mathf.Max(0, movementArea.width), Mathf.Max(0, movementArea.height)));
        }
    }

    private bool TryGetLine(out Vector2 start, out Vector2 end)
    {
        start = end = default;
        if (lineSprite == null || lineSprite.sprite == null) return false;
        var bounds = lineSprite.localBounds;
        if (bounds.size.x < .0001f) return false;
        start = lineSprite.transform.TransformPoint(new Vector3(bounds.min.x, bounds.center.y, bounds.center.z));
        end = lineSprite.transform.TransformPoint(new Vector3(bounds.max.x, bounds.center.y, bounds.center.z));
        return true;
    }

    public bool IsBlocked(MergegameManager board)
    {
        if (!blockAboveLine || board == null || !TryGetLine(out var start, out var end)) return false;
        Vector2 direction = end - start;
        float length = direction.magnitude;
        if (length < .0001f) return false;
        Vector2 tangent = direction / length;
        Vector2 normal = new Vector2(-tangent.y, tangent.x);
        foreach (var piece in board.ActiveItems)
        {
            if (piece == null || !piece.IsLive || piece.Owner != board || piece.colider2d == null || !piece.colider2d.enabled) continue;
            var bounds = piece.colider2d.bounds;
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
        Rect area = MovementBounds;
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        local.x = moveX ? Mathf.Clamp(local.x, area.xMin, area.xMax) : lockedLocalPosition.x;
        local.y = moveY ? Mathf.Clamp(local.y, area.yMin, area.yMax) : lockedLocalPosition.y;
        local.z = lockedLocalPosition.z;
        return transform.TransformPoint(local);
    }

    public void SetMarkerVisibility(bool visible)
    {
        if (spawnMarker != null) spawnMarker.enabled = visible && (ItemMarker != null || spawnMarker.sprite != null);
        if (ItemMarker != null) ItemMarker.enabled = visible && ItemMarker.sprite != null;
    }

    public void RefreshMarker(ItemData nextItem)
    {
        var image = ItemMarker != null ? ItemMarker : spawnMarker;
        if (image == null) return;
        var sprite = nextItem != null ? nextItem.icon : null;
        if (image.sprite != sprite) image.sprite = sprite;
        image.enabled = sprite != null;
        if (spawnMarker != null && ItemMarker != null) spawnMarker.enabled = true;
        if (sprite == null) return;
        float scale = Mathf.Max(.01f, markerSize) / Mathf.Max(.0001f, sprite.bounds.size.x, sprite.bounds.size.y);
        image.transform.localScale = new Vector3(scale, scale, 1f);
    }

    public void RefreshLine()
    {
        if (lineSprite != null) lineSprite.enabled = blockAboveLine && showWorldLine;
    }

    private void OnDrawGizmosSelected()
    {
        var previous = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.cyan;
        var area = MovementBounds;
        Gizmos.DrawWireCube(area.center, new Vector3(area.width, area.height, 0f));
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.red;
        if (TryGetLine(out var start, out var end)) Gizmos.DrawLine(start, end);
        Gizmos.matrix = previous;
    }
}
