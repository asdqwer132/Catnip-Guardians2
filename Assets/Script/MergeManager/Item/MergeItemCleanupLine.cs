using UnityEngine;

// One scene-wide trigger, independent of series, box level, selection and recipes.
[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
public sealed class MergeItemCleanupLine : MonoBehaviour
{
    private BoxCollider2D area;
    private void Reset()
    {
        area = GetComponent<BoxCollider2D>();
        area.isTrigger = true;
        area.size = new Vector2(100f, .1f);
    }
    private void OnEnable() { area = GetComponent<BoxCollider2D>(); area.isTrigger = true; }
    private void OnValidate() { area = GetComponent<BoxCollider2D>(); area.isTrigger = true; }
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
        if (area == null) area = GetComponent<BoxCollider2D>();
        var matrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(area.offset, area.size);
        Gizmos.matrix = matrix;
    }
}
