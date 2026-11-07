using UnityEngine;

// Shared geometry for rectangular spawn borders and attack bands.
public static class EnemyRegionGeometry
{
    public static void RectangleHalfSizes(Vector2 size, float thickness, out Vector2 inner, out Vector2 outer)
    {
        Vector2 half = new Vector2(
            EffectStatUtility.Safe(size.x, .01f, 100000f, 4f),
            EffectStatUtility.Safe(size.y, .01f, 100000f, 4f)) * .5f;
        float padding = EffectStatUtility.Safe(thickness, 0f, 100000f, 0f) * .5f;
        inner = Vector2.Max(Vector2.zero, half - Vector2.one * padding);
        outer = half + Vector2.one * padding;
    }

    public static float RectangleRayDistance(Vector2 direction, Vector2 halfSize)
    {
        float x = Mathf.Abs(direction.x), y = Mathf.Abs(direction.y);
        if (x < .000001f && y < .000001f) return 0f;
        return Mathf.Min(x > .000001f ? halfSize.x / x : float.PositiveInfinity,
            y > .000001f ? halfSize.y / y : float.PositiveInfinity);
    }

    public static Vector2 RectangleBorderPoint(Vector2 size, float thickness)
    {
        RectangleHalfSizes(size, thickness, out var inner, out var outer);
        Vector2 half = (inner + outer) * .5f;
        float edge = Random.Range(0f, 4f * (half.x + half.y));
        Vector2 point;
        if (edge < half.x * 2f) point = new Vector2(-half.x + edge, half.y);
        else if ((edge -= half.x * 2f) < half.y * 2f) point = new Vector2(half.x, half.y - edge);
        else if ((edge -= half.y * 2f) < half.x * 2f) point = new Vector2(half.x - edge, -half.y);
        else point = new Vector2(-half.x, -half.y + edge - half.x * 2f);
        Vector2 direction = point.normalized;
        return direction * Random.Range(RectangleRayDistance(direction, inner), RectangleRayDistance(direction, outer));
    }
}
