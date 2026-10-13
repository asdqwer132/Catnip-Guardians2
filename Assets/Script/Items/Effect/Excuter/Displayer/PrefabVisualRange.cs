using System;
using UnityEngine;

public enum PrefabRangeShape { Circle, Sector, Rectangle }
public enum PrefabRangeKind { Damage, Detection, Support, Collider, Preview }

public struct PrefabVisualRange
{
    public string label;
    public PrefabRangeShape shape;
    public PrefabRangeKind kind;
    public Vector3 center, forward, corner0, corner1, corner2, corner3;
    public float radius, angle;
    public static PrefabVisualRange Circle(Vector3 center, float radius, string label, PrefabRangeKind kind)
        => new PrefabVisualRange { center = center, radius = EffectStatUtility.Safe(radius, 0f, 10000f, 0f),
            angle = 360f, forward = Vector3.up, label = label, kind = kind, shape = PrefabRangeShape.Circle };
    public Vector3 ArcPoint(float fraction)
    {
        float spread = shape == PrefabRangeShape.Circle ? 360f : EffectStatUtility.Safe(angle, 0f, 360f, 90f);
        float bearing = Mathf.Atan2(forward.y, forward.x) + (fraction - 0.5f) * spread * Mathf.Deg2Rad;
        return center + new Vector3(Mathf.Cos(bearing), Mathf.Sin(bearing), 0f) * radius;
    }
    public static bool TryCollider(Collider2D collider, string label, out PrefabVisualRange range)
    {
        range = default(PrefabVisualRange);
        if (collider == null || !collider.enabled) return false;
        if (collider is CircleCollider2D circle)
        {
            Vector3 scale = circle.transform.lossyScale;
            range = Circle(circle.transform.TransformPoint(circle.offset),
                circle.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y)), label, PrefabRangeKind.Collider);
            return true;
        }
        if (collider is BoxCollider2D box)
        {
            Vector2 half = box.size * 0.5f, offset = box.offset;
            range = new PrefabVisualRange { shape = PrefabRangeShape.Rectangle, label = label, kind = PrefabRangeKind.Collider,
                center = box.transform.TransformPoint(offset),
                corner0 = box.transform.TransformPoint(offset + new Vector2(-half.x, -half.y)),
                corner1 = box.transform.TransformPoint(offset + new Vector2(half.x, -half.y)),
                corner2 = box.transform.TransformPoint(offset + new Vector2(half.x, half.y)),
                corner3 = box.transform.TransformPoint(offset + new Vector2(-half.x, half.y)) };
            return true;
        }
        return false;
    }
}
