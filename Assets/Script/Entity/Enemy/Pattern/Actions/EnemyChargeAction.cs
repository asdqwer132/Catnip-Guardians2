using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyChargeAction", menuName = "GameData/Enemy/Enemy Pattern/Action/Charge")]
public class EnemyChargeAction : EnemyPatternAction
{
    [Header("Move")]
    [Min(0f)] public float speed = 7f;
    [Min(0f)] public float duration = 0.6f;
    public EnemyPatternMoveCurve moveCurve = EnemyPatternMoveCurve.Linear;

    [Header("Pass Through")]
    public bool guaranteePassTarget = true;
    [Min(0f)] public float passTargetDistance = 0.6f;
    [Min(0f)] public float maxExtraDuration = 1f;

    [Header("Hit")]
    [Min(0f)] public float hitRadius = 0.7f;
    public float damage = 0f;
    public float damageMultiplier = 1.2f;
    public bool damageOnce = true;

    [Tooltip("Damage Once가 꺼져 있을 때 반복 피해 간격")]
    [Min(0.01f)] public float damageInterval = 0.2f;

    [Header("Option")]
    public bool forceBlockDefaultAI = true;
    public bool keepFacingChargeDirection = true;

    public override bool ForceBlockDefaultAI => forceBlockDefaultAI;

    public override IEnumerator Execute(EnemyPatternContext context, EnemyPatternEntry pattern)
    {
        if (context == null || !context.HasEnemy || !context.HasTarget)
            yield break;

        ActorMover mover = context.Enemy.mover;
        if (mover == null)
            yield break;

        Transform target = context.GetTargetTransform();
        if (target == null)
            yield break;

        Vector3 startPosition = context.Position;
        Vector3 targetPosition = target.position;

        Vector2 chargeDirection = targetPosition - startPosition;
        if (chargeDirection.sqrMagnitude <= 0.0001f)
            yield break;

        chargeDirection.Normalize();
        context.FaceDirection(chargeDirection);

        float targetDistanceAtStart = Vector2.Distance(startPosition, targetPosition);
        float requiredMoveDistance = guaranteePassTarget
            ? targetDistanceAtStart + passTargetDistance
            : 0f;

        float timer = 0f;
        float movedDistance = 0f;
        float damageTimer = 0f;
        bool damaged = false;

        float baseDuration = Mathf.Max(0.0001f, duration);
        float hardLimitDuration = duration + maxExtraDuration;

        while (ShouldKeepCharging(timer, movedDistance, requiredMoveDistance, hardLimitDuration))
        {
            Vector3 previousPosition = context.Position;

            float deltaTime = Time.deltaTime;
            float t = Mathf.Clamp01(timer / baseDuration);
            float curveMultiplier = GetCurveMultiplier(t);
            float currentSpeed = speed * curveMultiplier;

            if (currentSpeed <= 0f)
                break;

            mover.MoveDirection(chargeDirection, currentSpeed);

            if (keepFacingChargeDirection)
                context.FaceDirection(chargeDirection);

            timer += deltaTime;

            yield return null;

            Vector3 currentPosition = context.Position;
            movedDistance += Vector2.Distance(previousPosition, currentPosition);

            TickDamageTimer(ref damageTimer, deltaTime);
            TryDamageTarget(context, previousPosition, currentPosition, ref damaged, ref damageTimer);
        }

        mover.Stop();

        if (keepFacingChargeDirection)
            context.FaceDirection(chargeDirection);
    }

    private bool ShouldKeepCharging(float timer, float movedDistance, float requiredMoveDistance, float hardLimitDuration)
    {
        if (speed <= 0f)
            return false;

        if (timer < duration)
            return true;

        if (!guaranteePassTarget)
            return false;

        if (requiredMoveDistance <= 0f)
            return false;

        if (movedDistance >= requiredMoveDistance)
            return false;

        return timer < hardLimitDuration;
    }

    private void TickDamageTimer(ref float damageTimer, float deltaTime)
    {
        if (damageTimer > 0f)
            damageTimer -= deltaTime;
    }

    private void TryDamageTarget(EnemyPatternContext context, Vector3 previousPosition, Vector3 currentPosition, ref bool damaged, ref float damageTimer)
    {
        bool canDamage = !damageOnce || !damaged;

        if (!canDamage)
            return;

        if (damageTimer > 0f)
            return;

        if (hitRadius <= 0f)
            return;

        if (!IsTargetHitDuringMove(context, previousPosition, currentPosition, hitRadius))
            return;

        float finalDamage = damage > 0f
            ? damage
            : context.GetAttackDamage() * damageMultiplier;

        context.DamageTarget(finalDamage);

        damaged = true;
        damageTimer = damageInterval;
    }

    private bool IsTargetHitDuringMove(EnemyPatternContext context, Vector3 from, Vector3 to, float radius)
    {
        Transform target = context.GetTargetTransform();

        if (target == null)
            return false;

        float sqrDistance = GetPointToSegmentSqrDistance(target.position, from, to);
        return sqrDistance <= radius * radius;
    }

    private float GetPointToSegmentSqrDistance(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
    {
        Vector2 segment = segmentEnd - segmentStart;
        float segmentLengthSqr = segment.sqrMagnitude;

        if (segmentLengthSqr <= 0.0001f)
            return (point - segmentStart).sqrMagnitude;

        float t = Vector2.Dot(point - segmentStart, segment) / segmentLengthSqr;
        t = Mathf.Clamp01(t);

        Vector2 closestPoint = segmentStart + segment * t;
        return (point - closestPoint).sqrMagnitude;
    }

    private float GetCurveMultiplier(float t)
    {
        switch (moveCurve)
        {
            case EnemyPatternMoveCurve.EaseInOut:
                return Mathf.SmoothStep(0.3f, 1f, t);

            case EnemyPatternMoveCurve.Linear:
            default:
                return 1f;
        }
    }
}