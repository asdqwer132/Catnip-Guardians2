using System.Collections;
using UnityEngine;

public enum EnemyCircleMoveFinishMode
{
    ByAngle,
    ByDuration
}

[CreateAssetMenu(fileName = "EnemyCircleMoveAction", menuName = "GameData/Enemy/Enemy Pattern/Action/Circle Move Around Target")]
public class EnemyCircleMoveAction : EnemyPatternAction
{
    [Header("Finish")]
    public EnemyCircleMoveFinishMode finishMode = EnemyCircleMoveFinishMode.ByAngle;

    [Tooltip("ByDuration일 때는 실제 지속 시간. ByAngle일 때는 안전 제한 시간")]
    [Min(0.05f)] public float duration = 1.2f;

    [Tooltip("몇 도 돌지. 180이면 반 바퀴, 360이면 한 바퀴")]
    [Min(0f)] public float angle = 180f;

    [Header("Move")]
    [Tooltip("타겟과 유지하려는 거리")]
    [Min(0f)] public float radius = 1.5f;

    [Tooltip("ByAngle 모드에서 사용하는 원형 이동 속도")]
    [Min(0f)] public float moveSpeed = 4f;

    [Tooltip("반지름에서 벗어났을 때 다시 원 궤도로 돌아오려는 힘")]
    [Min(0f)] public float radiusCorrectionPower = 4f;

    [Tooltip("반지름 보정 속도 제한")]
    [Min(0f)] public float maxRadiusCorrectionSpeed = 3f;

    [Header("Direction")]
    public bool clockwise = true;

    [Header("Option")]
    public bool faceMoveDirection = true;
    public bool stopWhenFinished = true;

    public override IEnumerator Execute(EnemyPatternContext context, EnemyPatternEntry pattern)
    {
        if (context == null || !context.HasEnemy)
            yield break;

        ActorMover mover = context.Enemy.mover;

        if (mover == null)
            yield break;

        Transform target = context.GetTargetTransform();

        if (target == null)
            yield break;

        float timer = 0f;
        float rotatedAngle = 0f;

        Vector2 previousRadialDirection = GetRadialDirection(context.Position, target.position);

        while (true)
        {
            target = context.GetTargetTransform();

            if (target == null)
                break;

            float deltaTime = Time.deltaTime;

            Vector2 enemyPosition = context.Position;
            Vector2 targetPosition = target.position;

            Vector2 currentRadialDirection = GetRadialDirection(enemyPosition, targetPosition);

            float frameAngle = Vector2.SignedAngle(previousRadialDirection, currentRadialDirection);

            if (clockwise)
            {
                if (frameAngle < 0f)
                    rotatedAngle += -frameAngle;
            }
            else
            {
                if (frameAngle > 0f)
                    rotatedAngle += frameAngle;
            }

            previousRadialDirection = currentRadialDirection;

            if (ShouldFinish(timer, rotatedAngle))
                break;

            Vector2 moveVelocity = GetCircleMoveVelocity(
                enemyPosition,
                targetPosition,
                currentRadialDirection
            );

            if (moveVelocity.sqrMagnitude > 0.0001f)
            {
                mover.MoveDirection(moveVelocity.normalized, moveVelocity.magnitude);
            }
            else
            {
                mover.Stop();
            }

            timer += deltaTime;
            yield return null;
        }

        if (stopWhenFinished)
            mover.Stop();
    }

    private bool ShouldFinish(float timer, float rotatedAngle)
    {
        switch (finishMode)
        {
            case EnemyCircleMoveFinishMode.ByDuration:
                return timer >= duration;

            case EnemyCircleMoveFinishMode.ByAngle:
            default:
                if (angle > 0f && rotatedAngle >= angle)
                    return true;

                return timer >= duration;
        }
    }

    private Vector2 GetCircleMoveVelocity(Vector2 enemyPosition, Vector2 targetPosition, Vector2 radialDirection)
    {
        float currentDistance = Vector2.Distance(enemyPosition, targetPosition);

        if (currentDistance <= 0.0001f)
            currentDistance = radius;

        Vector2 tangentDirection = clockwise
            ? new Vector2(radialDirection.y, -radialDirection.x)
            : new Vector2(-radialDirection.y, radialDirection.x);

        float orbitSpeed = GetOrbitSpeed();
        float radiusError = radius - currentDistance;

        float correctionSpeed = Mathf.Clamp(
            radiusError * radiusCorrectionPower,
            -maxRadiusCorrectionSpeed,
            maxRadiusCorrectionSpeed
        );

        return tangentDirection * orbitSpeed + radialDirection * correctionSpeed;
    }

    private float GetOrbitSpeed()
    {
        if (finishMode == EnemyCircleMoveFinishMode.ByDuration)
        {
            if (duration <= 0.0001f)
                return moveSpeed;

            float targetRadius = Mathf.Max(0.0001f, radius);
            float angularSpeedRad = angle * Mathf.Deg2Rad / duration;

            return targetRadius * angularSpeedRad;
        }

        return moveSpeed;
    }

    private Vector2 GetRadialDirection(Vector2 enemyPosition, Vector2 targetPosition)
    {
        Vector2 radial = enemyPosition - targetPosition;

        if (radial.sqrMagnitude <= 0.0001f)
            radial = Vector2.right;

        return radial.normalized;
    }
}