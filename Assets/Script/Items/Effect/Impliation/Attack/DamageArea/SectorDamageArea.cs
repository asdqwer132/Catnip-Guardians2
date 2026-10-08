using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public sealed class SectorDamageArea : DamageArea
{
    [Range(0f, 360f)] public float sectorAngle = 90f;

    public static bool ContainsDirection(Vector2 forward, Vector2 offset, float angle)
    {
        if (offset.sqrMagnitude < 0.000001f) return true;
        if (forward.sqrMagnitude < 0.000001f) return false;
        angle = EffectStatUtility.Safe(angle, 0f, 360f, 90f);
        if (angle >= 360f) return true;
        return Vector2.Dot(forward.normalized, offset.normalized) >=
            Mathf.Cos(angle * 0.5f * Mathf.Deg2Rad) - 0.000001f;
    }

    protected override bool IsInsideAttack(Enemy enemy)
        => enemy != null && ContainsDirection(transform.up,
            enemy.transform.position - transform.position, sectorAngle);
}
