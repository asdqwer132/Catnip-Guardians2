using UnityEngine;

// 대상마다 하나의 값 타입 상태를 유지한다. 별도 코루틴/컴포넌트가 필요 없다.
internal struct ActorControlledMovement
{
    private const float Epsilon = 0.000001f;
    private bool active;
    private ActorMovementControlRequest request;
    private Vector2 pushDirection;
    private float elapsed;
    private float movedDistance;

    internal bool IsActive => active;
    internal bool SuppressBaseMovement => active && request.suppressBaseMovement;
    internal bool AllowWhileStopped => active && request.allowWhileStopped;

    internal Vector2 GetResistanceDirection(Vector2 position)
    {
        if (!active) return Vector2.zero;
        if (request.mode != ActorMovementControlMode.PullTowards) return -pushDirection;
        Vector2 awayFromCenter = position - request.center;
        return awayFromCenter.sqrMagnitude > Epsilon ? awayFromCenter.normalized : pushDirection;
    }

    internal bool IsControlledBy(object source)
    {
        return active && source != null && ReferenceEquals(request.source, source);
    }

    internal bool TryStart(ActorMovementControlRequest next, Vector2 position)
    {
        if (!IsValid(next) || !Finite(position.x) || !Finite(position.y))
            return false;

        if (active)
        {
            if (next.reapplyMode == ActorMovementControlReapplyMode.IgnoreWhileActive)
                return false;
            if (next.reapplyMode == ActorMovementControlReapplyMode.KeepStronger &&
                request.strength >= next.strength)
                return false;
        }

        Vector2 radial = position - next.center;
        if (next.mode == ActorMovementControlMode.PullTowards && !next.holdAtCenter &&
            radial.magnitude <= next.pullStopDistance)
            return false;

        Vector2 direction = radial.sqrMagnitude > Epsilon
            ? radial.normalized : next.fallbackDirection.normalized;
        if (next.mode == ActorMovementControlMode.SidewaysFromPath)
        {
            Vector2 forward = next.fallbackDirection.sqrMagnitude > Epsilon
                ? next.fallbackDirection.normalized : Vector2.right;
            Vector2 side = new Vector2(-forward.y, forward.x);
            direction = Vector2.Dot(radial, side) < 0f ? -side : side;
        }
        if (direction.sqrMagnitude <= Epsilon)
            direction = Vector2.right;

        request = next;
        pushDirection = direction;
        elapsed = 0f;
        movedDistance = 0f;
        active = true;
        return true;
    }

    internal Vector2 Tick(Vector2 position, float deltaTime)
    {
        if (!active || !Finite(deltaTime) || deltaTime <= 0f)
            return Vector2.zero;

        if (!Finite(position.x) || !Finite(position.y))
        {
            Clear();
            return Vector2.zero;
        }

        float activeDelta = Mathf.Min(deltaTime, Mathf.Max(0f, request.duration - elapsed));
        float multiplier = 1f;
        if (request.speedCurve == ActorMovementControlSpeedCurve.EaseOut)
        {
            // Linear EaseOut의 구간 평균. 프레임 크기에 관계없이 같은 총 이동량.
            multiplier = Mathf.Max(0f, 1f - (elapsed + activeDelta * 0.5f) / request.duration);
        }

        float distance = request.strength * multiplier * activeDelta;
        bool reachedDistanceLimit = false;
        if (request.maxDistance > 0f)
        {
            float remainingDistance = Mathf.Max(0f, request.maxDistance - movedDistance);
            reachedDistanceLimit = distance >= remainingDistance;
            distance = Mathf.Min(distance, remainingDistance);
        }

        Vector2 direction = pushDirection;
        bool reachedCenter = false;
        if (request.mode == ActorMovementControlMode.PullTowards)
        {
            Vector2 toCenter = request.center - position;
            float remainingDistance = Mathf.Max(0f, toCenter.magnitude - request.pullStopDistance);
            reachedCenter = distance >= remainingDistance;
            distance = Mathf.Min(distance, remainingDistance);
            direction = toCenter.sqrMagnitude > Epsilon ? toCenter.normalized : Vector2.zero;
        }

        Vector2 delta = direction * distance;
        movedDistance += distance;
        elapsed += activeDelta;
        bool keepHolding = reachedCenter && request.holdAtCenter;
        if (elapsed >= request.duration || (reachedDistanceLimit && !keepHolding) ||
            (reachedCenter && !request.holdAtCenter))
            Clear();
        return delta;
    }

    internal void Clear()
    {
        this = default(ActorControlledMovement);
    }

    private static bool IsValid(ActorMovementControlRequest value)
    {
        return (value.mode == ActorMovementControlMode.PushAway || value.mode == ActorMovementControlMode.PullTowards ||
                value.mode == ActorMovementControlMode.SidewaysFromPath) &&
            (value.speedCurve == ActorMovementControlSpeedCurve.Constant || value.speedCurve == ActorMovementControlSpeedCurve.EaseOut) &&
            (value.reapplyMode == ActorMovementControlReapplyMode.Replace ||
             value.reapplyMode == ActorMovementControlReapplyMode.IgnoreWhileActive ||
             value.reapplyMode == ActorMovementControlReapplyMode.KeepStronger) &&
            Finite(value.center.x) && Finite(value.center.y) &&
            Finite(value.fallbackDirection.x) && Finite(value.fallbackDirection.y) &&
            Finite(value.strength) && value.strength > 0f &&
            Finite(value.duration) && value.duration > 0f &&
            Finite(value.maxDistance) && value.maxDistance >= 0f &&
            Finite(value.pullStopDistance) && value.pullStopDistance >= 0f;
    }

    private static bool Finite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
