using UnityEngine;

public enum AttackPlacementMode { FixedPoint, Forward, CircleEven, CircleRandom, Shotgun }
public enum AttackDirectionMode { ThrownDirection, FixedWorldDirection }
public enum AttackSpreadDistribution { Random, Even }
public enum AttackSpreadStartMode { CenteredOnDirection, FromDirection }

// 스텝마다, 발사체마다 독립적으로 위치를 계산한다.
public static class AttackPlacement
{
    public static Vector3 Direction(ItemEffectContext context, AttackDirectionMode mode,
        Vector2 fixedDirection, float angle)
    {
        Vector3 direction;
        if (mode == AttackDirectionMode.FixedWorldDirection)
            direction = fixedDirection;
        else if (!context.TryGetDirection(out direction))
            direction = Vector3.right;
        direction.z = 0f;
        if (direction.sqrMagnitude < 0.000001f) direction = Vector3.right;
        return Quaternion.Euler(0f, 0f, angle) * direction.normalized;
    }

    public static Vector3 Position(AttackPlacementMode mode, Vector3 origin, Vector3 forward,
        int index, int count, float forwardOffset, float sideOffset, float radius, float spread,
        float shotgunRadiusOffset = 0f,
        AttackSpreadDistribution shotgunDistribution = AttackSpreadDistribution.Random,
        AttackSpreadStartMode spreadStartMode = AttackSpreadStartMode.CenteredOnDirection,
        bool clockwiseSpread = false)
    {
        Vector3 right = new Vector3(forward.y, -forward.x, 0f);
        if (mode == AttackPlacementMode.Forward)
            return origin + (forward * forwardOffset + right * sideOffset) * index;
        if (mode == AttackPlacementMode.FixedPoint)
            return origin;
        float angle;
        float distance = radius;
        if (mode == AttackPlacementMode.Shotgun)
        {
            angle = ResolveSpreadAngle(spread, index, count, shotgunDistribution, spreadStartMode,
                clockwiseSpread, shotgunDistribution == AttackSpreadDistribution.Random ? Random.value : 0f);
            // 거리 오프셋이 꺼져 있으면 추가 난수를 소비하지 않는다.
            if (shotgunRadiusOffset > 0f)
                distance = ResolveShotgunDistance(radius, shotgunRadiusOffset, Random.value);
            return origin + (Quaternion.Euler(0f, 0f, angle) * forward) * distance;
        }
        if (mode == AttackPlacementMode.CircleRandom)
        {
            angle = ResolveSpreadAngle(spread, index, count, AttackSpreadDistribution.Random,
                spreadStartMode, clockwiseSpread, Random.value);
            distance *= Mathf.Sqrt(Random.value);
        }
        else
        {
            angle = ResolveSpreadAngle(spread, index, count, AttackSpreadDistribution.Even,
                spreadStartMode, clockwiseSpread, 0f);
        }
        return origin + (Quaternion.Euler(0f, 0f, angle) * forward) * distance;
    }

    public static float ResolveSpreadAngle(float spread, int index, int count,
        AttackSpreadDistribution distribution, AttackSpreadStartMode startMode,
        bool clockwise, float sample)
    {
        spread = EffectStatUtility.Safe(spread, 0f, 360f, 0f);
        float start = startMode == AttackSpreadStartMode.FromDirection ? 0f : -spread * 0.5f;
        float angle;
        if (distribution == AttackSpreadDistribution.Random)
            angle = start + spread * EffectStatUtility.Safe(sample, 0f, 1f, 0.5f);
        else
        {
            // 한 발은 기준 방향으로, 360도는 첫/마지막이 겹치지 않게 배치한다.
            if (count <= 1) return 0f;
            float divisor = spread >= 359.999f ? count : count - 1;
            angle = start + spread * Mathf.Clamp(index, 0, count - 1) / divisor;
        }
        return clockwise ? -angle : angle;
    }

    public static float ResolveShotgunDistance(float radius, float offset, float sample)
    {
        radius = EffectStatUtility.Safe(radius, 0f, 100000000f, 0f);
        offset = EffectStatUtility.Safe(offset, 0f, 100000000f, 0f);
        sample = EffectStatUtility.Safe(sample, 0f, 1f, 0.5f);
        // Clip the sampling interval so shots never land behind the origin.
        return Mathf.Lerp(Mathf.Max(0f, radius - offset), radius + offset, sample);
    }
}
