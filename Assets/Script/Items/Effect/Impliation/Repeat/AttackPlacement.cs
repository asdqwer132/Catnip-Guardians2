using UnityEngine;

public enum AttackPlacementMode { FixedPoint, Forward, CircleEven, CircleRandom, Shotgun }
public enum AttackDirectionMode { ThrownDirection, FixedWorldDirection }

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
        float shotgunRadiusOffset = 0f)
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
            angle = Random.Range(-spread * 0.5f, spread * 0.5f);
            // Keep the original random sequence when the offset is disabled.
            if (shotgunRadiusOffset > 0f)
                distance = ResolveShotgunDistance(radius, shotgunRadiusOffset, Random.value);
            return origin + (Quaternion.Euler(0f, 0f, angle) * forward) * distance;
        }
        if (mode == AttackPlacementMode.CircleRandom)
        {
            angle = Random.Range(-spread * 0.5f, spread * 0.5f);
            distance *= Mathf.Sqrt(Random.value);
        }
        else
        {
            // 360도일 때 첫/마지막 폭탄이 같은 지점에 겹치지 않는다.
            float divisor = spread >= 359.999f ? count : Mathf.Max(1, count - 1);
            angle = count <= 1 ? 0f : -spread * 0.5f + spread * index / divisor;
        }
        return origin + (Quaternion.Euler(0f, 0f, angle) * forward) * distance;
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
