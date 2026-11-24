using UnityEngine;

// One scene-wide trigger, independent of series, box level, selection and recipes.
[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
public sealed class MergeItemCleanupLine : MonoBehaviour
{
    private BoxCollider2D area;
    public bool useBoundary = true;
    public Vector2 boundarySize = new Vector2(30f, 20f);
    [Min(0f)] public float boundaryThickness = .1f;
    private EdgeCollider2D boundary;
    [SerializeField] bool drawGizmo = false;
    private void Reset()
    {
        area = GetComponent<BoxCollider2D>();
        area.isTrigger = true;
        area.size = new Vector2(100f, .1f);
    }
    private void OnEnable() => RefreshBoundary();
    private void OnValidate() { area = GetComponent<BoxCollider2D>(); area.isTrigger = true; }
    private void LateUpdate() => RefreshBoundary();
    private Vector2 previousSize;
    private void RefreshBoundary()
    {
        if (area == null) area = GetComponent<BoxCollider2D>();
        area.isTrigger = true; area.enabled = !useBoundary;
        if (boundary == null && useBoundary)
        {
            boundary = GetComponent<EdgeCollider2D>();
            if (boundary == null) boundary = gameObject.AddComponent<EdgeCollider2D>();
        }
        if (boundary == null) return;
        boundary.enabled = useBoundary; boundary.isTrigger = true; boundary.edgeRadius = boundaryThickness * .5f;
        if (previousSize == boundarySize && boundary.pointCount == 5) return;
        previousSize = boundarySize;
        Vector2 half = new Vector2(Mathf.Max(.01f,boundarySize.x), Mathf.Max(.01f,boundarySize.y)) * .5f;
        boundary.points = new[] { new Vector2(-half.x,-half.y), new Vector2(-half.x,half.y), half, new Vector2(half.x,-half.y), new Vector2(-half.x,-half.y) };
    }
    private void OnTriggerEnter2D(Collider2D other) => TryCleanup(other);
    private void OnTriggerStay2D(Collider2D other) => TryCleanup(other);

    public bool TryCleanup(Collider2D other)
    {
        if (!isActiveAndEnabled) return false;
        var piece = other != null ? other.GetComponentInParent<MergeItemObject>() : null;
        if (piece == null || !piece.IsLive) return false;
        int life = piece.LifeId;
        piece.ReturnToPool();
        return !piece.IsLive || piece.LifeId != life;
    }
    private void OnDrawGizmos()
    {
        if (!drawGizmo) return;
        if (area == null) area = GetComponent<BoxCollider2D>();
        var matrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(useBoundary ? Vector2.zero : area.offset, useBoundary ? boundarySize : area.size);
        Gizmos.matrix = matrix;
    }
}
